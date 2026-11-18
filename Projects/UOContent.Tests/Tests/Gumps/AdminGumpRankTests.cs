using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Server.Accounting;
using Server.Accounting.Security;
using Server.Gumps;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests.Gumps;

[Collection("Sequential UOContent Tests")]
public class AdminGumpRankTests : IDisposable
{
    private readonly PasswordProtectionAlgorithm _algorithm = AccountSecurity.CurrentAlgorithm;

    public AdminGumpRankTests() => AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.SHA2;

    public void Dispose() => AccountSecurity.CurrentAlgorithm = _algorithm;

    private static Account NewAccount(AccessLevel level) =>
        new("rank-test-" + Guid.NewGuid().ToString("N"), "test-secret-P4a-123") { AccessLevel = level };

    private static Mobile Connect(NetState state, AccessLevel level, Account account)
    {
        var mobile = new Mobile { Name = "rank-" + Guid.NewGuid().ToString("N"), AccessLevel = level, Account = account };
        mobile.NetState = state;
        state.Mobile = mobile;
        state.Account = account;
        return mobile;
    }

    [Fact]
    public void FirewallAccountListPagesByRowsShown()
    {
        var actor = new Mobile { Name = "rank-pager", AccessLevel = AccessLevel.Administrator };
        var accounts = Enumerable.Range(0, 10).Select(_ => NewAccount(AccessLevel.Player)).ToList();

        try
        {
            var entry = new SingleIpFirewallEntry(IPAddress.Parse("192.0.2.78"));
            var gump = new AdminGump(
                actor,
                AdminGumpPage.FirewallInfo,
                list: accounts.ConvertAll(a => (object)a),
                state: entry
            );

            // Page 0 shows nine rows, so the Next button must be present for the tenth.
            Assert.Contains(gump.Entries, e => e is GumpButton { ButtonID: 1 + 1 + 11 * 1 });
        }
        finally
        {
            actor.Delete();
            accounts.ForEach(Accounts.Remove);
        }
    }

    [Fact]
    public void AccountSearchDoesNotOpenSingleMatchAboveActorRank()
    {
        using var ns = PacketTestUtilities.CreateTestNetState();
        var actorAccount = NewAccount(AccessLevel.Administrator);
        var actor = Connect(ns, AccessLevel.Administrator, actorAccount);
        var target = NewAccount(AccessLevel.Developer);

        try
        {
            var name = Encoding.BigEndianUnicode.GetBytes(target.Username);
            new AdminGump(actor, AdminGumpPage.Accounts).OnResponse(
                ns,
                new RelayInfo(1 + 5 + 11 * 7, [], new ushort[] { 0 }, new Range[] { 0..name.Length }, name)
            );

            var page = typeof(AdminGump).GetField("m_PageType", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(actor.FindGump<AdminGump>());
            Assert.Equal(AdminGumpPage.Accounts, page);
        }
        finally
        {
            actor.Delete();
            Accounts.Remove(target);
            Accounts.Remove(actorAccount);
        }
    }

    [Fact]
    public void ClientBringRefusesHigherRankedCharacter()
    {
        using var actorState = PacketTestUtilities.CreateTestNetState();
        using var targetState = PacketTestUtilities.CreateTestNetState();
        var actorAccount = NewAccount(AccessLevel.GameMaster);
        var targetAccount = NewAccount(AccessLevel.Seer);
        var actor = Connect(actorState, AccessLevel.GameMaster, actorAccount);
        var target = Connect(targetState, AccessLevel.Seer, targetAccount);

        try
        {
            // Positions are set directly: moving a connected mobile between maps sends packets.
            actor.Location = new Point3D(100, 100, 0);
            target.Location = new Point3D(200, 200, 0);
            actor.Map = Map.Felucca;
            target.Map = Map.Felucca;

            new ClientGump(actor, targetState).OnResponse(actorState, new RelayInfo(6, [], [], [], []));

            Assert.Equal(new Point3D(200, 200, 0), target.Location);
        }
        finally
        {
            actor.Delete();
            target.Delete();
            Accounts.Remove(actorAccount);
            Accounts.Remove(targetAccount);
        }
    }
}
