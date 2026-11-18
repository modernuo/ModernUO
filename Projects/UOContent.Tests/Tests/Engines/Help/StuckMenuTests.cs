using Server;
using Server.Factions;
using Server.Gumps;
using Server.Items;
using Server.Menus.Questions;
using Server.Mobiles;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class StuckMenuTests
{
    [Fact]
    public void SigilCarrierDoesNotSpendAStuckMenuUse()
    {
        var player = new PlayerMobile { Player = true, Body = 400 };
        player.AddItem(new Backpack());
        player.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        player.Backpack.DropItem(new Sigil(null));
        using var ns = PacketTestUtilities.CreateTestNetState();

        try
        {
            // Two uses a day; a refused answer must not spend either.
            for (var i = 0; i < 2; i++)
            {
                new StuckMenu(player, player, true).OnResponse(ns, new RelayInfo(1, [], [], [], []));
            }

            Assert.True(player.CanUseStuckMenu());
        }
        finally
        {
            player.Delete();
        }
    }
}
