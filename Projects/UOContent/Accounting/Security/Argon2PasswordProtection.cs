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

    // 16 MiB at t=1 is cheaper than 8 MiB at t=3 (8.5 ms vs 10.1 ms) and twice as memory-hard, which
    // is what resists GPU and ASIC cracking. p=1: native argon2 spawns a thread per lane.
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

    // Verification uses the parameters embedded in the PHC string, not the configured ones, so
    // comparing them is what lets a parameter change reach existing accounts.
    public bool NeedsRehash(string encryptedPassword)
    {
        // Unparseable but verified: a format this build does not understand, so rewrite it.
        if (!Argon2PasswordHasher.TryExtractMetadataValues(encryptedPassword, out var values))
        {
            return true;
        }

        return values.ArgonType != _passwordHasher.ArgonType
               || values.MemoryCost != _passwordHasher.MemoryCost
               || values.TimeCost != _passwordHasher.TimeCost
               || values.Parallelism != _passwordHasher.Parallelism
               || values.HashLength != (int)_passwordHasher.HashLength
               || values.SaltLength != (int)_passwordHasher.SaltLength;
    }
}
