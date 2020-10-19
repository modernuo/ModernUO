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

            var result = await reader.GetBytes();

            Assert.True(result.Buffer[0].Count == 3);
        }

        [Fact]
        public void Wrap()
        {
            var pipe = new Pipe(new byte[10]);

            var reader = pipe.Reader;
            var writer = pipe.Writer;

            var buffer = writer.GetBytes();
            Assert.True(buffer.Length == 9);

            writer.Advance(7);
            Assert.True(buffer.Length == 9);

            buffer = writer.GetBytes();
            Assert.True(buffer.Length == 2);

            reader.Advance(4);
            buffer = writer.GetBytes();
            Assert.True(buffer.Length == 7);

            writer.Advance(3);
            buffer = writer.GetBytes();
            Assert.True(buffer.Length == 3);

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
