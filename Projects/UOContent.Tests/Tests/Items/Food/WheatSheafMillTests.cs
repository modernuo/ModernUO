using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class WheatSheafMillTests
{
    private sealed class Mill : IFlourMill
    {
        public int MaxFlour => 10;
        public int CurFlour { get; set; }
    }

    [Fact]
    public void SheafNowHeldByAnotherMobileDoesNotFillTheMill()
    {
        var from = new PlayerMobile { Player = true };
        var other = new PlayerMobile { Player = true };
        var pack = new Backpack();
        other.AddItem(pack);
        var held = new WheatSheaf(5);
        pack.DropItem(held);
        var ground = new WheatSheaf(3);
        ground.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        var mill = new Mill();

        try
        {
            held.OnTarget(from, mill);

            Assert.Equal(0, mill.CurFlour);
            Assert.Equal(5, held.Amount);

            ground.OnTarget(from, mill);

            Assert.Equal(3, mill.CurFlour);
        }
        finally
        {
            held.Delete();
            ground.Delete();
            from.Delete();
            other.Delete();
        }
    }
}
