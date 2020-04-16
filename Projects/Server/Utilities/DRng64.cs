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
using System.Security.Cryptography;

namespace Server
{
  public sealed class DRng64 : RandomNumberGenerator, IHardwareRNG
  {
    public enum RDRandError
    {
      Unknown = -4,
      Unsupported = -3,
      Supported = -2,
      NotReady = -1,

      Failure = 0,

      Success = 1
    }

    [DllImport("libdrng", CallingConvention = CallingConvention.Cdecl)]
    internal static extern RDRandError rdrand_64(ref ulong rand, bool retry);

    [DllImport("libdrng", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe RDRandError rdrand_get_bytes(int n, byte* buffer);

    public bool IsSupported()
    {
      ulong r = 0;
      return rdrand_64(ref r, true) == RDRandError.Success;
    }

    private static unsafe void GetBytes(byte* pBuffer, int count)
    {
      rdrand_get_bytes(count, pBuffer);
    }

    public override void GetBytes(byte[] data)
    {
      if (data == null) throw new ArgumentNullException(nameof(data));
      GetBytes(new Span<byte>(data));
    }

    public override void GetBytes(byte[] data, int offset, int count)
    {
      GetBytes(new Span<byte>(data, offset, count));
    }

    public override unsafe void GetBytes(Span<byte> data)
    {
      if (data.Length > 0)
        fixed (byte* ptr = data)
        {
          GetBytes(ptr, data.Length);
        }
    }

    public override void GetNonZeroBytes(byte[] data)
    {
      if (data == null) throw new ArgumentNullException(nameof(data));
      GetNonZeroBytes(new Span<byte>(data));
    }

    public override void GetNonZeroBytes(Span<byte> data)
    {
      while (data.Length > 0)
      {
        // Fill the remaining portion of the span with random bytes.
        GetBytes(data);

        // Find the first zero in the remaining portion.
        var indexOfFirst0Byte = data.Length;
        for (var i = 0; i < data.Length; i++)
          if (data[i] == 0)
          {
            indexOfFirst0Byte = i;
            break;
          }

        // If there were any zeros, shift down all non-zeros.
        for (var i = indexOfFirst0Byte + 1; i < data.Length; i++)
          if (data[i] != 0)
            data[indexOfFirst0Byte++] = data[i];

        // Request new random bytes if necessary; dont re-use
        // existing bytes since they were shifted down.
        data = data.Slice(indexOfFirst0Byte);
      }
    }
  }
}
