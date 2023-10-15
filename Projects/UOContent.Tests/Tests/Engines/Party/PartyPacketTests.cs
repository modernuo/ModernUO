using Server;
using Server.Engines.PartySystem;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests
{
    public class PartyPacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestPartyEmptyList()
        {
            Serial m = (Serial)0x1024u;

            var expected = new PartyEmptyList(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPartyRemoveMember(m);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestPartyRemoveMember()
        {
            var leader = new Mobile((Serial)0x1024u);
            leader.DefaultMobileInit();

            var member = new Mobile((Serial)0x2048u);
            member.DefaultMobileInit();

            var p = new Party(leader);
            p.Add(member);

            var expected = new PartyRemoveMember(member.Serial, p).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPartyRemoveMember(member.Serial, p);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestPartyMemberList()
        {
            var leader = new Mobile((Serial)0x1024u);
            leader.DefaultMobileInit();

            var member = new Mobile((Serial)0x2048u);
            member.DefaultMobileInit();

            var p = new Party(leader);
            p.Add(member);

            var expected = new PartyMemberList(p).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPartyMemberList(p);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestPartyTextMessage(bool toAll)
        {
            Serial serial = (Serial)0x1024u;
            var text = "[Party] Stuff Happens";

            var expected = new PartyTextMessage(toAll, serial, text).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPartyTextMessage(serial, text, toAll);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestPartyInvitation()
        {
            Serial m = (Serial)0x1024u;

            var expected = new PartyInvitation(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPartyInvitation(m);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }
    }
}
