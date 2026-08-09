/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: HashAlgorithmPasswordProtection.cs                              *
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
using System.Security.Cryptography;
using Server.Text;

namespace Server.Accounting.Security;

/// <summary>
/// The obsolete unsalted digests, kept only so imported accounts can log in once and be upgraded.
///
/// Hashing goes through the one-shot static APIs rather than a retained <see cref="HashAlgorithm"/>.
/// A <see cref="HashAlgorithm"/> instance carries the running digest across HashCore/HashFinal, so
/// two threads sharing one corrupt each other's result -- and these are process-wide singletons.
/// The static form has no such state, allocates nothing, and produces identical bytes.
/// </summary>
public class HashAlgorithmPasswordProtection : IPasswordProtection
{
    private enum Kind
    {
        MD5,
        SHA1,
        SHA512
    }

    public static readonly IPasswordProtection MD5Instance = new HashAlgorithmPasswordProtection(Kind.MD5);
    public static readonly IPasswordProtection SHA1Instance = new HashAlgorithmPasswordProtection(Kind.SHA1);
    public static readonly IPasswordProtection SHA2Instance = new HashAlgorithmPasswordProtection(Kind.SHA512);

    private const int MaxDigestLength = 64; // SHA512, the largest of the three.

    private readonly Kind _kind;

    private HashAlgorithmPasswordProtection(Kind kind) => _kind = kind;

    public string EncryptPassword(string plainPassword)
    {
        var bytes = plainPassword.AsSpan(0, Math.Min(256, plainPassword.Length)).GetBytesAscii();

        Span<byte> digest = stackalloc byte[MaxDigestLength];

        var written = _kind switch
        {
            Kind.MD5  => MD5.HashData(bytes, digest),
            Kind.SHA1 => SHA1.HashData(bytes, digest),
            _         => SHA512.HashData(bytes, digest)
        };

        return digest[..written].ToHexString();
    }

    public bool ValidatePassword(string encryptedPassword, string plainPassword) =>
        EncryptPassword(plainPassword) == encryptedPassword;
}
