using System;
using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace Server.Tests.Pipe
{
  public class PipeTests
  {
    private static async void DelayedExecute<T>(int ms, Action<T> action, T arg)
    {
      await Task.Delay(ms);
      action(arg);
    }

    [Fact]
    public async void Await()
    {
      var pipe = new Server.Network.Pipe(new byte[100]);

      var reader = pipe.Reader;
      var writer = pipe.Writer;

      DelayedExecute(5, w =>
      {
        // Write some data into the pipe
        var buffer = w.GetBytes().Buffer;
        var bw = new BufferWriter(buffer);

        bw.Write((byte)1);
        bw.Write((byte)2);
        bw.Write((byte)3);

        w.Advance((uint)bw.Position);
        w.Flush();
      }, writer);

      var result = await reader;

      byte[] expected = { 0x1, 0x2, 0x3 };

      AssertThat.Equal(expected, result.Buffer[0]);
    }
  }
}
