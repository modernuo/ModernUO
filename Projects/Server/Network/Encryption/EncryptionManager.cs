/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EncryptionManager.cs                                            *
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
using System.Buffers.Binary;
using Server.Logging;

namespace Server.Network;

/// <summary>
/// Manages encryption detection and configuration for client connections.
/// </summary>
public static class EncryptionManager
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(EncryptionManager));

    private static EncryptionMode _mode = EncryptionMode.None;
    private static bool _debug;

    /// <summary>
    /// Gets whether encryption handling is enabled.
    /// </summary>
    public static bool Enabled => _mode != EncryptionMode.None;

    /// <summary>
    /// Gets the current encryption mode.
    /// </summary>
    public static EncryptionMode Mode => _mode;

    /// <summary>
    /// Gets whether debug logging is enabled for encryption.
    /// </summary>
    public static bool Debug => _debug;

    /// <summary>
    /// Configures encryption settings from server configuration.
    /// </summary>
    public static void Configure()
    {
        _mode = ServerConfiguration.GetSetting("network.encryptionMode", EncryptionMode.Both);
        _debug = ServerConfiguration.GetSetting("network.encryptionDebug", false);

        if (_mode != EncryptionMode.None)
        {
            logger.Information("Encryption support enabled: {Mode}", _mode);
        }
    }

    /// <param name="ns">The network state.</param>
    extension(NetState ns)
    {
        /// <summary>
        /// Detects and initializes encryption for a login packet (0x80).
        /// </summary>
        /// <param name="buffer">The 62-byte login packet buffer.</param>
        /// <param name="encryption">The detected encryption, or null if unencrypted.</param>
        /// <returns>True if detection succeeded (encrypted or unencrypted), false if rejected.</returns>
        public bool DetectLoginEncryption(ReadOnlySpan<byte> buffer, out IClientEncryption encryption)
        {
            encryption = null;

            if (buffer.Length < 62)
            {
                return false;
            }

            // Check if unencrypted:
            // - Packet ID is 0x80, OR
            // - Username and password null terminators are present
            var isUnencrypted = buffer[0] == 0x80 || buffer[30] == 0x00 && buffer[60] == 0x00;

            if (isUnencrypted)
            {
                if (!_mode.HasFlag(EncryptionMode.Unencrypted))
                {
                    if (_debug)
                    {
                        logger.Debug("Client {Address}: Unencrypted login rejected (mode: {Mode})", ns.Address, _mode);
                    }
                    return false;
                }

                if (_debug)
                {
                    logger.Debug("Client {Address}: Unencrypted login detected", ns.Address);
                }

                return true;
            }

            // Try encrypted
            if (!_mode.HasFlag(EncryptionMode.Encrypted))
            {
                if (_debug)
                {
                    logger.Debug("Client {Address}: Encrypted login rejected (mode: {Mode})", ns.Address, _mode);
                }
                return false;
            }

            // Attempt decryption with version-derived keys
            if (LoginEncryption.TryDecrypt(ns.Version, (uint)ns.Seed, buffer, out var loginEncryption))
            {
                encryption = loginEncryption;

                if (_debug)
                {
                    logger.Debug("Client {Address}: Encrypted login detected (version: {Version})", ns.Address, ns.Version);
                }

                return true;
            }

            if (_debug)
            {
                logger.Debug("Client {Address}: Login encryption detection failed", ns.Address);
            }

            return false;
        }

        /// <summary>
        /// Detects and initializes encryption for a game server login packet (0x91).
        /// </summary>
        /// <param name="buffer">The 65-byte game login packet buffer.</param>
        /// <param name="encryption">The detected encryption, or null if unencrypted.</param>
        /// <returns>True if detection succeeded (encrypted or unencrypted), false if rejected.</returns>
        public bool DetectGameEncryption(ReadOnlySpan<byte> buffer, out IClientEncryption encryption)
        {
            encryption = null;

            if (buffer.Length < 65)
            {
                return false;
            }

            // Extract auth ID from packet (bytes 1-4, big-endian)
            var authId = BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);

            // Check if unencrypted:
            // - Packet ID is 0x91, OR
            // - Auth ID equals seed (indicates no encryption applied)
            var isUnencrypted = buffer[0] == 0x91 || authId == (uint)ns.Seed;

            if (isUnencrypted)
            {
                if (!_mode.HasFlag(EncryptionMode.Unencrypted))
                {
                    if (_debug)
                    {
                        logger.Debug("Client {Address}: Unencrypted game login rejected (mode: {Mode})", ns.Address, _mode);
                    }
                    return false;
                }

                if (_debug)
                {
                    logger.Debug("Client {Address}: Unencrypted game login detected", ns.Address);
                }

                return true;
            }

            // Try encrypted
            if (!_mode.HasFlag(EncryptionMode.Encrypted))
            {
                if (_debug)
                {
                    logger.Debug("Client {Address}: Encrypted game login rejected (mode: {Mode})", ns.Address, _mode);
                }
                return false;
            }

            // Attempt decryption with seed-derived Twofish
            if (GameEncryption.TryDecrypt((uint)ns.Seed, buffer, out var gameEncryption))
            {
                encryption = gameEncryption;

                if (_debug)
                {
                    logger.Debug("Client {Address}: Encrypted game login detected (seed: 0x{Seed:X8})", ns.Address, ns.Seed);
                }

                return true;
            }

            if (_debug)
            {
                logger.Debug("Client {Address}: Game encryption detection failed", ns.Address);
            }

            return false;
        }
    }
}
