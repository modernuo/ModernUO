using Server.Engines.Chat;
using Server.Mobiles;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class ChatRenameTests
{
    [Fact]
    public void RenameToAnExistingOrEmptyNameIsRefused()
    {
        var enabled = ChatSystem.Enabled;
        ChatSystem.Enabled = true;
        var mobile = new PlayerMobile { Player = true, Name = "Rename mod" };
        using var state = PacketTestUtilities.CreateTestNetState();
        mobile.NetState = state;
        state.Mobile = mobile;
        var existing = Channel.AddChannel("RenameExisting");
        var channel = Channel.AddChannel("RenameSource");
        var user = ChatUser.AddChatUser(mobile, "Mod");

        try
        {
            ChatActionHandlers.RenameChannel(user, channel, "RenameExisting");
            ChatActionHandlers.RenameChannel(user, channel, "  ");

            Assert.Equal("RenameSource", channel.Name);
        }
        finally
        {
            ChatUser.RemoveChatUser(user);
            Channel.RemoveChannel(existing);
            Channel.RemoveChannel(channel);
            mobile.Delete();
            ChatSystem.Enabled = enabled;
        }
    }
}
