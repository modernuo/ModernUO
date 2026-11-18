using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class RejuvinationAnkhTests
{
    [Fact]
    public void FourthRollRestoresHitsManaAndStamina()
    {
        var m = new PlayerMobile(World.NewMobile);
        m.DefaultMobileInit();
        m.RawStr = m.RawDex = m.RawInt = 50;
        m.MoveToWorld(new Point3D(1400, 1400, 0), Map.Felucca);
        var component = new RejuvinationAddonComponent(0x3);

        try
        {
            var restoredAll = false;

            // The roll is random, so double-click until the fourth roll comes up (1 in 4 each time).
            for (var i = 0; i < 200 && !restoredAll; i++)
            {
                m.Hits = 1;
                m.Mana = 0;
                m.Stam = 0;
                component.OnDoubleClick(m);
                restoredAll = m.Hits == m.HitsMax && m.Mana == m.ManaMax && m.Stam == m.StamMax;
                m.EndAction<RejuvinationAddonComponent>();
            }

            Assert.True(m.ManaMax > 0 && m.StamMax > 0);
            Assert.True(restoredAll);
        }
        finally
        {
            component.Delete();
            m.Delete();
        }
    }
}
