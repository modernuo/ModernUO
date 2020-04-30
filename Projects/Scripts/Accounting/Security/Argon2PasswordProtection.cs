namespace Server.Accounting.Security
{
  public class Argon2PasswordProtection : IPasswordProtection
  {
    public static IPasswordProtection Instance = new Argon2PasswordProtection();
    private Argon2PasswordHasher m_PasswordHasher = new Argon2PasswordHasher();

    public string EncryptPassword(string plainPassword) =>
      m_PasswordHasher.Hash(plainPassword);

    public bool ValidatePassword(string encryptedPassword, string plainPassword) =>
      m_PasswordHasher.Verify(encryptedPassword, plainPassword);
  }
}
