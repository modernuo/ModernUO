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
    internal static readonly bool IsDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    internal static readonly bool IsFreeBSD = RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    internal static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    internal static readonly IArgon2 Library =
      IsLinux || IsFreeBSD || IsDarwin ? (IArgon2)new UnixArgon2() : new WindowsArgon2();
  }

  internal class WindowsArgon2 : IArgon2
  {
    public Argon2Error Hash(uint t_cost, uint m_cost, uint parallelism,
      ReadOnlySpan<byte> pwd,
      ReadOnlySpan<byte> salt,
      Span<byte> hash,
      Span<byte> encoded,
      int type, int version) =>
      SafeNativeMethods.crypto_argon2_hash(t_cost, m_cost, parallelism,
        in pwd.GetPinnableReference(), pwd.Length,
        in salt.GetPinnableReference(), salt.Length,
        ref hash.GetPinnableReference(), hash.Length,
        ref encoded.GetPinnableReference(), encoded.Length,
        type, version
      );

    public Argon2Error Verify(ReadOnlySpan<byte> encoded, ReadOnlySpan<byte> pwd, int pwdlen, int type) =>
      SafeNativeMethods.crypto_argon2_verify(in encoded.GetPinnableReference(), in pwd.GetPinnableReference(), pwdlen, type);

    public Argon2Error Decode(Argon2Context ctx, ReadOnlySpan<byte> str, int type) =>
      SafeNativeMethods.crypto_decode_string(ctx, in str.GetPinnableReference(), type);

    internal static class SafeNativeMethods
    {
      [DllImport("argon2.dll", EntryPoint = "crypto_argon2_hash", CallingConvention = CallingConvention.Cdecl)]
      internal static extern Argon2Error crypto_argon2_hash(uint t_cost, uint m_cost, uint parallelism,
        in byte pwd, int pwdlen,
        in byte salt, int saltlen,
        ref byte hash, int hashlen,
        ref byte encoded, int encodedlen,
        int type, int version
      );

      [DllImport("argon2.dll", EntryPoint = "crypto_argon2_verify", CallingConvention = CallingConvention.Cdecl)]
      internal static extern Argon2Error crypto_argon2_verify(in byte encoded, in byte pwd, int pwdlen, int type);

      [DllImport("argon2.dll", EntryPoint = "crypto_decode_string", CallingConvention = CallingConvention.Cdecl)]
      internal static extern Argon2Error crypto_decode_string(Argon2Context ctx, in byte str, int type);
    }
  }

  internal class UnixArgon2 : IArgon2
  {
    public Argon2Error Hash(uint t_cost, uint m_cost, uint parallelism,
      ReadOnlySpan<byte> pwd,
      ReadOnlySpan<byte> salt,
      Span<byte> hash,
      Span<byte> encoded,
      int type, int version) =>
      SafeNativeMethods.crypto_argon2_hash(t_cost, m_cost, parallelism,
        in pwd.GetPinnableReference(), pwd.Length,
        in salt.GetPinnableReference(), salt.Length,
        ref hash.GetPinnableReference(), hash.Length,
        ref encoded.GetPinnableReference(), encoded.Length,
        type, version
      );

    public Argon2Error Verify(ReadOnlySpan<byte> encoded, ReadOnlySpan<byte> pwd, int pwdlen, int type) =>
      SafeNativeMethods.crypto_argon2_verify(in encoded.GetPinnableReference(), in pwd.GetPinnableReference(), pwdlen, type);

    public Argon2Error Decode(Argon2Context ctx, ReadOnlySpan<byte> str, int type) =>
      SafeNativeMethods.crypto_decode_string(ctx, in str.GetPinnableReference(), type);

    internal static class SafeNativeMethods
    {
      [DllImport("libargon2", EntryPoint = "argon2_hash")]
      internal static extern Argon2Error crypto_argon2_hash(uint t_cost, uint m_cost, uint parallelism,
        in byte pwd, int pwdlen,
        in byte salt, int saltlen,
        ref byte hash, int hashlen,
        ref byte encoded, int encodedlen,
        int type, int version
      );

      [DllImport("libargon2", EntryPoint = "argon2_verify")]
      internal static extern unsafe Argon2Error crypto_argon2_verify(in byte encoded, in byte pwd, int pwdlen, int type);

      [DllImport("libargon2", EntryPoint = "decode_string")]
      internal static extern unsafe Argon2Error crypto_decode_string(Argon2Context ctx, in byte str, int type);
    }
  }
}
