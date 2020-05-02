using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Server.Accounting.Security
{
  internal interface IArgon2
  {
    public Argon2Error Hash(uint t_cost, uint m_cost, uint parallelism,
      byte[] pwd,
      byte[] salt,
      byte[] hash,
      byte[] encoded,
      int type, int version);

    Argon2Error Verify(byte[] encoded, byte[] pwd, int pwdlen, int type);

    // TODO: Change str to use ReadOnlySpan<char> then convert to pointer later
    Argon2Error Decode(Argon2Context ctx, string str, int type);
  }

  internal static class Argon2
  {
    internal static readonly IArgon2 Library;

    static Argon2()
    {
      if (RuntimeUtility.Unix)
        Library = new UnixArgon2();
      else
        Library = new WindowsArgon2();
    }
  }

  internal class WindowsArgon2 : IArgon2
  {
    public Argon2Error Hash(uint t_cost, uint m_cost, uint parallelism,
      byte[] pwd,
      byte[] salt,
      byte[] hash,
      byte[] encoded,
      int type, int version) =>
      NativeMethods.crypto_argon2_hash(t_cost, m_cost, parallelism,
        pwd, pwd.Length,
        salt, salt.Length,
        hash, hash.Length,
        encoded, encoded.Length,
        type, version
      );

    public Argon2Error Verify(byte[] encoded, byte[] pwd, int pwdlen, int type) =>
      NativeMethods.crypto_argon2_verify(encoded, pwd, pwdlen, type);

    public Argon2Error Decode(Argon2Context ctx, string str, int type)
    {
      byte[] bytes = new byte[str.Length];
      Encoding.ASCII.GetBytes(str, bytes);

      // TODO: Use pointers instead
      return NativeMethods.crypto_decode_string(ctx, bytes, type);
    }

    internal static class NativeMethods
    {
      [DllImport("argon2.dll", EntryPoint = "crypto_argon2_hash", CallingConvention = CallingConvention.Cdecl)]
      internal static extern Argon2Error crypto_argon2_hash(uint t_cost, uint m_cost, uint parallelism,
        byte[] pwd, int pwdlen,
        byte[] salt, int saltlen,
        byte[] hash, int hashlen,
        byte[] encoded, int encodedlen,
        int type, int version
      );

      [DllImport("argon2.dll", EntryPoint = "crypto_argon2_verify", CallingConvention = CallingConvention.Cdecl)]
      internal static extern Argon2Error crypto_argon2_verify(byte[] encoded, byte[] pwd, int pwdlen, int type);

      [DllImport("argon2.dll", EntryPoint = "crypto_decode_string", CallingConvention = CallingConvention.Cdecl)]
      internal static extern Argon2Error crypto_decode_string(Argon2Context ctx, byte[] str, int type);
    }
  }

  internal class UnixArgon2 : IArgon2
  {
    public Argon2Error Hash(uint t_cost, uint m_cost, uint parallelism,
      byte[] pwd,
      byte[] salt,
      byte[] hash,
      byte[] encoded,
      int type, int version) =>
      NativeMethods.crypto_argon2_hash(t_cost, m_cost, parallelism,
        pwd, pwd.Length,
        salt, salt.Length,
        hash, hash.Length,
        encoded, encoded.Length,
        type, version
      );

    public Argon2Error Verify(byte[] encoded, byte[] pwd, int pwdlen, int type) =>
      NativeMethods.crypto_argon2_verify(encoded, pwd, pwdlen, type);

    public Argon2Error Decode(Argon2Context ctx, string str, int type)
    {
      byte[] bytes = new byte[str.Length];
      Encoding.ASCII.GetBytes(str, bytes);

      // TODO: Use pointers instead
      return NativeMethods.crypto_decode_string(ctx, bytes, type);
    }

    internal static class NativeMethods
    {
      [DllImport("libargon2", EntryPoint = "argon2_hash")]
      internal static extern Argon2Error crypto_argon2_hash(uint t_cost, uint m_cost, uint parallelism,
        byte[] pwd, int pwdlen,
        byte[] salt, int saltlen,
        byte[] hash, int hashlen,
        byte[] encoded, int encodedlen,
        int type, int version
      );

      [DllImport("libargon2", EntryPoint = "argon2_verify")]
      internal static extern Argon2Error crypto_argon2_verify(byte[] encoded, byte[] pwd, int pwdlen, int type);

      [DllImport("libargon2", EntryPoint = "decode_string")]
      internal static extern Argon2Error crypto_decode_string(Argon2Context ctx, byte[] str, int type);
    }
  }
}
