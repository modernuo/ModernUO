using System;
using System.Runtime.InteropServices;

namespace Server.Accounting.Security
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
    internal static readonly IArgon2 Library;

    static Argon2()
    {
      if (RuntimeUtility.IsUnix)
        Library = new UnixArgon2();
      else
        Library = new WindowsArgon2();
    }
  }

  internal class WindowsArgon2 : IArgon2
  {
    public unsafe Argon2Error Hash(uint t_cost, uint m_cost, uint parallelism,
      ReadOnlySpan<byte> pwd,
      ReadOnlySpan<byte> salt,
      Span<byte> hash,
      Span<byte> encoded,
      int type, int version)
    {
      fixed (byte* pwdPtr = &MemoryMarshal.GetReference(pwd))
      fixed (byte* saltPtr = &MemoryMarshal.GetReference(salt))
      fixed (byte* hashPtr = &MemoryMarshal.GetReference(hash))
      fixed (byte* encodedPtr = &MemoryMarshal.GetReference(encoded))
        return NativeMethods.crypto_argon2_hash(t_cost, m_cost, parallelism,
          pwdPtr, pwd.Length,
          saltPtr, salt.Length,
          hashPtr, hash.Length,
          encodedPtr, encoded.Length,
          type, version
        );
    }

    public unsafe Argon2Error Verify(ReadOnlySpan<byte> encoded, ReadOnlySpan<byte> pwd, int pwdlen, int type)
    {
      fixed (byte* encodedPtr = &MemoryMarshal.GetReference(encoded))
      fixed (byte* pwdPtr = &MemoryMarshal.GetReference(pwd))
        return NativeMethods.crypto_argon2_verify(encodedPtr, pwdPtr, pwdlen, type);
    }

    public unsafe Argon2Error Decode(Argon2Context ctx, ReadOnlySpan<byte> str, int type)
    {
      fixed (byte* strPtr = &MemoryMarshal.GetReference(str))
        return NativeMethods.crypto_decode_string(ctx, strPtr, type);
    }

    internal static class NativeMethods
    {
      [DllImport("argon2.dll", EntryPoint = "crypto_argon2_hash", CallingConvention = CallingConvention.Cdecl)]
      internal static extern unsafe Argon2Error crypto_argon2_hash(uint t_cost, uint m_cost, uint parallelism,
        byte* pwd, int pwdlen,
        byte* salt, int saltlen,
        byte* hash, int hashlen,
        byte* encoded, int encodedlen,
        int type, int version
      );

      [DllImport("argon2.dll", EntryPoint = "crypto_argon2_verify", CallingConvention = CallingConvention.Cdecl)]
      internal static extern unsafe Argon2Error crypto_argon2_verify(byte* encoded, byte* pwd, int pwdlen, int type);

      [DllImport("argon2.dll", EntryPoint = "crypto_decode_string", CallingConvention = CallingConvention.Cdecl)]
      internal static extern unsafe Argon2Error crypto_decode_string(Argon2Context ctx, byte* str, int type);
    }
  }

  internal class UnixArgon2 : IArgon2
  {
    public unsafe Argon2Error Hash(uint t_cost, uint m_cost, uint parallelism,
      ReadOnlySpan<byte> pwd,
      ReadOnlySpan<byte> salt,
      Span<byte> hash,
      Span<byte> encoded,
      int type, int version)
    {
      fixed (byte* pwdPtr = &MemoryMarshal.GetReference(pwd))
      fixed (byte* saltPtr = &MemoryMarshal.GetReference(salt))
      fixed (byte* hashPtr = &MemoryMarshal.GetReference(hash))
      fixed (byte* encodedPtr = &MemoryMarshal.GetReference(encoded))
        return NativeMethods.crypto_argon2_hash(t_cost, m_cost, parallelism,
          pwdPtr, pwd.Length,
          saltPtr, salt.Length,
          hashPtr, hash.Length,
          encodedPtr, encoded.Length,
          type, version
        );
    }

    public unsafe Argon2Error Verify(ReadOnlySpan<byte> encoded, ReadOnlySpan<byte> pwd, int pwdlen, int type)
    {
      fixed (byte* encodedPtr = &MemoryMarshal.GetReference(encoded))
      fixed (byte* pwdPtr = &MemoryMarshal.GetReference(pwd))
        return NativeMethods.crypto_argon2_verify(encodedPtr, pwdPtr, pwdlen, type);
    }

    public unsafe Argon2Error Decode(Argon2Context ctx, ReadOnlySpan<byte> str, int type)
    {
      fixed (byte* strPtr = &MemoryMarshal.GetReference(str))
        return NativeMethods.crypto_decode_string(ctx, strPtr, type);
    }

    internal static class NativeMethods
    {
      [DllImport("libargon2", EntryPoint = "argon2_hash")]
      internal static extern unsafe Argon2Error crypto_argon2_hash(uint t_cost, uint m_cost, uint parallelism,
        byte* pwd, int pwdlen,
        byte* salt, int saltlen,
        byte* hash, int hashlen,
        byte* encoded, int encodedlen,
        int type, int version
      );

      [DllImport("libargon2", EntryPoint = "argon2_verify")]
      internal static extern unsafe Argon2Error crypto_argon2_verify(byte* encoded, byte* pwd, int pwdlen, int type);

      [DllImport("libargon2", EntryPoint = "decode_string")]
      internal static extern unsafe Argon2Error crypto_decode_string(Argon2Context ctx, byte* str, int type);
    }
  }
}
