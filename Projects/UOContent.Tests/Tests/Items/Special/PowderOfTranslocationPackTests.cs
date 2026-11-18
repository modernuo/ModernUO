using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PowderOfTranslocationPackTests
{
    [Fact]
    public void PowderDoesNotChargeAnItemOutsideThePack()
    {
        var from = new PlayerMobile { Player = true };
        from.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        var pack = new Backpack();
        from.AddItem(pack);
        var powder = new PowderOfTranslocation(5);
        pack.DropItem(powder);
        var bracelet = new BraceletOfBinding();
        bracelet.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        var charges = bracelet.Charges;

        try
        {
            powder.OnDoubleClick(from);
            typeof(Target).GetMethod("OnTarget", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(from.Target, [from, bracelet]);

            Assert.Equal(charges, bracelet.Charges);
            Assert.Equal(5, powder.Amount);
        }
        finally
        {
            powder.Delete();
            bracelet.Delete();
            from.Delete();
        }
    }
}
