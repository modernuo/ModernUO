using System.Linq;
using System.Reflection;
using Server;
using Server.Engines.ConPVP;
using Server.Items;
using Xunit;

namespace Server.Engines.ConPVP.Tests;

[Collection("Sequential UOContent Tests")]
public class BombingRunTests
{
    [Fact]
    public void TestFinishAwardsSharedTeamOnce()
    {
        var player = new Mobile((Serial)0x1025u);
        player.DefaultMobileInit();
        var controller = new BRController();

        try
        {
            // Five participants over four teams: participants 0 and 4 share team 0.
            var context = new DuelContext(player, RulesetLayout.Root);
            for (var i = 0; i < 3; ++i)
            {
                context.Participants.Add(new Participant(context, 1));
            }

            // Skip DuelContext.Finish; this test covers only the reward loop.
            typeof(DuelContext).GetProperty(nameof(DuelContext.Finished))!.SetValue(context, true);

            controller.TeamInfo[0][player].Score = 1;

            var game = new BRGame(controller, context);
            typeof(BRGame).GetMethod("Finish_Callback", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, null);

            var items = player.BankBox.Items;
            Assert.Single(items.OfType<BankCheck>());
            Assert.Single(items.OfType<Trophy>());
        }
        finally
        {
            controller.Delete();
            player.Delete();
        }
    }
}
