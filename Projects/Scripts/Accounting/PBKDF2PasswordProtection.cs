using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using Server.Misc;

namespace Server.Accounting
{
  public class PBKDF2PasswordProtection : IPasswordProtection
  {
    public static PBKDF2PasswordProtection Instance = new PBKDF2PasswordProtection();

    private const ushort m_MinIterations = 1024;
    private const ushort m_MaxIterations = 1536;
    private static readonly HashAlgorithmName m_Algorithm = HashAlgorithmName.SHA256;
    private const int m_SaltSize = 8;
    private const int m_HashSize = 32;
    private const int m_OutputSize = 2 + m_SaltSize + m_HashSize;

    public string EncryptPassword(string plainPassword)
    {
      Span<byte> output = stackalloc byte[m_OutputSize];
      int iterations = Utility.RandomMinMax(m_MinIterations, m_MaxIterations);
      BinaryPrimitives.WriteUInt16LittleEndian(output.Slice(0, 2), (ushort)iterations);

      var rfc2898 = new Rfc2898DeriveBytes(plainPassword, m_SaltSize, iterations, m_Algorithm);
      rfc2898.Salt.CopyTo(output.Slice(2, m_SaltSize));
      rfc2898.GetBytes(m_HashSize).CopyTo(output.Slice(m_SaltSize + 2));

      return HexStringConverter.GetString(output);
    }

    public bool ValidatePassword(string encryptedPassword, string plainPassword)
    {
      Span<byte> encryptedBytes = stackalloc byte[m_OutputSize];
      HexStringConverter.GetBytes(encryptedPassword, encryptedBytes);

      ushort iterations = BinaryPrimitives.ReadUInt16LittleEndian(encryptedBytes.Slice(0, 2));
      Span<byte> salt = encryptedBytes.Slice(2, m_SaltSize);

      ReadOnlySpan<byte> hash =
        new Rfc2898DeriveBytes(plainPassword, salt.ToArray(), iterations, m_Algorithm).GetBytes(m_HashSize);

      return hash.SequenceEqual(encryptedBytes.Slice(m_SaltSize + 2));
    }
  }
}
