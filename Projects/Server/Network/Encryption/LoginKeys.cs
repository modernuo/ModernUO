/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LoginKeys.cs                                                    *
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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server.Network;

/// <summary>
/// Represents encryption keys derived from a client version.
/// Used for login packet encryption/decryption.
/// </summary>
public readonly struct LoginKeys
{
    public static readonly LoginKeys Empty = new(0, 0);

    private static readonly Dictionary<ClientVersion, LoginKeys> _cache = [];

    private static LoginKeys[] _legacyKeys;

    public uint Key1 { get; }
    public uint Key2 { get; }

    private LoginKeys(uint key1, uint key2)
    {
        Key1 = key1;
        Key2 = key2;
    }

    /// <summary>
    /// Gets pre-computed login keys for all known client versions before 6.0.5
    /// that do not send the 0xEF packet (and thus have no version available at login time).
    /// </summary>
    public static ReadOnlySpan<LoginKeys> LegacyKeys => _legacyKeys ??= BuildLegacyKeys();

    /// <summary>
    /// Gets or computes encryption keys for the specified client version.
    /// Results are cached for performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LoginKeys GetKeys(ClientVersion version)
    {
        if (version == null)
        {
            return Empty;
        }

        if (_cache.TryGetValue(version, out var keys))
        {
            return keys;
        }

        keys = ComputeKeys((uint)version.Major, (uint)version.Minor, (uint)version.Revision);
        _cache[version] = keys;
        return keys;
    }

    /// <summary>
    /// Computes encryption keys from version components using the UO key derivation algorithm.
    /// </summary>
    private static LoginKeys ComputeKeys(uint major, uint minor, uint revision)
    {
        // Key1 derivation
        uint key1 = (major << 23) | (minor << 14) | (revision << 4);
        key1 ^= (revision * revision) << 9;
        key1 ^= minor * minor;
        key1 ^= (minor * 11) << 24;
        key1 ^= (revision * 7) << 19;
        key1 ^= 0x2C13A5FD;

        // Key2 derivation
        uint key2 = (major << 22) | (revision << 13) | (minor << 3);
        key2 ^= (revision * revision * 3) << 10;
        key2 ^= minor * minor;
        key2 ^= (minor * 13) << 23;
        key2 ^= (revision * 7) << 18;
        key2 ^= 0xA31D527F;

        return new LoginKeys(key1, key2);
    }

    /// <summary>
    /// Builds the static array of pre-computed keys for pre-6.0.5 client versions.
    /// These versions don't send 0xEF, so login encryption must be detected by trying known keys.
    /// </summary>
    private static LoginKeys[] BuildLegacyKeys()
    {
        // All unique (Major, Minor, Revision) tuples from 4.0.11 through 6.0.4
        ReadOnlySpan<(uint Major, uint Minor, uint Revision)> versions =
        [
            (4, 0, 11),
            (5, 0, 0), (5, 0, 1), (5, 0, 2), (5, 0, 3), (5, 0, 4),
            (5, 0, 5), (5, 0, 6), (5, 0, 7), (5, 0, 8), (5, 0, 9),
            (6, 0, 0), (6, 0, 1), (6, 0, 2), (6, 0, 3), (6, 0, 4),
        ];

        var keys = new LoginKeys[versions.Length];
        for (var i = 0; i < versions.Length; i++)
        {
            keys[i] = ComputeKeys(versions[i].Major, versions[i].Minor, versions[i].Revision);
        }

        return keys;
    }
}
