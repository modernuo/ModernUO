using Server;
using Server.Engines.PartySystem;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PartyPacketTests
{
    // Without a party, the remove-member packet tells the removed member their list is now empty.
    [Fact]
    public void TestPartyRemoveMember_WithoutParty_SendsEmptyList()
    {
        var m = (Serial)0x1024u;

        var expected = new PartyEmptyList(m).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendPartyRemoveMember(m);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestPartyRemoveMember()
    {
        // Fresh serials: Delete removes by serial, and a fixed one could belong to a live entity.
        var leader = new Mobile(World.NewMobile);
        var member = new Mobile(World.NewMobile);

        try
        {
            leader.DefaultMobileInit();
            member.DefaultMobileInit();

            var p = new Party(leader);
            p.Add(member);

            var expected = new PartyRemoveMember(member.Serial, p).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPartyRemoveMember(member.Serial, p);

            var result = ns.SendBuffer.GetReadSpan();
            AssertThat.Equal(result, expected);
        }
        finally
        {
            member.Delete();
            leader.Delete();
        }
    }

    [Fact]
    public void TestPartyMemberList()
    {
        // Fresh serials: Delete removes by serial, and a fixed one could belong to a live entity.
        var leader = new Mobile(World.NewMobile);
        var member = new Mobile(World.NewMobile);

        try
        {
            leader.DefaultMobileInit();
            member.DefaultMobileInit();

            var p = new Party(leader);
            p.Add(member);

            var expected = new PartyMemberList(p).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPartyMemberList(p);

            var result = ns.SendBuffer.GetReadSpan();
            AssertThat.Equal(result, expected);
        }
        finally
        {
            member.Delete();
            leader.Delete();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestPartyTextMessage(bool toAll)
    {
        var serial = (Serial)0x1024u;
        var text = "[Party] Stuff Happens";

        var expected = new PartyTextMessage(toAll, serial, text).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendPartyTextMessage(serial, text, toAll);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestPartyInvitation()
    {
        var m = (Serial)0x1024u;

        var expected = new PartyInvitation(m).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendPartyInvitation(m);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
