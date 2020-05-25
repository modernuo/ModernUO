using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
  [StructLayout(LayoutKind.Sequential)]
  internal class Argon2Context
  {
    public IntPtr Out;
    public uint OutLen;

    public IntPtr Pwd;
    public uint PwdLen;

    public IntPtr Salt;
    public uint SaltLen;

    public IntPtr Secret;
    public uint SecretLen;

    public IntPtr AssocData;
    public uint AssocDataLen;

    public uint TimeCost;
    public uint MemoryCost;
    public uint Lanes;
    public uint Threads;

    public IntPtr AllocateCallback;
    public IntPtr FreeCallback;

    public uint Flags;
  }
}
