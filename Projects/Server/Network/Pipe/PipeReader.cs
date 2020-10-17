/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PipeReader.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
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
        private readonly Pipe _pipe;

        private readonly PipeResult _result = new PipeResult(2);

        internal PipeReader(Pipe pipe) => _pipe = pipe;

        private void UpdateBufferReader()
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            if (read <= write)
            {
                _result.Buffer[0] = write - read == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe.m_Buffer, (int)read, (int)(write - read));
                _result.Buffer[1] = ArraySegment<byte>.Empty;
            }
            else
            {
                _result.Buffer[0] = _pipe.Size - read == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe.m_Buffer, (int)read, (int)(_pipe.Size - read));
                _result.Buffer[1] = write == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe.m_Buffer, 0, (int)write);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint BytesAvailable()
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            return (read <= write ? write : write + _pipe.Size) - read;
        }

        public PipeResult TryGetBytes()
        {
            if (BytesAvailable() > 0)
            {
                UpdateBufferReader();
            }

            return _result;
        }

        public PipeResult GetBytes()
        {
            if (BytesAvailable() > 0)
            {
                UpdateBufferReader();
            }

            if (_pipe._awaitBeginning)
            {
                throw new Exception("Double await on PipeReader");
            }

            return _result;
        }

        public void Advance(uint bytes)
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            if (read <= write)
            {
                if (bytes > write - read)
                {
                    throw new InvalidOperationException();
                }

                read += bytes;
            }
            else
            {
                var sz = Math.Min(bytes, _pipe.Size - read);

                read += sz;
                if (read > _pipe.Size - 1)
                {
                    read = 0;
                }

                bytes -= sz;

                if (bytes > 0)
                {
                    if (bytes > write)
                    {
                        throw new InvalidOperationException();
                    }

                    read = bytes;
                }
            }

            _pipe._readIdx = read;
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
                {
                    return true;
                }

                _pipe._awaitBeginning = true;
                return false;
            }
        }

        // TODO: Return by ref?
        public PipeResult GetResult()
        {
            UpdateBufferReader();
            return _result;
        }

        public void OnCompleted(Action continuation) => _pipe._readerContinuation = state => continuation();
        #endregion
    }
}
