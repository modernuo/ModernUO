using System;
using System.Threading;

namespace Server.Network
{
    public class PipeWriter
    {
        private Pipe _pipe;

        private PipeResult _result = new PipeResult(2);

        public PipeWriter(Pipe pipe) => _pipe = pipe;

        public PipeResult GetBytes()
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            if (read <= write)
            {
                var sz = Math.Min(read + _pipe.Size - write - 1, _pipe.Size - write);

                _result.Buffer[0] = sz == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe.m_Buffer, (int)write, (int)sz);
                _result.Buffer[1] = read == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(_pipe.m_Buffer, 0, (int)read);
            }
            else
            {
                var sz = read - write - 1;

                _result.Buffer[0] = sz == 0 ? new ArraySegment<byte>() : new ArraySegment<byte>(_pipe.m_Buffer, (int)write, (int)sz);
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
                if (bytes > read + _pipe.Size - write)
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
            _result.IsCompleted = true;
            _pipe.Reader.Complete();

            Flush();
        }

        public void Flush()
        {
            var waiting = _pipe._awaitBeginning;
            WaitCallback continuation;

            if (!waiting)
            {
                return;
            }

            do {
                continuation = _pipe._readerContinuation;
            } while (continuation == null);

            _pipe._readerContinuation = null;
            _pipe._awaitBeginning = false;

            ThreadPool.UnsafeQueueUserWorkItem(continuation, null);
        }
    }
}
