using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
  internal interface IArgon2
  {
    public Argon2Error Hash(uint t_cost, uint m_cost, uint parallelism,
      ReadOnlySpan<byte> pwd,
      ReadOnlySpan<byte> salt,
      Span<byte> hash,
      Span<byte> encoded,
      int type, int version);

    Argon2Error Verify(ReadOnlySpan<byte> encoded, ReadOnlySpan<byte> pwd, int pwdlen, int type);

    // TODO: Change str to use ReadOnlySpan<char> then convert to pointer later
    Argon2Error Decode(Argon2Context ctx, ReadOnlySpan<byte> str, int type);
  }

  internal static class Argon2
  {
    internal static Argon2Error Hash(uint t_cost, uint m_cost, uint parallelism,
      ReadOnlySpan<byte> pwd,
      ReadOnlySpan<byte> salt,
      Span<byte> hash,
      Span<byte> encoded,
      int type, int version) =>
      SafeNativeMethods.argon2_hash(t_cost, m_cost, parallelism,
        in pwd.GetPinnableReference(), pwd.Length,
        in salt.GetPinnableReference(), salt.Length,
        ref hash.GetPinnableReference(), hash.Length,
        ref encoded.GetPinnableReference(), encoded.Length,
        type, version
      );

    internal static Argon2Error Verify(ReadOnlySpan<byte> encoded, ReadOnlySpan<byte> pwd, int pwdlen, int type) =>
      SafeNativeMethods.argon2_verify(in encoded.GetPinnableReference(), in pwd.GetPinnableReference(), pwdlen, type);

    internal static Argon2Error Decode(Argon2Context ctx, ReadOnlySpan<byte> str, int type) =>
      SafeNativeMethods.decode_string(ctx, in str.GetPinnableReference(), type);

    internal static class SafeNativeMethods
    {
      [DllImport("libargon2", EntryPoint = "argon2_hash")]
      internal static extern Argon2Error argon2_hash(uint t_cost, uint m_cost, uint parallelism,
        in byte pwd, int pwdlen,
        in byte salt, int saltlen,
        ref byte hash, int hashlen,
        ref byte encoded, int encodedlen,
        int type, int version
      );

      [DllImport("libargon2", EntryPoint = "argon2_verify")]
      internal static extern Argon2Error argon2_verify(in byte encoded, in byte pwd, int pwdlen, int type);

      [DllImport("libargon2", EntryPoint = "decode_string")]
      internal static extern Argon2Error decode_string(Argon2Context ctx, in byte str, int type);
    }
  }
}
