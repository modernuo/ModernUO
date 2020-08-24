/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MD5PasswordProtection.cs                                        *
 * Created: 2020/05/01 - Updated: 2020/05/02                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Security.Cryptography;
using System.Text;
using Server.Misc;

namespace Server.Accounting.Security
{
    public class MD5PasswordProtection : IPasswordProtection
    {
        public static IPasswordProtection Instance = new MD5PasswordProtection();
        private MD5CryptoServiceProvider m_MD5HashProvider = new MD5CryptoServiceProvider();

        public string EncryptPassword(string plainPassword)
        {
            ReadOnlySpan<char> password = plainPassword.AsSpan(0, Math.Min(256, plainPassword.Length));
            byte[] bytes = new byte[Encoding.ASCII.GetByteCount(password)];
            Encoding.ASCII.GetBytes(password, bytes);

            return HexStringConverter.GetString(m_MD5HashProvider.ComputeHash(bytes));
        }

        public bool ValidatePassword(string encryptedPassword, string plainPassword) =>
            EncryptPassword(plainPassword) == encryptedPassword;
    }
}
