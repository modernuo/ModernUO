/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IClientEncryption.cs                                            *
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
/// Interface for client encryption implementations.
/// Uses Span-based API for zero-allocation in the hot path.
/// </summary>
public interface IClientEncryption
{
    /// <summary>
    /// Decrypts incoming data from the client (in-place).
    /// </summary>
    /// <param name="buffer">The buffer containing encrypted data. Modified in-place.</param>
    void ClientDecrypt(Span<byte> buffer);

    /// <summary>
    /// Encrypts outgoing data to the client (in-place).
    /// </summary>
    /// <param name="buffer">The buffer containing plaintext data. Modified in-place.</param>
    void ServerEncrypt(Span<byte> buffer);
}
