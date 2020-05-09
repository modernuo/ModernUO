using System;
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

      Span<byte> expectedData = stackalloc byte[]
      {
        0xC2, // Packet ID
        0x00, 0x15, // Length
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, 0x00, 0x00, // Prompt Serial
        0x00, 0x00, 0x00, 0x00, // Unused
        0x00, 0x00, 0x00, 0x00, // Unused
        0x00, 0x00 // Unused
      };

      prompt.Serial.CopyTo(expectedData.Slice(3, 4));
      prompt.Serial.CopyTo(expectedData.Slice(7, 4));

      AssertThat.Equal(data, expectedData);
    }
  }
}
