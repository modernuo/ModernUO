/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PBKDF2PasswordProtection.cs                                     *
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
using System.Security.Cryptography;
using Server.Text;

namespace Server.Accounting.Security;

public class PBKDF2PasswordProtection : IPasswordProtection
{
    private const ushort MinIterations = 1024;
    private const ushort MaxIterations = 1536;
    private const int SaltSize = 8;
    private const int HashSize = 32;
    private const int OutputSize = 2 + SaltSize + HashSize;
    public static readonly IPasswordProtection Instance = new PBKDF2PasswordProtection();

    public string EncryptPassword(string plainPassword)
    {
        Span<byte> output = stackalloc byte[OutputSize];

        var iterations = RandomNumberGenerator.GetInt32(MinIterations, MaxIterations + 1);
        BinaryPrimitives.WriteUInt16LittleEndian(output[..2], (ushort)iterations);

        var salt = output.Slice(2, SaltSize);
        RandomNumberGenerator.Fill(salt);

        var hash = output.Slice(2 + SaltSize, HashSize);
        Rfc2898DeriveBytes.Pbkdf2(plainPassword, salt, hash, iterations, HashAlgorithmName.SHA256);

        return output.ToHexString();
    }

    public bool ValidatePassword(string encryptedPassword, string plainPassword)
    {
        Span<byte> encryptedBytes = stackalloc byte[OutputSize];
        encryptedPassword.GetBytes(encryptedBytes);

        var iterations = BinaryPrimitives.ReadUInt16LittleEndian(encryptedBytes[..2]);
        var salt = encryptedBytes.Slice(2, SaltSize);

        Span<byte> hash = stackalloc byte[HashSize];
        Rfc2898DeriveBytes.Pbkdf2(plainPassword, salt, hash, iterations, HashAlgorithmName.SHA256);

        return hash.SequenceEqual(encryptedBytes[(SaltSize + 2)..]);
    }
}
