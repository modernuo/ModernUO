using System;
using System.Runtime.CompilerServices;

namespace Server
{
  public class SplitMix64
  {
    private ulong x;

    public SplitMix64(ulong seed) => x = seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Next() {
      ulong z = x += 0x9e3779b97f4a7c15;
      z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9;
      z = (z ^ (z >> 27)) * 0x94d049bb133111eb;
      return z ^ (z >> 31);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillArray(ulong[] arr)
    {
      if (arr == null)
        return;

      for(int i = 0; i < arr.Length; i++) arr[i] = Next();
    }
  }
}
