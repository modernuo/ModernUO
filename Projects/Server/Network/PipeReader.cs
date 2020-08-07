/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: PipeReader.cs                                                   *
 * Created: 2020/08/05 - Updated: 2020/08/07                             *
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
using System.Runtime.CompilerServices;

namespace Server.Network
{
  public class PipeReader : INotifyCompletion
  {
    private readonly Pipe m_Pipe;

    private readonly PipeResult _result = new PipeResult(2);

    internal PipeReader(Pipe pipe) => m_Pipe = pipe;

    private void UpdateBufferReader()
    {
      var read = m_Pipe.m_ReadIdx;
      var write = m_Pipe.m_WriteIdx;

      if (read <= write)
      {
        _result.Buffer[0] = write - read == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(m_Pipe.m_Buffer, (int)read, (int)(write - read));
        _result.Buffer[1] = ArraySegment<byte>.Empty;
      }
      else
      {
        _result.Buffer[0] = m_Pipe.Size - read == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(m_Pipe.m_Buffer, (int)read, (int)(m_Pipe.Size - read));
        _result.Buffer[1] = write == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(m_Pipe.m_Buffer, 0, (int)write);
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint BytesAvailable()
    {
      var read = m_Pipe.m_ReadIdx;
      var write = m_Pipe.m_WriteIdx;

      if (read <= write) return write - read;

      return write + m_Pipe.Size - read;
    }

    public PipeResult GetBytes()
    {
      if (BytesAvailable() > 0) UpdateBufferReader();

      return _result;
    }

    // The PipeReader itself is awaitable
    // public PipeReader GetBytes()
    // {
    //   if (BytesAvailable() > 0) UpdateBufferReader();
    //
    //   if (m_Pipe.m_AwaitBeginning)
    //     throw new Exception("Double await on reader");
    //
    //   return this;
    // }

    public void Advance(uint bytes)
    {
      var read = m_Pipe.m_ReadIdx;
      var write = m_Pipe.m_WriteIdx;

      if (read <= write)
      {
        if (bytes > write - read) throw new InvalidOperationException();

        read += bytes;
      }
      else
      {
        var sz = Math.Min(bytes, m_Pipe.Size - read);

        read += sz;
        if (read > m_Pipe.Size - 1) read = 0;
        bytes -= sz;

        if (bytes > 0)
        {
          if (bytes > write) throw new InvalidOperationException();

          read = bytes;
        }
      }

      m_Pipe.m_ReadIdx = read;
    }

    #region Awaitable

    // The following makes it possible to await the reader. Do not use any of this directly.

    public PipeReader GetAwaiter() => this;

    public bool Complete() => _result.IsCompleted = true;

    public bool IsCompleted
    {
      get
      {
        if (BytesAvailable() > 0)
          return true;

        m_Pipe.m_AwaitBeginning = true;
        return false;
      }
    }

    // TODO: Return by ref?
    public PipeResult GetResult()
    {
      UpdateBufferReader();
      return _result;
    }

    public void OnCompleted(Action continuation) => m_Pipe.m_ReaderContinuation = state => continuation();

    #endregion
  }
}
