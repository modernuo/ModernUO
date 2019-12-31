/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: DRng64.cs - Created: 2019/12/30 - Updated: 2019/12/30           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Runtime.InteropServices;

namespace Server
{
  public sealed class DRng64 : BaseRandom, IHardwareRNG
  {
    [DllImport("libdrng", CallingConvention = CallingConvention.Cdecl)]
    internal static extern RDRandError rdrand_64(ref ulong rand, bool retry);

    [DllImport("libdrng", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe RDRandError rdrand_get_bytes(int n, byte* buffer);

    public bool IsSupported()
    {
      ulong r = 0;
      return rdrand_64(ref r, true) == RDRandError.Success;
    }

    internal override unsafe void GetBytes(Span<byte> b)
    {
      fixed (byte* ptr = b)
        rdrand_get_bytes(b.Length, ptr);
    }

    internal override void GetBytes(byte[] b, int offset, int count)
    {
      GetBytes(b.AsSpan(offset, count));
    }
  }

  public enum RDRandError
  {
    Unknown = -4,
    Unsupported = -3,
    Supported = -2,
    NotReady = -1,

    Failure = 0,

    Success = 1
  }
}
