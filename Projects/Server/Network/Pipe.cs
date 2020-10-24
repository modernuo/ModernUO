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
    public interface IPipeTask<T> : INotifyCompletion
    {
        public IPipeTask<T> GetAwaiter();

        public bool IsCompleted { get; }

        public T GetResult();

        public void OnCompleted(Action continuation);
    }

    public class Pipe<T>
    {
        public struct Result<T>
        {
            public ArraySegment<T>[] Buffer { get; }
            public bool IsClosed { get; set; }

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

            public void CopyFrom(ReadOnlySpan<T> bytes)
            {
                var remaining = bytes.Length;
                var offset = 0;

                if (remaining == 0)
                {
                    return;
                }

                for (int i = 0; i < Buffer.Length; i++)
                {
                    var buffer = Buffer[i];
                    var sz = Math.Min(remaining, buffer.Count);
                    bytes.Slice(offset, sz).CopyTo(buffer);

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
                IsClosed = false;
                Buffer = new ArraySegment<T>[segments];
            }
        }

        public class PipeWriter<T>
        {
            private readonly Pipe<T> _pipe;

            public PipeWriter(Pipe<T> pipe) => _pipe = pipe;

            public Result<T> GetAvailable()
            {
                var read = _pipe._readIdx;
                var write = _pipe._writeIdx;

                var result = new Result<T>(2) { IsClosed = _pipe._closed };

                if (read <= write)
                {
                    var readZero = read == 0;
                    var sz = _pipe.Size - write - (readZero ? 1 : 0);

                    result.Buffer[0] = sz == 0 ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, (int)write, (int)sz);
                    result.Buffer[1] = readZero ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, 0, (int)read - 1);
                }
                else
                {
                    var sz = read - write - 1;

                    result.Buffer[0] = sz == 0 ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, (int)write, (int)sz);
                    result.Buffer[1] = ArraySegment<T>.Empty;
                }

                return result;
            }

            public void Advance(uint count)
            {
                var read = _pipe._readIdx;
                var write = _pipe._writeIdx;

                if (count == 0)
                {
                    return;
                }

                if (count > _pipe.Size - 1)
                {
                    throw new InvalidOperationException();
                }

                if (read <= write)
                {
                    if (count > read + _pipe.Size - write - 1)
                    {
                        throw new InvalidOperationException();
                    }

                    var sz = Math.Min(count, _pipe.Size - write);

                    write += sz;
                    if (write > _pipe.Size - 1)
                    {
                        write = 0;
                    }
                    count -= sz;

                    if (count > 0)
                    {
                        if (count >= read)
                        {
                            throw new InvalidOperationException();
                        }

                        write = count;
                    }
                }
                else
                {
                    if (count > read - write - 1)
                    {
                        throw new InvalidOperationException();
                    }

                    write += count;
                }

                // It's never valid to advance the write pointer to become equal to
                // the read pointer. Check that here.
                if (write == read)
                {
                    throw new InvalidOperationException("Write index equals read index after advance");
                }

                _pipe._writeIdx = write;
            }

            public void Close()
            {
                _pipe._closed = true;

                Flush();
            }

            public void Flush()
            {
                var waiting = _pipe._awaitBeginning;

                if (!waiting)
                {
                    return;
                }

                Action continuation;

                do
                {
                    continuation = _pipe._readerContinuation;
                } while (continuation == null);

                _pipe._readerContinuation = null;
                _pipe._awaitBeginning = false;

                ThreadPool.UnsafeQueueUserWorkItem(state => continuation(), true);
            }
        }

        public class PipeReader<T> : IPipeTask<Result<T>>
        {
            private readonly Pipe<T> _pipe;

            internal PipeReader(Pipe<T> pipe) => _pipe = pipe;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint GetAvailable()
            {
                var read = _pipe._readIdx;
                var write = _pipe._writeIdx;

                if (read <= write)
                {
                    return write - read;
                }

                return write + _pipe.Size - read;
            }

            public Result<T> TryRead()
            {
                var read = _pipe._readIdx;
                var write = _pipe._writeIdx;

                var result = new Result<T>(2) { IsClosed = _pipe._closed };

                if (read <= write)
                {
                    result.Buffer[0] = write - read == 0 ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, (int)read, (int)(write - read));
                    result.Buffer[1] = ArraySegment<T>.Empty;
                }
                else
                {
                    result.Buffer[0] = _pipe.Size - read == 0 ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, (int)read, (int)(_pipe.Size - read));
                    result.Buffer[1] = write == 0 ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, 0, (int)write);
                }

                return result;
            }

            public IPipeTask<Result<T>> Read()
            {
                if (_pipe._awaitBeginning)
                {
                    throw new Exception("Double await on reader");
                }

                return this;
            }

            public void Advance(uint count)
            {
                var read = _pipe._readIdx;
                var write = _pipe._writeIdx;

                if (read <= write)
                {
                    if (count > write - read)
                    {
                        throw new InvalidOperationException();
                    }

                    read += count;
                }
                else
                {
                    var sz = Math.Min(count, _pipe.Size - read);

                    read += sz;
                    if (read > _pipe.Size - 1)
                    {
                        read = 0;
                    }
                    count -= sz;

                    if (count > 0)
                    {
                        if (count > write)
                        {
                            throw new InvalidOperationException();
                        }

                        read = count;
                    }
                }

                _pipe._readIdx = read;
            }

            #region Awaitable

            // The following makes it possible to await the reader. Do not use any of this directly.

            public IPipeTask<Result<T>> GetAwaiter() => this;

            public bool IsCompleted
            {
                get
                {
                    if (GetAvailable() > 0)
                    {
                        return true;
                    }

                    _pipe._awaitBeginning = true;
                    return false;
                }
            }

            public Result<T> GetResult() => TryRead();

            public void OnCompleted(Action continuation) => _pipe._readerContinuation = continuation;

            #endregion
        }

        private readonly T[] _buffer;
        private volatile uint _writeIdx;
        private volatile uint _readIdx;
        private bool _closed;

        public PipeWriter<T> Writer { get; }
        public PipeReader<T> Reader { get; }

        public uint Size => (uint)_buffer.Length;

        public Pipe(T[] buf)
        {
            _buffer = buf;
            _writeIdx = 0;
            _readIdx = 0;
            _closed = false;

            Writer = new PipeWriter<T>(this);
            Reader = new PipeReader<T>(this);
        }

        #region Awaitable
        private volatile bool _awaitBeginning;
        private volatile Action _readerContinuation;

        #endregion
    }
}
