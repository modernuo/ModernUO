/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SHA1PasswordProtection.cs                                       *
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
    public class SHA1PasswordProtection : IPasswordProtection
    {
        public static IPasswordProtection Instance = new SHA1PasswordProtection();
#pragma warning disable CA5350
        private readonly SHA1CryptoServiceProvider m_SHA1HashProvider = new();
#pragma warning restore CA5350

        public string EncryptPassword(string plainPassword)
        {
            byte[] bytes = plainPassword.AsSpan(0, Math.Min(256, plainPassword.Length)).GetBytesAscii();
            return m_SHA1HashProvider.ComputeHash(bytes).ToHexString();
        }

        public bool ValidatePassword(string encryptedPassword, string plainPassword) =>
            EncryptPassword(plainPassword) == encryptedPassword;
    }
}
