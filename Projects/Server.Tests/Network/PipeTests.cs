using System;
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
            var pipe = new Pipe(new byte[100]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            DelayedExecute(() =>
            {
                // Write some data into the pipe
                var buffer = writer.GetBytes();
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

        [Fact]
        public void Wrap()
        {
            var pipe = new Pipe(new byte[10]);

            Pipe.Result result;

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            result = writer.GetBytes();
            Assert.True(result.Length == 9);
            Assert.True(reader.BytesAvailable() == 0);
            result = reader.TryGetBytes();
            Assert.True(result.Length == 0);

            writer.Advance(7);
            result = writer.GetBytes();
            Assert.True(result.Length == 2);
            Assert.True(reader.BytesAvailable() == 7);
            result = reader.TryGetBytes();
            Assert.True(result.Length == 7);

            reader.Advance(4);
            result = writer.GetBytes();
            Assert.True(result.Length == 6);
            Assert.True(reader.BytesAvailable() == 3);
            result = reader.TryGetBytes();
            Assert.True(result.Length == 3);

            writer.Advance(3);
            result = writer.GetBytes();
            Assert.True(result.Length == 3);
            Assert.True(reader.BytesAvailable() == 6);
            result = reader.TryGetBytes();
            Assert.True(result.Length == 6);

        }

        [Fact]
        public void Match()
        {
            var pipe = new Pipe(new byte[10]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            for (uint i = 0; i < 9; i++)
            {
                writer.Advance(i);
                writer.Flush();

                Assert.True(reader.BytesAvailable() == i);
                reader.Advance(i);
            }
        }
    }
}
