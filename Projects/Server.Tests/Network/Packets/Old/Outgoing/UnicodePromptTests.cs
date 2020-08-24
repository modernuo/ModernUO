using System;
using System.Buffers;
using Server.Network;
using Server.Prompts;
using Xunit;

namespace Server.Tests.Network.Packets
{
    internal class TestPrompt : Prompt
    {
    }

    public class UnicodePromptTests
    {
        [Fact]
        public void TestUnicodePrompt()
        {
            var prompt = new TestPrompt();
            Span<byte> data = new UnicodePrompt(prompt).Compile();

            Span<byte> expectedData = stackalloc byte[21];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xC2); // Packet ID
            expectedData.Write(ref pos, (ushort)0x15); // Length
            expectedData.Write(ref pos, prompt.Serial);
            expectedData.Write(ref pos, prompt.Serial);

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (ulong)0);
      expectedData.Write(ref pos, (ushort)0);
#endif

            AssertThat.Equal(data, expectedData);
        }
    }
}
