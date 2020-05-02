using System;

namespace Server.Accounting.Security
{
  public enum PasswordProtectionAlgorithm
  {
    // Obsolete algorithms. These are not secure!
    // They are included for password upgrades only.
    None,
    MD5,
    SHA1,

    // Support algorithms
    PBKDF2,
    Argon2 // Recommended algorithm for real security.
  }

  public static class AccountSecurity
  {
    // TODO: Put it in a configuration
    public const PasswordProtectionAlgorithm AlgorithmName = PasswordProtectionAlgorithm.Argon2;

    public static readonly IPasswordProtection CurrentPasswordProtection = GetPasswordProtection(AlgorithmName);

    public static void Configure()
    {
      if (AlgorithmName < PasswordProtectionAlgorithm.PBKDF2)
        throw new Exception($"Security: {AlgorithmName} is obselete and not secure. Do not use it.");
    }

    public static IPasswordProtection GetPasswordProtection(PasswordProtectionAlgorithm algorithm)
    {
      var passwordProtection = algorithm switch
      {
        PasswordProtectionAlgorithm.MD5 => MD5PasswordProtection.Instance,
        PasswordProtectionAlgorithm.SHA1 => SHA1PasswordProtection.Instance,
        PasswordProtectionAlgorithm.PBKDF2 => PBKDF2PasswordProtection.Instance,
        PasswordProtectionAlgorithm.Argon2 => Argon2PasswordProtection.Instance,
        _ => null
      };

      return passwordProtection;
    }
  }
}
