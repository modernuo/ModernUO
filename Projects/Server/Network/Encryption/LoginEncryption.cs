/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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
using Server.Logging;

namespace Server.Network;

/// <summary>
/// Implements login packet encryption/decryption using XOR with version-derived keys.
/// Used for the initial account login packet (0x80).
/// </summary>
public sealed class LoginEncryption : IClientEncryption
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(LoginEncryption));

    private const int LoginPacketSize = 62;

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
    /// When version is null (pre-6.0.5 clients that don't send 0xEF), tries all known legacy keys.
    /// </summary>
    public static bool TryDecrypt(
        ClientVersion version,
        uint seed,
        ReadOnlySpan<byte> encryptedPacket,
        out LoginEncryption encryption)
    {
        encryption = null;

        if (encryptedPacket.Length < LoginPacketSize)
        {
            return false;
        }

        if (version != null)
        {
            var keys = LoginKeys.GetKeys(version);
            return keys is not { Key1: 0, Key2: 0 } && TryDecryptWithKeys(keys, seed, encryptedPacket, out encryption);
        }

        // No version available (pre-6.0.5 client) - try all known legacy keys. The 5.0.x and 6.0.x (x <= 4)
        // keystreams differ only in bytes 16-23 and 37-44, inside the credential fields, so a login from
        // either passes the structural check under both keys; only the field contents tell them apart.
        // When they don't, the first candidate wins, as it always has: this may add logins, never remove one.
        Span<byte> decrypted = stackalloc byte[LoginPacketSize];
        var legacyKeys = LoginKeys.LegacyKeys;
        var candidates = 0;
        var firstCandidate = -1;
        var wellFormed = 0;
        var wellFormedCandidate = -1;

        for (var i = 0; i < legacyKeys.Length; i++)
        {
            if (!Decrypt(legacyKeys[i], seed, encryptedPacket, decrypted))
            {
                continue;
            }

            if (candidates++ == 0)
            {
                firstCandidate = i;
            }

            if (HasWellFormedCredentials(decrypted))
            {
                wellFormed++;
                wellFormedCandidate = i;
            }
        }

        if (candidates == 0)
        {
            return false;
        }

        if (candidates > 1 && wellFormed == 1)
        {
            encryption = new LoginEncryption(seed, legacyKeys[wellFormedCandidate]);
            return true;
        }

        if (candidates > 1)
        {
            logger.Debug(
                "Login from a pre-6.0.5 client matches {Count} legacy encryption keys ({WellFormed} with well-formed credentials); using the first",
                candidates,
                wellFormed
            );
        }

        encryption = new LoginEncryption(seed, legacyKeys[firstCandidate]);
        return true;
    }

    /// <summary>
    /// Attempts decryption with a specific key pair and validates the result.
    /// </summary>
    private static bool TryDecryptWithKeys(
        LoginKeys keys,
        uint seed,
        ReadOnlySpan<byte> encryptedPacket,
        out LoginEncryption encryption)
    {
        Span<byte> decrypted = stackalloc byte[LoginPacketSize];

        // Re-initialize encryption state for actual use
        encryption = Decrypt(keys, seed, encryptedPacket, decrypted) ? new LoginEncryption(seed, keys) : null;
        return encryption != null;
    }

    /// <summary>
    /// Decrypts a copy of the login packet and checks its structure: byte 0 is 0x80 and the username and
    /// password fields end in a NUL at bytes 30 and 60.
    /// </summary>
    private static bool Decrypt(LoginKeys keys, uint seed, ReadOnlySpan<byte> encryptedPacket, Span<byte> decrypted)
    {
        encryptedPacket[..LoginPacketSize].CopyTo(decrypted);
        new LoginEncryption(seed, keys).ClientDecrypt(decrypted);
        return decrypted[0] == 0x80 && decrypted[30] == 0x00 && decrypted[60] == 0x00;
    }

    // Only breaks ties between keys, so it may be stricter than the 0x80 handler: a wrong key leaves
    // flipped bits in the padding or turns a character into a non-ASCII byte.
    private static bool HasWellFormedCredentials(ReadOnlySpan<byte> decrypted) =>
        IsWellFormedField(decrypted.Slice(1, 30), minLength: 1) && IsWellFormedField(decrypted.Slice(31, 30), minLength: 0);

    // Printable ASCII, then NUL padding to the end of the field
    private static bool IsWellFormedField(ReadOnlySpan<byte> field, int minLength)
    {
        var length = field.IndexOf((byte)0);
        if (length < minLength)
        {
            return false;
        }

        for (var i = 0; i < length; i++)
        {
            if (field[i] is < 0x20 or > 0x7E)
            {
                return false;
            }
        }

        return !field[length..].ContainsAnyExcept((byte)0);
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
