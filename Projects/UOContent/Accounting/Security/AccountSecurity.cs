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
    /// Enables a one-time repair for accounts whose password was corrupted by the pre-fix
    /// SetPassword, which hashed username + password but tagged it with an algorithm whose phrase
    /// rule omits the username. Off by default: the repair costs a second verify on every FAILED
    /// login, and failed logins are the credential-stuffing surface. Turn it on for a migration
    /// window, then off again.
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
