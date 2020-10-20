/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Pipe.cs                                                         *
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
using System.Threading;

namespace Server.Network
{
    public class Pipe
    {
        public struct Result
        {
            public ArraySegment<byte>[] Buffer { get; }
            public bool IsCanceled { get; set; }
            public bool IsCompleted { get; set; }

            public int Length
            {
                get
                {
                    var length = 0;
                    for (int i = 0; i < Buffer.Length; i++)
                    {
                        length += Buffer[i].Count;
                    }

                    return length;
                }
            }

            public void CopyFrom(Span<byte> bytes)
            {
                var remaining = bytes.Length;
                var offset = 0;

                if (remaining == 0)
                {
                    return;
                }

                for (int i = 0; i < Buffer.Length; i++)
                {
                    var sz = Math.Min(remaining, Buffer[i].Count);
                    bytes.Slice(offset, sz).CopyTo(Buffer[i].AsSpan());

                    remaining -= sz;
                    offset += sz;

                    if (remaining == 0)
                    {
                        return;
                    }
                }

                throw new OutOfMemoryException();
            }

            public Result(int segments)
            {
                IsCanceled = false;
                IsCompleted = false;
                Buffer = new ArraySegment<byte>[segments];
            }
        }

        public class PipeWriter
        {
            private Pipe _pipe;

            private Result _result = new Result(2);

            public PipeWriter(Pipe pipe) => _pipe = pipe;

            public Result GetBytes()
            {
                var read = _pipe._readIdx;
                var write = _pipe._writeIdx;

                if (read <= write)
                {
                    var sz = Math.Min(read + _pipe.Size - write - 1, _pipe.Size - write);

                    _result.Buffer[0] = sz == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe._buffer, (int)write, (int)sz);
                    _result.Buffer[1] = read == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe._buffer, 0, (int)read - 1);
                }
                else
                {
                    var sz = read - write - 1;

                    _result.Buffer[0] = sz == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe._buffer, (int)write, (int)sz);
                    _result.Buffer[1] = ArraySegment<byte>.Empty;
                }

                return _result;
            }

            public void Advance(uint bytes)
            {
                var read = _pipe._readIdx;
                var write = _pipe._writeIdx;

                if (bytes == 0)
                {
                    return;
                }

                if (bytes > _pipe.Size - 1)
                {
                    throw new InvalidOperationException();
                }

                if (read <= write)
                {
                    if (bytes > read + _pipe.Size - write - 1)
                    {
                        throw new InvalidOperationException();
                    }

                    var sz = Math.Min(bytes, _pipe.Size - write);

                    write += sz;
                    if (write > _pipe.Size - 1)
                    {
                        write = 0;
                    }
                    bytes -= sz;

                    if (bytes > 0)
                    {
                        if (bytes >= read)
                        {
                            throw new InvalidOperationException();
                        }

                        write = bytes;
                    }
                }
                else
                {
                    if (bytes > read - write - 1)
                    {
                        throw new InvalidOperationException();
                    }

                    write += bytes;
                }

                _pipe._writeIdx = write;
            }

            public void Complete()
            {
                _pipe.Reader._result.IsCompleted = true;

                Flush();
            }

            public void Flush()
            {
                var waiting = _pipe._awaitBeginning;
                Action continuation;

                if (!waiting)
                {
                    return;
                }

                do
                {
                    continuation = _pipe._readerContinuation;
                } while (continuation == null);

                _pipe._readerContinuation = null;
                _pipe._awaitBeginning = false;

                ThreadPool.UnsafeQueueUserWorkItem(state => continuation(), true);
            }
        }

        public class PipeReader : INotifyCompletion
        {
            private Pipe _pipe;

            internal Result _result = new Result(2);

            internal PipeReader(Pipe pipe) => _pipe = pipe;

            private void UpdateBufferReader()
            {
                var read = _pipe._readIdx;
                var write = _pipe._writeIdx;

                if (read <= write)
                {
                    _result.Buffer[0] = write - read == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe._buffer, (int)read, (int)(write - read));
                    _result.Buffer[1] = ArraySegment<byte>.Empty;
                }
                else
                {
                    _result.Buffer[0] = _pipe.Size - read == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe._buffer, (int)read, (int)(_pipe.Size - read));
                    _result.Buffer[1] = write == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe._buffer, 0, (int)write);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint BytesAvailable()
            {
                var read = _pipe._readIdx;
                var write = _pipe._writeIdx;

                if (read <= write)
                {
                    return write - read;
                }

                return write + _pipe.Size - read;
            }

            public Result TryGetBytes()
            {
                UpdateBufferReader();
                return _result;
            }

            public PipeReader GetBytes()
            {
                if (_pipe._awaitBeginning)
                {
                    throw new Exception("Double await on reader");
                }

                return this;
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

            public Result GetResult() => TryGetBytes();

            public void OnCompleted(Action continuation) => _pipe._readerContinuation = continuation;

            #endregion
        }

        private byte[] _buffer;
        private volatile uint _writeIdx;
        private volatile uint _readIdx;

        public PipeWriter Writer { get; }
        public PipeReader Reader { get; }

        public uint Size => (uint)_buffer.Length;

        public Pipe(byte[] buf)
        {
            _buffer = buf;
            _writeIdx = 0;
            _readIdx = 0;

            Writer = new PipeWriter(this);
            Reader = new PipeReader(this);
        }

        #region Awaitable
        private volatile bool _awaitBeginning;
        private volatile Action _readerContinuation;

        #endregion
    }
}
