using Server;
using Server.ContextMenus;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class EjectPlayerTests
{
    [Fact]
    public void EjectTargetOutsideAnyHouseIsIgnored()
    {
        var from = new PlayerMobile { Player = true };
        var target = new PlayerMobile { Player = true };
        from.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        target.MoveToWorld(new Point3D(1002, 1000, 0), Map.Felucca);

        try
        {
            new EjectPlayerEntry().OnClick(from, target);
        }
        finally
        {
            from.Delete();
            target.Delete();
        }
    }
}
