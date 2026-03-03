/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LoginEncryption.cs                                              *
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

namespace Server.Network;

/// <summary>
/// Implements login packet encryption/decryption using XOR with version-derived keys.
/// Used for the initial account login packet (0x80).
/// </summary>
public sealed class LoginEncryption : IClientEncryption
{
    private uint _table1;
    private uint _table2;
    private readonly uint _key1;
    private readonly uint _key2;

    public LoginEncryption(uint seed, LoginKeys keys)
    {
        _key1 = keys.Key1;
        _key2 = keys.Key2;

        // Initialize state tables from seed
        _table1 = ((~seed ^ 0x00001357) << 16) | ((seed ^ 0xFFFFAAAA) & 0x0000FFFF);
        _table2 = ((seed ^ 0x43210000) >> 16) | ((~seed ^ 0xABCDFFFF) & 0xFFFF0000);
    }

    /// <summary>
    /// Attempts to initialize login encryption and validate the packet.
    /// Returns true if the packet appears to be validly encrypted with this scheme.
    /// </summary>
    public static bool TryDecrypt(
        ClientVersion version,
        uint seed,
        ReadOnlySpan<byte> encryptedPacket,
        out LoginEncryption encryption)
    {
        const int LoginPacketSize = 62;

        encryption = null;

        var keys = LoginKeys.GetKeys(version);
        if (keys is { Key1: 0, Key2: 0 })
        {
            return false;
        }

        if (encryptedPacket.Length < LoginPacketSize)
        {
            return false;
        }

        // Copy and decrypt
        Span<byte> decrypted = stackalloc byte[LoginPacketSize];
        encryptedPacket[..LoginPacketSize].CopyTo(decrypted);
        var enc = new LoginEncryption(seed, keys);
        enc.ClientDecrypt(decrypted);

        // Validate decrypted packet structure:
        // - Byte 0 must be 0x80 (account login packet ID)
        // - Byte 30 must be 0x00 (null terminator for username)
        // - Byte 60 must be 0x00 (null terminator for password)
        if (decrypted[0] != 0x80 || decrypted[30] != 0x00 || decrypted[60] != 0x00)
        {
            return false;
        }

        // Re-initialize encryption state for actual use
        encryption = new LoginEncryption(seed, keys);
        return true;
    }

    /// <summary>
    /// Decrypts incoming data from the client (in-place).
    /// </summary>
    public void ClientDecrypt(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] ^= (byte)(_table1 & 0xFF);

            var edx = _table2;
            var esi = _table1 << 31;
            var eax = _table2 >> 1;

            eax |= esi;
            eax ^= _key1 - 1;
            edx <<= 31;
            eax >>= 1;

            var ecx = _table1 >> 1;

            eax |= esi;
            ecx |= edx;
            eax ^= _key1;
            ecx ^= _key2;

            _table1 = ecx;
            _table2 = eax;
        }
    }

    /// <summary>
    /// Server does not encrypt login responses, so this is a no-op.
    /// </summary>
    public void ServerEncrypt(Span<byte> buffer)
    {
        // Login encryption is client-to-server only
    }
}
