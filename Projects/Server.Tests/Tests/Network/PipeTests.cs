using System;
using System.Threading;
using System.Threading.Tasks;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    [Collection("Sequential Tests")]
    public class PipeTests
    {
        private async void DelayedExecute(Action action)
        {
            await Task.Delay(1);

            action();
        }

        [Fact]
        public async void AwaitRead()
        {
            var pipe = new Pipe<byte>(new byte[128]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            DelayedExecute(() =>
            {
                // Write some data into the pipe
                var buffer = writer.TryGetMemory();
                Assert.Equal(127, buffer.Length);

                buffer.CopyFrom(new byte[] { 1 });
                buffer.CopyFrom(new byte[] { 2 });
                buffer.CopyFrom(new byte[] { 3 });

                writer.Advance(3);
                writer.Flush();
            });

            var result = await reader.Read();

            Assert.Equal(3, result.Buffer[0].Count);
        }

        [Fact]
        public async void AwaitWrite()
        {
            var pipe = new Pipe<byte>(new byte[128]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            // Fill the entire pipe up
            writer.Advance(127);

            DelayedExecute(() =>
            {
                // Read some data from the pipe
                var buffer = reader.TryRead();
                Assert.Equal(127, buffer.Length);

                reader.Advance(50);
                reader.Commit();
            });

            var result = await writer.GetMemory();
            Assert.Equal(50, result.Length);
        }

        private bool _signal;

        private void Consumer(object state)
        {
            var reader = ((Pipe<byte>)state).Reader;

            int count = 0;
            byte expected_value = 0xFA;

            while (count < 0x8000000)
            {
                var result = reader.TryRead();

                for (int i = 0; i < result.Buffer[0].Count; i++)
                {
                    Assert.Equal(expected_value, result.Buffer[0][i]);
                    count++;

                    if (count == 0x1000)
                    {
                        expected_value = 0xAC;
                    }
                }

                for (int i = 0; i < result.Buffer[1].Count; i++)
                {
                    Assert.Equal(expected_value, result.Buffer[1][i]);
                    count++;

                    if (count == 0x1000)
                    {
                        expected_value = 0xAC;
                    }
                }

                reader.Advance((uint)result.Length);
            }

            _signal = true;
        }

        [Fact]
        public async void Threading()
        {
            var pipe = new Pipe<byte>(new byte[0x1000]);

            ThreadPool.UnsafeQueueUserWorkItem(Consumer, pipe);

            var writer = pipe.Writer;

            int count = 0;
            byte expected_value = 0xFA;

            while (count < 0x8000000)
            {
                var result = writer.TryGetMemory();

                if (result.Length < 16)
                {
                    continue;
                }

                result.CopyFrom(new[] {
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
            var pipe = new Pipe<byte>(new byte[16]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            var result = writer.TryGetMemory();
            Assert.Equal(15, result.Length);
            Assert.Equal(0u, reader.GetAvailable());
            result = reader.TryRead();
            Assert.Equal(0, result.Length);

            writer.Advance(7);
            result = writer.TryGetMemory();
            Assert.Equal(8, result.Length);
            Assert.Equal(7u, reader.GetAvailable());
            result = reader.TryRead();
            Assert.Equal(7, result.Length);

            reader.Advance(4);
            result = writer.TryGetMemory();
            Assert.Equal(12, result.Length);
            Assert.Equal(3u, reader.GetAvailable());
            result = reader.TryRead();
            Assert.Equal(3, result.Length);

            writer.Advance(3);
            result = writer.TryGetMemory();
            Assert.Equal(9, result.Length);
            Assert.Equal(6u, reader.GetAvailable());
            result = reader.TryRead();
            Assert.Equal(6, result.Length);

        }

        [Fact]
        public void Match()
        {
            var pipe = new Pipe<byte>(new byte[16]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            for (uint i = 0; i < 16; i++)
            {
                writer.Advance(i);
                writer.Flush();

                Assert.Equal(i, reader.GetAvailable());
                reader.Advance(i);
            }
        }

        [Fact]
        public void Sequence()
        {
            var pipe = new Pipe<byte>(new byte[16]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            var buffer = writer.TryGetMemory();
            Assert.Equal(15, buffer.Length);

            buffer.CopyFrom(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });

            writer.Advance(9);
            writer.Flush();

            Assert.Equal(9u, reader.GetAvailable());

            buffer = reader.TryRead();

            for (int i = 0; i < 9; i++)
            {
                Assert.Equal(i, buffer.Buffer[0][i]);
            }

            reader.Advance(4);
            buffer = reader.TryRead();
            Assert.Equal(5, buffer.Length);
            Assert.Equal(4, buffer.Buffer[0][0]);
            Assert.Equal(5, buffer.Buffer[0][1]);
            Assert.Equal(6, buffer.Buffer[0][2]);
            Assert.Equal(7, buffer.Buffer[0][3]);
            Assert.Equal(8, buffer.Buffer[0][4]);
        }
    }
}
