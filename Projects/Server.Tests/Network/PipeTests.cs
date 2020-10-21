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
                var buffer = writer.GetAvailable();
                Assert.True(buffer.Length == 99);

                buffer.CopyFrom(new byte[] { 1 });
                buffer.CopyFrom(new byte[] { 2 });
                buffer.CopyFrom(new byte[] { 3 });

                writer.Advance(3);
                writer.Flush();
            });

            var result = await reader;

            Assert.True(result.Buffer[0].Count == 3);
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
                    Assert.True(result.Buffer[0][i] == expected_value);
                    count++;

                    if (count == 0x1000)
                    {
                        expected_value = 0xAC;
                    }
                }

                for (int i = 0; i < result.Buffer[1].Count; i++)
                {
                    Assert.True(result.Buffer[1][i] == expected_value);
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
            var pipe = new Pipe<byte>(new byte[0x1001]);

            ThreadPool.UnsafeQueueUserWorkItem(Consumer, pipe);

            var writer = pipe.Writer;

            int count = 0;
            byte expected_value = 0xFA;

            while (count < 0x8000000)
            {
                var result = writer.GetAvailable();

                if (result.Length < 16)
                {
                    continue;
                }

                result.CopyFrom(new[] { expected_value, expected_value, expected_value, expected_value, expected_value, expected_value, expected_value, expected_value,
                    expected_value, expected_value, expected_value, expected_value, expected_value, expected_value, expected_value, expected_value });

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

            var result = writer.GetAvailable();
            Assert.True(result.Length == 9);
            Assert.True(reader.GetRemaining() == 0);
            result = reader.TryRead();
            Assert.True(result.Length == 0);

            writer.Advance(7);
            result = writer.GetAvailable();
            Assert.True(result.Length == 2);
            Assert.True(reader.GetRemaining() == 7);
            result = reader.TryRead();
            Assert.True(result.Length == 7);

            reader.Advance(4);
            result = writer.GetAvailable();
            Assert.True(result.Length == 6);
            Assert.True(reader.GetRemaining() == 3);
            result = reader.TryRead();
            Assert.True(result.Length == 3);

            writer.Advance(3);
            result = writer.GetAvailable();
            Assert.True(result.Length == 3);
            Assert.True(reader.GetRemaining() == 6);
            result = reader.TryRead();
            Assert.True(result.Length == 6);

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

                Assert.True(reader.GetRemaining() == i);
                reader.Advance(i);
            }
        }

        [Fact]
        public void Sequence()
        {
            var pipe = new Pipe<byte>(new byte[10]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            var buffer = writer.GetAvailable();
            Assert.True(buffer.Length == 9);

            buffer.CopyFrom(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });

            writer.Advance(9);
            writer.Flush();

            Assert.True(reader.GetRemaining() == 9);

            buffer = reader.TryRead();

            for (int i = 0; i < 9; i++)
            {
                Assert.True(buffer.Buffer[0][i] == i);
            }

            reader.Advance(4);
            buffer = reader.TryRead();
            Assert.True(buffer.Length == 5);
            Assert.True(buffer.Buffer[0][0] == 4);
            Assert.True(buffer.Buffer[0][1] == 5);
            Assert.True(buffer.Buffer[0][2] == 6);
            Assert.True(buffer.Buffer[0][3] == 7);
            Assert.True(buffer.Buffer[0][4] == 8);
        }
    }
}
