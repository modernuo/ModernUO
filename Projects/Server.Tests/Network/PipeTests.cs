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
                // Write some data into the pipe
                writer.GetAvailable(out var buffer);
                Assert.True(buffer.Length == 99);

                buffer.CopyFrom(new byte[] { 1 });
                buffer.CopyFrom(new byte[] { 2 });
                buffer.CopyFrom(new byte[] { 3 });

                writer.Advance(3);
                writer.Flush();
            });

            var segments = new ArraySegment<byte>[2];
            (await reader).TryRead(segments);

            Assert.True(segments[0].Count == 3);
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

            Assert.True(buffer.Length == 9);
            Assert.True(reader.GetAvailable() == 0);
            reader.TryRead(out buffer);
            Assert.True(buffer.Length == 0);

            writer.Advance(7);
            writer.GetAvailable(out buffer);
            Assert.True(buffer.Length == 2);
            Assert.True(reader.GetAvailable() == 7);
            reader.TryRead(out buffer);
            Assert.True(buffer.Length == 7);

            reader.Advance(4);
            writer.GetAvailable(out buffer);
            Assert.True(buffer.Length == 6);
            Assert.True(reader.GetAvailable() == 3);
            reader.TryRead(out buffer);
            Assert.True(buffer.Length == 3);

            writer.Advance(3);
            writer.GetAvailable(out buffer);
            Assert.True(buffer.Length == 3);
            Assert.True(reader.GetAvailable() == 6);
            reader.TryRead(out buffer);
            Assert.True(buffer.Length == 6);

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

            Assert.True(reader.GetAvailable() == 9);

            reader.TryRead(out buffer);

            var first = buffer.GetSpan(0);

            for (int i = 0; i < 9; i++)
            {
                Assert.True(first[i] == i);
            }

            reader.Advance(4);
            reader.TryRead(out buffer);
            first = buffer.GetSpan(0);
            Assert.True(buffer.Length == 5);
            Assert.True(first[0] == 4);
            Assert.True(first[1] == 5);
            Assert.True(first[2] == 6);
            Assert.True(first[3] == 7);
            Assert.True(first[4] == 8);
        }
    }
}
