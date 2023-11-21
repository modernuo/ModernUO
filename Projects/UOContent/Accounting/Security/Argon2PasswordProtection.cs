/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Argon2PasswordProtection.cs                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Security.Cryptography;

namespace Server.Accounting.Security
{
    public class Argon2PasswordProtection : IPasswordProtection
    {
        public static IPasswordProtection Instance = new Argon2PasswordProtection();

        private readonly Argon2PasswordHasher m_PasswordHasher = new(rng: BuiltInSecureRng.Generator);

        public string EncryptPassword(string plainPassword) =>
            m_PasswordHasher.Hash(plainPassword);

        public bool ValidatePassword(string encryptedPassword, string plainPassword) =>
            m_PasswordHasher.Verify(encryptedPassword, plainPassword);
    }
}
