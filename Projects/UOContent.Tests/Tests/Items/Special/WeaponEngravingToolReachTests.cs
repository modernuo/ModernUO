using System.Reflection;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests;

[Collection("Sequential UOContent Tests")]
public class WeaponEngravingToolReachTests
{
    [Fact]
    public void ToolRefusesAWeaponHeldByAnotherPlayerOrFixedInPlace()
    {
        var from = new PlayerMobile { Player = true };
        from.MoveToWorld(new Point3D(1610, 1600, 0), Map.Felucca);
        using var state = PacketTestUtilities.CreateTestNetState();
        from.NetState = state;
        state.Mobile = from;
        var pack = new Backpack();
        from.AddItem(pack);
        var tool = new WeaponEngravingTool();
        pack.DropItem(tool);
        var other = new PlayerMobile { Player = true };
        other.MoveToWorld(new Point3D(1610, 1601, 0), Map.Felucca);
        var weapon = new Longsword();
        other.AddItem(weapon);
        var fixedWeapon = new Longsword { Movable = false };
        fixedWeapon.MoveToWorld(new Point3D(1611, 1600, 0), Map.Felucca);

        try
        {
            tool.OnDoubleClick(from);
            typeof(Target).GetMethod("OnTarget", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(from.Target, [from, weapon]);

            Assert.Null(state.FindGump<BaseGump>());

            tool.OnDoubleClick(from);
            typeof(Target).GetMethod("OnTarget", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(from.Target, [from, fixedWeapon]);

            Assert.Null(state.FindGump<BaseGump>());
        }
        finally
        {
            state.Mobile = null;
            from.NetState = null;
            tool.Delete();
            weapon.Delete();
            fixedWeapon.Delete();
            other.Delete();
            from.Delete();
        }
    }
}
