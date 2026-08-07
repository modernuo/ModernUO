/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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

namespace Server.Accounting.Security;

public class Argon2PasswordProtection : IPasswordProtection
{
    public static IPasswordProtection Instance = new Argon2PasswordProtection();

    // Argon2id over Argon2i: RFC 9106 recommends Argon2i only where side-channel resistance is
    // required and memory is scarce. 16 MiB at t=1 measures cheaper than the old 8 MiB at t=3
    // (8.5 ms vs 10.1 ms) while doubling memory-hardness, which is the property that resists GPU
    // and ASIC cracking; iterations mostly buy wall-clock. p=1 because native argon2 spawns a
    // thread per lane, which is oversubscription on the 1-2 core hosts this path exists to serve.
    private readonly Argon2PasswordHasher _passwordHasher = new(
        time: 1,
        memory: 16384,
        parallel: 1,
        type: Argon2Type.Argon2id,
        rng: RandomNumberGenerator.Create()
    );

    public string EncryptPassword(string plainPassword) =>
        _passwordHasher.Hash(plainPassword);

    public bool ValidatePassword(string encryptedPassword, string plainPassword) =>
        _passwordHasher.Verify(encryptedPassword, plainPassword);
}
