using Server.Engines.Chat;
using Server.Mobiles;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class ChatGlobalBroadcastTests
{
    [Fact]
    public void GlobalBroadcastReachesTheUserAfterAnOfflineOne()
    {
        var enabled = ChatSystem.Enabled;
        ChatSystem.Enabled = true;
        var gone = new PlayerMobile { Player = true, Name = "Chat gone" };
        var next = new PlayerMobile { Player = true, Name = "Chat next" };
        using var goneState = PacketTestUtilities.CreateTestNetState();
        using var nextState = PacketTestUtilities.CreateTestNetState();
        gone.NetState = goneState;
        goneState.Mobile = gone;
        nextState.Account = new MockAccount();
        next.NetState = nextState;
        nextState.Mobile = next;
        ChatUser.AddChatUser(gone, "Gone");
        ChatUser.AddChatUser(next, "Next");
        Channel channel = null;

        try
        {
            gone.NetState = null; // the first user disconnects without leaving chat
            var before = nextState.SendBuffer.GetReadSpan().Length;

            channel = Channel.AddChannel("BroadcastTest");

            Assert.True(nextState.SendBuffer.GetReadSpan().Length > before);
        }
        finally
        {
            ChatUser.RemoveChatUser(ChatUser.GetChatUser(gone));
            ChatUser.RemoveChatUser(ChatUser.GetChatUser(next));
            Channel.RemoveChannel(channel);
            gone.Delete();
            next.Delete();
            ChatSystem.Enabled = enabled;
        }
    }
}
