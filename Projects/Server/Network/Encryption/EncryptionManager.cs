/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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
    /// Gets whether encrypted clients are accepted. When false, a login's first byte is enough to
    /// tell plaintext from garbage; when true, it is not.
    /// </summary>
    public static bool AcceptsEncrypted => _mode.HasFlag(EncryptionMode.Encrypted);

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

    // A plaintext 0x80 holds NUL-terminated 30-byte username and password fields.
    private static bool IsPlainLogin(ReadOnlySpan<byte> buffer) =>
        buffer[0] == 0x80 && buffer[30] == 0x00 && buffer[60] == 0x00;

    // The game connection's seed is the auth id, and GameLogin rejects a 0x91 whose auth id differs.
    private static bool IsPlainGameLogin(ReadOnlySpan<byte> buffer, int seed) =>
        buffer[0] == 0x91 && BinaryPrimitives.ReadInt32BigEndian(buffer[1..]) == seed;

    /// <param name="ns">The network state.</param>
    extension(NetState ns)
    {
        /// <summary>
        /// Tells from its first five bytes whether the first packet on a game connection is a game
        /// login (0x91), plain or encrypted.
        /// </summary>
        /// <param name="buffer">At least the first five bytes received.</param>
        public bool IsGameLoginStart(ReadOnlySpan<byte> buffer)
        {
            if (IsPlainGameLogin(buffer, ns.Seed))
            {
                return true;
            }

            if (!_mode.HasFlag(EncryptionMode.Encrypted))
            {
                return false;
            }

            Span<byte> header = stackalloc byte[5];
            buffer[..5].CopyTo(header);
            new GameEncryption((uint)ns.Seed).ClientDecrypt(header);
            return IsPlainGameLogin(header, ns.Seed);
        }

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

            // A first byte of 0x80 alone is not plaintext: the first login keystream byte is the seed's
            // low byte ^ 0xAA, so an encrypted 0x80 also starts with 0x80 for one seed in 256.
            if (IsPlainLogin(buffer) && _mode.HasFlag(EncryptionMode.Unencrypted))
            {
                if (_debug)
                {
                    logger.Debug("Client {Address}: Unencrypted login detected", ns.Address);
                }

                return true;
            }

            // Attempt decryption with version-derived keys
            if (_mode.HasFlag(EncryptionMode.Encrypted) &&
                LoginEncryption.TryDecrypt(ns.Version, (uint)ns.Seed, buffer, out var loginEncryption))
            {
                encryption = loginEncryption;

                if (_debug)
                {
                    logger.Debug("Client {Address}: Encrypted login detected (version: {Version})", ns.Address, ns.Version);
                }

                return true;
            }

            // A 30-character username or password leaves no NUL at byte 30 or 60; accept those
            // plaintext logins as before, now that decryption has ruled out an encrypted client.
            if (buffer[0] == 0x80 && _mode.HasFlag(EncryptionMode.Unencrypted))
            {
                if (_debug)
                {
                    logger.Debug("Client {Address}: Unencrypted login detected", ns.Address);
                }

                return true;
            }

            if (_debug)
            {
                logger.Debug("Client {Address}: Login encryption detection failed (mode: {Mode})", ns.Address, _mode);
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

            // A first byte of 0x91 alone is not plaintext: about one auth id in 256 encrypts 0x91 to 0x91.
            if (IsPlainGameLogin(buffer, ns.Seed) && _mode.HasFlag(EncryptionMode.Unencrypted))
            {
                if (_debug)
                {
                    logger.Debug("Client {Address}: Unencrypted game login detected", ns.Address);
                }

                return true;
            }

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
