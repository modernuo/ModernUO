using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Server.Accounting.Security
{
  public class SHA1PasswordProtection : IPasswordProtection
  {
    public static IPasswordProtection Instance = new SHA1PasswordProtection();
    private SHA1CryptoServiceProvider m_SHA1HashProvider = new SHA1CryptoServiceProvider();

    public string EncryptPassword(string plainPassword)
    {
      ReadOnlySpan<char> password = plainPassword.AsSpan(0, Math.Min(256, plainPassword.Length));
      byte[] bytes = new byte[Encoding.ASCII.GetByteCount(password)];
      Encoding.ASCII.GetBytes(password, bytes);

      return BitConverter.ToString(m_SHA1HashProvider.ComputeHash(bytes));
    }

    public bool ValidatePassword(string encryptedPassword, string plainPassword) =>
      EncryptPassword(plainPassword) == encryptedPassword;
  }
}
