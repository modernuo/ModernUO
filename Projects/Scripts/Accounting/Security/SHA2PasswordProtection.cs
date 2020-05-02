using System;
using System.Security.Cryptography;
using System.Text;
using Server.Misc;

namespace Server.Accounting.Security
{
  public class SHA2PasswordProtection : IPasswordProtection
  {
    public static IPasswordProtection Instance = new SHA2PasswordProtection();
    private SHA512CryptoServiceProvider m_SHA2HashProvider = new SHA512CryptoServiceProvider();

    public string EncryptPassword(string plainPassword)
    {
      ReadOnlySpan<char> password = plainPassword.AsSpan(0, Math.Min(256, plainPassword.Length));
      byte[] bytes = new byte[Encoding.ASCII.GetByteCount(password)];
      Encoding.ASCII.GetBytes(password, bytes);

      return HexStringConverter.GetString(m_SHA2HashProvider.ComputeHash(bytes));
    }

    public bool ValidatePassword(string encryptedPassword, string plainPassword) =>
      EncryptPassword(plainPassword) == encryptedPassword;
  }
}
