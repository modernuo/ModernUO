/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: CSPRng.cs - Created: 2019/12/30 - Updated: 2019/12/30           *
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
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
  public sealed class CSPRng : BaseRandom
  {
    private static int BUFFER_SIZE = 0x4000;
    private static int LARGE_REQUEST = 0x40;
    private byte[] _Buffer = new byte[BUFFER_SIZE];
    private RNGCryptoServiceProvider _CSP = new RNGCryptoServiceProvider();

    private ManualResetEvent _filled = new ManualResetEvent(false);

    private int _Index;

    private object _sync = new object();

    private byte[] _Working = new byte[BUFFER_SIZE];

    public CSPRng()
    {
      _CSP.GetBytes(_Working);
      Task.Run(Fill);
    }

    public override void NextBytes(Span<byte> b)
    {
      int c = b.Length;

      if (c >= LARGE_REQUEST)
      {
        lock (_sync)
        {
          _CSP.GetBytes(b);
        }

        return;
      }

      GetBytes(b);
    }

    private void CheckSwap(int c)
    {
      if (_Index + c < BUFFER_SIZE)
        return;

      _filled.WaitOne();

      byte[] b = _Working;
      _Working = _Buffer;
      _Buffer = b;
      _Index = 0;

      _filled.Reset();

      Task.Run(Fill);
    }

    private void Fill()
    {
      _CSP.GetBytes(_Buffer);
      _filled.Set();
    }

    internal override void GetBytes(Span<byte> b)
    {
      int c = b.Length;

      lock (_sync)
      {
        CheckSwap(c);
        _Working.AsSpan(0, c).CopyTo(b);
        _Index += c;
      }
    }

    internal override void GetBytes(byte[] b, int offset, int count)
    {
      GetBytes(b.AsSpan(offset, count));
    }

    public override byte NextByte()
    {
      lock (_sync)
      {
        CheckSwap(1);
        return _Working[_Index++];
      }
    }
  }
}
