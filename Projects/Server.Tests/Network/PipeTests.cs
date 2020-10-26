using System;
using System.Threading;
using System.Threading.Tasks;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class PipeTests
    {
        private async void DelayedExecute(Action action)
        {
            await Task.Delay(5);

            action();
        }

        [Fact]
        public async void Await()
        {
            var pipe = new Pipe<byte>(new byte[100]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            DelayedExecute(() =>
            {
;                // Write some data into the pipe
                writer.GetAvailable(out var buffer);
                Assert.True(buffer.Length == 99);
                buffer[0] = 0x1;
                buffer[1] = 0x2;
                buffer[2] = 0x3;

                writer.Advance(3);
                writer.Flush();
            });

            var segments = new ArraySegment<byte>[2];
            await reader.Read(segments);

            Assert.Equal(3, segments[0].Count);
        }

        private bool _signal;

        private void Consumer(object state)
        {
            var reader = ((Pipe<byte>)state).Reader;

            int count = 0;
            byte expected_value = 0xFA;

            while (count < 0x8000000)
            {
                reader.TryRead(out var buffer);

                var first = buffer.GetSpan(0);

                for (int i = 0; i < first.Length; i++)
                {
                    Assert.True(first[i] == expected_value);
                    count++;

                    if (count == 0x1000)
                    {
                        expected_value = 0xAC;
                    }
                }

                var second = buffer.GetSpan(1);
                for (int i = 0; i < second.Length; i++)
                {
                    Assert.True(second[i] == expected_value);
                    count++;

                    if (count == 0x1000)
                    {
                        expected_value = 0xAC;
                    }
                }

                reader.Advance((uint)buffer.Length);
            }

            _signal = true;
        }

        [Fact]
        public void Threading()
        {
            var pipe = new Pipe<byte>(new byte[0x1001]);

            ThreadPool.UnsafeQueueUserWorkItem(Consumer, pipe);

            var writer = pipe.Writer;

            int count = 0;
            byte expected_value = 0xFA;

            while (count < 0x8000000)
            {
                writer.GetAvailable(out var buffer);

                if (buffer.Length < 16)
                {
                    continue;
                }

                buffer.CopyFrom(new[] {
                    expected_value, expected_value, expected_value, expected_value,
                    expected_value, expected_value, expected_value, expected_value,
                    expected_value, expected_value, expected_value, expected_value,
                    expected_value, expected_value, expected_value, expected_value
                });

                writer.Advance(16);
                count += 16;

                if (count == 0x1000)
                {
                    expected_value = 0xAC;
                }
            }

            while (_signal == false) { }

            _signal = false;
        }

        [Fact]
        public void Wrap()
        {
            var pipe = new Pipe<byte>(new byte[10]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            writer.GetAvailable(out var buffer);

            Assert.Equal(9, buffer.Length);
            Assert.Equal(0u, reader.GetAvailable());
            reader.TryRead(out buffer);
            Assert.Equal(0, buffer.Length);

            writer.Advance(7);
            writer.GetAvailable(out buffer);
            Assert.Equal(2, buffer.Length);
            Assert.Equal(7u, reader.GetAvailable());
            reader.TryRead(out buffer);
            Assert.Equal(7, buffer.Length);

            reader.Advance(4);
            writer.GetAvailable(out buffer);
            Assert.Equal(6, buffer.Length);
            Assert.Equal(3u, reader.GetAvailable());
            reader.TryRead(out buffer);
            Assert.Equal(3, buffer.Length);

            writer.Advance(3);
            writer.GetAvailable(out buffer);
            Assert.Equal(3, buffer.Length);
            Assert.Equal(6u, reader.GetAvailable());
            reader.TryRead(out buffer);
            Assert.Equal(6, buffer.Length);
        }

        [Fact]
        public void Match()
        {
            var pipe = new Pipe<byte>(new byte[10]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            for (uint i = 0; i < 9; i++)
            {
                writer.Advance(i);
                writer.Flush();

                Assert.True(reader.GetAvailable() == i);
                reader.Advance(i);
            }
        }

        [Fact]
        public void Sequence()
        {
            var pipe = new Pipe<byte>(new byte[10]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            writer.GetAvailable(out var buffer);
            Assert.True(buffer.Length == 9);

            buffer.CopyFrom(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });

            writer.Advance(9);
            writer.Flush();

            Assert.Equal(9u, reader.GetAvailable());

            reader.TryRead(out buffer);

            var first = buffer.GetSpan(0);

            for (int i = 0; i < 9; i++)
            {
                Assert.Equal(i, first[i]);
            }

            reader.Advance(4);
            reader.TryRead(out buffer);
            first = buffer.GetSpan(0);
            Assert.Equal(5, buffer.Length);
            Assert.Equal(4, first[0]);
            Assert.Equal(5, first[1]);
            Assert.Equal(6, first[2]);
            Assert.Equal(7, first[3]);
            Assert.Equal(8, first[4]);
        }
    }
}
