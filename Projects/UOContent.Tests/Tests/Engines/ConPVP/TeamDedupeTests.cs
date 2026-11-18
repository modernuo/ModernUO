using System.Linq;
using System.Reflection;
using Server;
using Server.Engines.ConPVP;
using Server.Items;
using Xunit;

namespace Server.Engines.ConPVP.Tests;

[Collection("Sequential UOContent Tests")]
public class TeamDedupeTests
{
    // Participants outnumber teams, so team 0 is listed twice unless the list is deduplicated.
    private static void AssertSharedTeamPaidOnce<TGame>(int participants, TGame game, Mobile player, DuelContext context)
    {
        for (var i = context.Participants.Count; i < participants; ++i)
        {
            context.Participants.Add(new Participant(context, 1));
        }

        // Skip DuelContext.Finish; this test covers only the reward loop.
        typeof(DuelContext).GetProperty(nameof(DuelContext.Finished))!.SetValue(context, true);

        typeof(TGame).GetMethod("Finish_Callback", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(game, null);

        var items = player.BankBox.Items;
        Assert.Single(items.OfType<BankCheck>());
        Assert.Single(items.OfType<Trophy>());
    }

    [Fact]
    public void TestDoubleDomPaysSharedTeamOnce()
    {
        var player = new Mobile((Serial)0x1031u);
        player.DefaultMobileInit();
        var controller = new DDController();

        try
        {
            var context = new DuelContext(player, RulesetLayout.Root);
            controller.TeamInfo[0][player].Score = 1;
            AssertSharedTeamPaidOnce(3, new DDGame(controller, context), player, context);
        }
        finally
        {
            controller.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void TestKingOfTheHillPaysSharedTeamOnce()
    {
        var player = new Mobile((Serial)0x1032u);
        player.DefaultMobileInit();
        var controller = new KHController();

        try
        {
            var context = new DuelContext(player, RulesetLayout.Root);
            controller.TeamInfo[0][player].Score = 1;
            AssertSharedTeamPaidOnce(9, new KHGame(controller, context), player, context);
        }
        finally
        {
            controller.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void TestCtfPaysSharedTeamOnce()
    {
        var player = new Mobile((Serial)0x1033u);
        player.DefaultMobileInit();
        var controller = new CTFController();
        var flag = new CTFFlag();

        try
        {
            var context = new DuelContext(player, RulesetLayout.Root);
            controller.TeamInfo[0].Name = "Red";
            controller.TeamInfo[0].Flag = flag;
            controller.TeamInfo[0][player].Score = 1;
            AssertSharedTeamPaidOnce(9, new CTFGame(controller, context), player, context);
        }
        finally
        {
            flag.Delete();
            controller.Delete();
            player.Delete();
        }
    }
}
