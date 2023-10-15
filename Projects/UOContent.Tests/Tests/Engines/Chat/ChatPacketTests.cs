using Server.Engines.Chat;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests
{
    public class ChatPacketTests
    {
        [Theory]
        [InlineData(null, 100, "some param", "some other param")]
        [InlineData("ENU", 200, "a third param", "another param")]
        public void TestSendChatMessage(string lang, int number, string param1, string param2)
        {
            var expected = new ChatMessagePacket(lang, number, param1, param2).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendChatMessage(lang, number, param1, param2);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }
    }
}
