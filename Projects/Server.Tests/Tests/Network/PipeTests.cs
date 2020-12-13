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
            await Task.Delay(5).ConfigureAwait(false);

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
                var result = writer.TryGetMemory();
                Assert.True(result.Buffer[0].Count == 99);
                result.Buffer[0][0] = 0x1;
                result.Buffer[0][1] = 0x2;
                result.Buffer[0][2] = 0x3;

                writer.Advance(3);
                writer.Flush();
            });

            var result = await reader.Read();
            Assert.Equal(3, result.Buffer[0].Count);
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

                var first = result.Buffer[0];

                for (int i = 0; i < first.Count; i++)
                {
                    Assert.True(first[i] == expected_value);
                    count++;

                    if (count == 0x1000)
                    {
                        expected_value = 0xAC;
                    }
                }

                var second = result.Buffer[1];
                for (int i = 0; i < second.Count; i++)
                {
                    Assert.True(second[i] == expected_value);
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
        public void Threading()
        {
            var pipe = new Pipe<byte>(new byte[0x1001]);

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
            var pipe = new Pipe<byte>(new byte[10]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            var result = writer.TryGetMemory();

            Assert.Equal(9, result.Length);
            Assert.Equal(0u, reader.GetAvailable());
            result = reader.TryRead();
            Assert.Equal(0, result.Length);

            writer.Advance(7);
            result = writer.TryGetMemory();
            Assert.Equal(2, result.Length);
            Assert.Equal(7u, reader.GetAvailable());
            result = reader.TryRead();
            Assert.Equal(7, result.Length);

            reader.Advance(4);
            result = writer.TryGetMemory();
            Assert.Equal(6, result.Length);
            Assert.Equal(3u, reader.GetAvailable());
            result = reader.TryRead();
            Assert.Equal(3, result.Length);

            writer.Advance(3);
            result = writer.TryGetMemory();
            Assert.Equal(3, result.Length);
            Assert.Equal(6u, reader.GetAvailable());
            result = reader.TryRead();
            Assert.Equal(6, result.Length);
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

            var result = writer.TryGetMemory();
            Assert.Equal(9, result.Length);

            result.CopyFrom(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });

            writer.Advance(9);
            writer.Flush();

            Assert.Equal(9u, reader.GetAvailable());

            result = reader.TryRead();

            var first = result.Buffer[0];

            for (int i = 0; i < 9; i++)
            {
                Assert.Equal(i, first[i]);
            }

            reader.Advance(4);
            result = reader.TryRead();
            Assert.Equal(5, result.Length);
            first = result.Buffer[0];
            Assert.Equal(4, first[0]);
            Assert.Equal(5, first[1]);
            Assert.Equal(6, first[2]);
            Assert.Equal(7, first[3]);
            Assert.Equal(8, first[4]);
        }
    }
}
