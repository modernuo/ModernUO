using System;

namespace Server.Accounting.Security
{
  /// <summary>
  /// HashMetadata represents the information stored in the encoded Argon2 format
  /// </summary>
  public class HashMetadata
  {
    /// <summary>
    /// The type of Argon2 hashing algorithm to use
    /// Argon2d - The memory access is dependent upon the hash value (vulnerable to side-channel attacks)
    /// Argon2i - The memory access is independent upon the hash value (safe from side-channel atacks)
    /// </summary>
    public Argon2Type ArgonType { get; set; }

    /// <summary>
    /// How much memory to use while hashing in kibibytes (KiB)
    /// </summary>
    public uint MemoryCost { get; set; }

    /// <summary>
    /// How many iterations of the Argon2 hash to perform
    /// </summary>
    public uint TimeCost { get; set; }

    /// <summary>
    /// How many threads to use while hashing
    /// </summary>
    public uint Parallelism { get; set; }

    /// <summary>
    /// The raw bytes of the salt
    /// </summary>
    public byte[] Salt { get; set; }

    /// <summary>
    /// The raw bytes of the hash
    /// </summary>
    public byte[] Hash { get; set; }


    /// <summary>
    /// A base-64 encoded string of the salt, minus the padding (=) characters
    /// </summary>
    public string GetBase64Salt() => Convert.ToBase64String(Salt).Replace("=", "");

    /// <summary>
    /// A base-64 encoded string of the hash, minus the padding (=) characters
    /// </summary>
    public string GetBase64Hash() => Convert.ToBase64String(Hash).Replace("=", "");


    /// <summary>
    /// Converts HashMetadata back into the original Argon2 formatted string.
    /// </summary>
    public override string ToString() =>
      $"$argon2{(ArgonType == Argon2Type.Argon2i ? "i" : "d")}$v=19$m={MemoryCost},t={TimeCost},p={Parallelism}${GetBase64Salt()}${GetBase64Hash()}";
  }
}
