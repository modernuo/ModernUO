/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

namespace Server.Network;

public interface IPipeTask<T> : INotifyCompletion
{
    public IPipeTask<T> GetAwaiter();

    public bool IsCompleted { get; }

    public T GetResult();
}

public class Pipe<T>
{
    public struct Result
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

            for (int i = 0; i < 2; i++)
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

    public class PipeWriter : IPipeTask<Result>
    {
        private readonly Pipe<T> _pipe;

        private Result _result = new(2);

        internal PipeWriter(Pipe<T> pipe) => _pipe = pipe;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetAvailable()
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            if (read <= write)
            {
                if (read == 0)
                {
                    return _pipe.Size - write - 1;
                }

                return _pipe.Size - write + (read - 1);
            }

            return read - write - 1;
        }

        public Result TryGetMemory()
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            _result.IsClosed = _pipe._closed;

            if (read <= write)
            {
                var readZero = read == 0;
                var sz = _pipe.Size - write - (readZero ? 1 : 0);

                _result.Buffer[0] = sz == 0 ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, (int)write, (int)sz);
                _result.Buffer[1] = readZero ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, 0, (int)read - 1);
            }
            else
            {
                var sz = read - write - 1;

                _result.Buffer[0] = sz == 0 ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, (int)write, (int)sz);
                _result.Buffer[1] = ArraySegment<T>.Empty;
            }

            return _result;
        }

        public IPipeTask<Result> GetMemory()
        {
            if (_pipe._writeAwaitBeginning)
            {
                throw new Exception("Double await on writer");
            }

            return this;
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

            var waiting = _pipe._readAwaitBeginning;

            if (!waiting)
            {
                return;
            }

            Action continuation;

            do
            {
                continuation = _pipe._readContinuation;
            } while (continuation == null);

            _pipe._readContinuation = null;
            _pipe._readAwaitBeginning = false;

            ThreadPool.UnsafeQueueUserWorkItem(_ => continuation(), true);
        }

        public void Flush()
        {
            if (_pipe._readIdx == _pipe._writeIdx)
            {
                return;
            }

            var waiting = _pipe._readAwaitBeginning;

            if (!waiting)
            {
                return;
            }

            Action continuation;

            do
            {
                continuation = _pipe._readContinuation;
            } while (continuation == null);

            _pipe._readContinuation = null;
            _pipe._readAwaitBeginning = false;

            ThreadPool.UnsafeQueueUserWorkItem(_ => continuation(), true);
        }

        #region Awaitable

        // The following makes it possible to await the writer. Do not use any of this directly.

        public IPipeTask<Result> GetAwaiter() => this;

        public bool IsCompleted
        {
            get
            {
                if (GetAvailable() > 0)
                {
                    return true;
                }

                if (_pipe._closed)
                {
                    return true;
                }

                _pipe._writeAwaitBeginning = true;
                return false;
            }
        }

        public Result GetResult() => TryGetMemory();

        public void OnCompleted(Action continuation) => _pipe._writeContinuation = continuation;

        #endregion
    }

    public class PipeReader : IPipeTask<Result>
    {
        private readonly Pipe<T> _pipe;

        private Result _result = new(2);

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

        public Result TryRead()
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            _result.IsClosed = _pipe._closed;

            if (read <= write)
            {
                _result.Buffer[0] = write - read == 0 ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, (int)read, (int)(write - read));
                _result.Buffer[1] = ArraySegment<T>.Empty;
            }
            else
            {
                _result.Buffer[0] = _pipe.Size - read == 0 ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, (int)read, (int)(_pipe.Size - read));
                _result.Buffer[1] = write == 0 ? ArraySegment<T>.Empty : new ArraySegment<T>(_pipe._buffer, 0, (int)write);
            }

            return _result;
        }

        public IPipeTask<Result> Read()
        {
            if (_pipe._readAwaitBeginning)
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

        public void Commit()
        {
            if (_pipe._readIdx == ((_pipe._writeIdx + 1) & (_pipe.Size - 1)))
            {
                return;
            }

            var waiting = _pipe._writeAwaitBeginning;

            if (!waiting)
            {
                return;
            }

            Action continuation;

            do
            {
                continuation = _pipe._writeContinuation;
            } while (continuation == null);

            _pipe._writeContinuation = null;
            _pipe._writeAwaitBeginning = false;

            ThreadPool.UnsafeQueueUserWorkItem(_ => continuation(), true);
        }

        public void Close()
        {
            _pipe._closed = true;

            var waiting = _pipe._writeAwaitBeginning;

            if (!waiting)
            {
                return;
            }

            Action continuation;

            do
            {
                continuation = _pipe._writeContinuation;
            } while (continuation == null);

            _pipe._writeContinuation = null;
            _pipe._writeAwaitBeginning = false;

            ThreadPool.UnsafeQueueUserWorkItem(_ => continuation(), true);
        }

        #region Awaitable

        // The following makes it possible to await the reader. Do not use any of this directly.

        public IPipeTask<Result> GetAwaiter() => this;

        public bool IsCompleted
        {
            get
            {
                if (GetAvailable() > 0)
                {
                    return true;
                }

                if (_pipe._closed)
                {
                    return true;
                }

                _pipe._readAwaitBeginning = true;
                return false;
            }
        }

        public Result GetResult() => TryRead();

        public void OnCompleted(Action continuation) => _pipe._readContinuation = continuation;

        #endregion
    }

    private readonly T[] _buffer;
    private volatile uint _writeIdx;
    private volatile uint _readIdx;
    private bool _closed;

    public PipeWriter Writer { get; }
    public PipeReader Reader { get; }

    public uint Size => (uint)_buffer.Length;

    public Pipe(T[] buf)
    {
        // Test if the buffer is a power of two
        if (buf.Length == 0 || (buf.Length & (buf.Length - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buf), "Pipe buffers must have a length that is a power of two");
        }

        _buffer = buf;
        _writeIdx = 0;
        _readIdx = 0;
        _closed = false;

        Writer = new PipeWriter(this);
        Reader = new PipeReader(this);
    }

    #region Awaitable
    private volatile bool _readAwaitBeginning;
    private volatile Action _readContinuation;

    private volatile bool _writeAwaitBeginning;
    private volatile Action _writeContinuation;

    #endregion
}
