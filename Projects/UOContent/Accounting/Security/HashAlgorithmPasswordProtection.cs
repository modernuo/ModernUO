/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

namespace Server.Accounting.Security
{
    public class HashAlgorithmPasswordProtection : IPasswordProtection
    {
        public static IPasswordProtection MD5Instance = new HashAlgorithmPasswordProtection(MD5.Create());
        public static IPasswordProtection SHA1Instance = new HashAlgorithmPasswordProtection(SHA1.Create());
        public static IPasswordProtection SHA2Instance = new HashAlgorithmPasswordProtection(SHA512.Create());
        private readonly HashAlgorithm _hashAlgorithm;

        public HashAlgorithmPasswordProtection(HashAlgorithm hashAlgorithm) => _hashAlgorithm = hashAlgorithm;

        public string EncryptPassword(string plainPassword)
        {
            byte[] bytes = plainPassword.AsSpan(0, Math.Min(256, plainPassword.Length)).GetBytesAscii();
            return _hashAlgorithm.ComputeHash(bytes).ToHexString();
        }

        public bool ValidatePassword(string encryptedPassword, string plainPassword) =>
            EncryptPassword(plainPassword) == encryptedPassword;
    }
}
