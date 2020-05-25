namespace System.Security.Cryptography
{
  /// <summary>
  /// The type of Argon2 hashing algorithm to use.
  /// </summary>
  public enum Argon2Type
  {
    /// <summary>
    /// The memory access is dependent upon the hash value (vulnerable to side-channel attacks)
    /// </summary>
    Argon2d = 0,

    /// <summary>
    /// The memory access is independent upon the hash value (safe from side-channel atacks)
    /// </summary>
    Argon2i = 1
  }
}
