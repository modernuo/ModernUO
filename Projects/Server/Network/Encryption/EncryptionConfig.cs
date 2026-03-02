/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EncryptionConfig.cs                                             *
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
/// Specifies which encryption modes the server will accept.
/// </summary>
[Flags]
public enum EncryptionMode
{
    /// <summary>
    /// Encryption handling is disabled. Current behavior.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// Accept unencrypted clients (e.g., ClassicUO with encryption disabled).
    /// </summary>
    Unencrypted = 0x1,

    /// <summary>
    /// Accept encrypted clients (original UO client, Enhanced Client).
    /// </summary>
    Encrypted = 0x2,

    /// <summary>
    /// Auto-detect and accept both encrypted and unencrypted clients.
    /// </summary>
    Both = Unencrypted | Encrypted
}
