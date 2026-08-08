/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AccountSecurity.cs                                              *
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

namespace Server.Accounting.Security;

public enum PasswordProtectionAlgorithm
{
    // Obsolete algorithms from RunUO. These are not secure!
    // They are included for password upgrades only.
    None,
    MD5,
    SHA1,

    // Supported algorithms
    SHA2, // ServUO compatibility
    PBKDF2,
    Argon2 // Recommended algorithm for real security.
}

public static class AccountSecurity
{
    public static PasswordProtectionAlgorithm CurrentAlgorithm { get; set; }

    /// <summary>
    /// Shard-wide master switch for the one-time repair of accounts whose password was corrupted by
    /// the pre-fix SetPassword, which stored the credential under the wrong family's phrase rule.
    /// This flag alone repairs nothing: an account is only repaired when it *also* carries the
    /// <see cref="Account.RepairPasswordTag"/> tag, which an operator adds from the admin gump
    /// (Account Details -> Tags -> Add Tag) and which is cleared automatically once the repair
    /// succeeds. Tag only an account whose owner has actually reported being locked out, never one
    /// that can still log in: the repair cannot distinguish a mis-migrated hash from a password that
    /// merely begins with the username, so tagging a working account risks rewriting its credential
    /// down to whatever was submitted. Off by default because the repair costs a second verify on a
    /// failed login -- for tagged accounts only, so the credential-stuffing surface is unaffected by
    /// the flag on its own. Turn it on for a migration window, then off again.
    /// </summary>
    public static bool RepairMigratedPasswords { get; set; }

    public static IPasswordProtection CurrentPasswordProtection => GetPasswordProtection(CurrentAlgorithm);

    public static void Configure()
    {
        CurrentAlgorithm =
            ServerConfiguration.GetOrUpdateSetting(
                "accountSecurity.encryptionAlgorithm",
                PasswordProtectionAlgorithm.Argon2
            );

        RepairMigratedPasswords =
            ServerConfiguration.GetOrUpdateSetting("accountSecurity.repairMigratedPasswords", false);

        if (CurrentAlgorithm < PasswordProtectionAlgorithm.SHA2)
        {
            throw new Exception($"Security: {CurrentAlgorithm} is obsolete and not secure. Do not use it.");
        }
    }

    public static IPasswordProtection GetPasswordProtection(PasswordProtectionAlgorithm algorithm)
    {
        var passwordProtection = algorithm switch
        {
            PasswordProtectionAlgorithm.MD5    => HashAlgorithmPasswordProtection.MD5Instance,
            PasswordProtectionAlgorithm.SHA1   => HashAlgorithmPasswordProtection.SHA1Instance,
            PasswordProtectionAlgorithm.SHA2   => HashAlgorithmPasswordProtection.SHA2Instance,
            PasswordProtectionAlgorithm.PBKDF2 => PBKDF2PasswordProtection.Instance,
            PasswordProtectionAlgorithm.Argon2 => Argon2PasswordProtection.Instance,
            PasswordProtectionAlgorithm.None   => throw new Exception("Do not use PasswordProtectionAlgorithm.None"),
            _                                  => throw new Exception("No algorithm")
        };

        return passwordProtection;
    }
}
