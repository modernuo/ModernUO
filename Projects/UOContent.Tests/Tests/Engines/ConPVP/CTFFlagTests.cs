using System.Reflection;
using Server;
using Server.Engines.ConPVP;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace Server.Engines.ConPVP.Tests;

[Collection("Sequential UOContent Tests")]
public class CTFFlagTests
{
    [Fact]
    public void TestEliminatedCarrierCaptureIsIgnored()
    {
        var carrier = new PlayerMobile(World.NewMobile);
        carrier.DefaultMobileInit();
        carrier.AddItem(new Backpack());
        var controller = new CTFController();
        var ownFlag = new CTFFlag();
        var enemyFlag = new CTFFlag();

        try
        {
            var context = new DuelContext(carrier, RulesetLayout.Root);
            var game = new CTFGame(controller, context);
            controller.TeamInfo[0].Flag = ownFlag;
            controller.TeamInfo[1].Game = game;
            enemyFlag.m_TeamInfo = controller.TeamInfo[1];
            carrier.Backpack.DropItem(enemyFlag);

            // The carrier left the arena with the target still open.
            carrier.DuelPlayer.Eliminated = true;

            typeof(CTFFlag).GetMethod("Flag_OnTarget", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(enemyFlag, [carrier, ownFlag]);

            Assert.True(enemyFlag.IsChildOf(carrier.Backpack));
        }
        finally
        {
            enemyFlag.Delete();
            ownFlag.Delete();
            controller.Delete();
            carrier.Delete();
        }
    }
}
