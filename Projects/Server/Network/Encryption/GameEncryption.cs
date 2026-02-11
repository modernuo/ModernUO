/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GameEncryption.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using Server.Logging;

namespace Server.Network;

/// <summary>
/// Implements game packet encryption/decryption using Twofish + MD5.
/// Used for all game packets after login (0x91 and onwards).
/// </summary>
public sealed class GameEncryption : IClientEncryption
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GameEncryption));

    private const int CipherTableSize = 256;
    private const int BlockSize = 16;

    // Static identity table [0..255] for vectorized CopyTo initialization
    private static readonly byte[] IdentityTable = CreateIdentityTable();

    private static byte[] CreateIdentityTable()
    {
        var table = new byte[CipherTableSize];
        for (var i = 0; i < CipherTableSize; i++)
        {
            table[i] = (byte)i;
        }
        return table;
    }

    private readonly TwofishEngine _twofish;
    private readonly byte[] _cipherTable;
    private readonly byte[] _xorKey;

    private ushort _recvPos;
    private byte _sendPos;

    public GameEncryption(uint seed)
    {
        // Create 16-byte key from seed (repeated 4 times)
        Span<byte> key = stackalloc byte[16];
        key[0] = key[4] = key[8] = key[12] = (byte)((seed >> 24) & 0xFF);
        key[1] = key[5] = key[9] = key[13] = (byte)((seed >> 16) & 0xFF);
        key[2] = key[6] = key[10] = key[14] = (byte)((seed >> 8) & 0xFF);
        key[3] = key[7] = key[11] = key[15] = (byte)(seed & 0xFF);

        _twofish = new TwofishEngine(key);

        // Initialize cipher table with identity [0..255] using vectorized copy
        _cipherTable = GC.AllocateUninitializedArray<byte>(CipherTableSize);
        IdentityTable.CopyTo(_cipherTable, 0);

        // Encrypt cipher table with Twofish
        RefreshCipherTable();

        // Compute MD5 hash of cipher table for server->client XOR key
        _xorKey = MD5.HashData(_cipherTable);
    }

    /// <summary>
    /// Refreshes the cipher table by encrypting it with Twofish.
    /// Called every 256 bytes of received data.
    /// </summary>
    private void RefreshCipherTable()
    {
        // Encrypt cipher table in 16-byte blocks
        for (var i = 0; i < CipherTableSize; i += BlockSize)
        {
            _twofish.EncryptBlock(_cipherTable.AsSpan(i, BlockSize));
        }

        _recvPos = 0;
    }

    /// <summary>
    /// Decrypts incoming data from the client (in-place).
    /// XORs with cipher table, refreshing every 256 bytes.
    /// </summary>
    public void ClientDecrypt(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            if (_recvPos >= CipherTableSize)
            {
                RefreshCipherTable();
            }

            buffer[i] ^= _cipherTable[_recvPos++];
        }
    }

    /// <summary>
    /// Encrypts outgoing data to the client (in-place).
    /// XORs with MD5 hash of cipher table (16-byte rotating key).
    /// Uses SIMD optimization for larger buffers.
    /// </summary>
    public void ServerEncrypt(Span<byte> buffer)
    {
        var i = 0;

        // SIMD path: process 16 bytes at a time when aligned with key
        if (_sendPos == 0 && buffer.Length >= 16 && Vector128.IsHardwareAccelerated)
        {
            var keyVec = Vector128.Create(_xorKey);

            for (; i + 16 <= buffer.Length; i += 16)
            {
                var chunk = Vector128.LoadUnsafe(ref buffer[i]);
                var result = Vector128.Xor(chunk, keyVec);
                result.StoreUnsafe(ref buffer[i]);
            }
        }

        // Scalar path for remainder or when not aligned
        for (; i < buffer.Length; i++)
        {
            buffer[i] ^= _xorKey[_sendPos++];
            _sendPos &= 0x0F; // Wrap at 16
        }
    }

    /// <summary>
    /// Attempts to decrypt a game login packet and validate it.
    /// Returns true if the packet appears to be validly encrypted.
    /// </summary>
    public static bool TryDecrypt(uint seed, ReadOnlySpan<byte> encryptedPacket, out GameEncryption encryption)
    {
        const int GameLoginPacketSize = 65;

        encryption = null;

        if (encryptedPacket.Length < GameLoginPacketSize)
        {
            if (EncryptionManager.Debug)
            {
                logger.Debug("GameEncryption.TryDecrypt: Invalid buffer length {Length}", encryptedPacket.Length);
            }
            return false;
        }

        if (EncryptionManager.Debug)
        {
            logger.Debug("GameEncryption.TryDecrypt: Seed=0x{Seed:X8}", seed);
            logger.Debug("GameEncryption.TryDecrypt: Encrypted[0..16]: {Bytes}", Convert.ToHexString(encryptedPacket[..16]));
        }

        // Copy and decrypt
        Span<byte> decrypted = stackalloc byte[GameLoginPacketSize];
        encryptedPacket[..GameLoginPacketSize].CopyTo(decrypted);
        var enc = new GameEncryption(seed);

        if (EncryptionManager.Debug)
        {
            logger.Debug("GameEncryption.TryDecrypt: CipherTable[0..16]: {Bytes}",
                Convert.ToHexString(enc._cipherTable.AsSpan(0, 16)));
            logger.Debug("GameEncryption.TryDecrypt: XorKey: {Bytes}", Convert.ToHexString(enc._xorKey));
        }

        enc.ClientDecrypt(decrypted);

        if (EncryptionManager.Debug)
        {
            logger.Debug("GameEncryption.TryDecrypt: Decrypted[0..16]: {Bytes}", Convert.ToHexString(decrypted[..16]));
            logger.Debug("GameEncryption.TryDecrypt: First byte=0x{Byte:X2} (expected 0x91)", decrypted[0]);
        }

        // Validate: first byte must be 0x91 (game server login packet ID)
        if (decrypted[0] != 0x91)
        {
            if (EncryptionManager.Debug)
            {
                logger.Debug("GameEncryption.TryDecrypt: Validation FAILED - first byte is not 0x91");
            }
            return false;
        }

        if (EncryptionManager.Debug)
        {
            logger.Debug("GameEncryption.TryDecrypt: Validation PASSED");
        }

        // Re-create encryption for actual use
        encryption = new GameEncryption(seed);
        return true;
    }
}
