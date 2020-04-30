namespace Server.Accounting
{
  public enum PasswordProtectionAlgorithm
  {
    PBKDF2
  }

  public static class AccountSecurity
  {
    // TODO: Put it in a configuration
    public const PasswordProtectionAlgorithm AlgorithmName = PasswordProtectionAlgorithm.PBKDF2;

    public static readonly IPasswordProtection CurrentPasswordProtection = GetPasswordProtection(AlgorithmName);

    public static IPasswordProtection GetPasswordProtection(PasswordProtectionAlgorithm algorithm)
    {
      var passwordProtection = algorithm switch
      {
        PasswordProtectionAlgorithm.PBKDF2 => PBKDF2PasswordProtection.Instance,
        _ => null
      };

      return passwordProtection;
    }
  }
}
