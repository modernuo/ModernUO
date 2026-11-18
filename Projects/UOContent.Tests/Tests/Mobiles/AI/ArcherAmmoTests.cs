using Server.Items;
using Server.Mobiles;
using Xunit;

namespace Server.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class ArcherAmmoTests
{
    private sealed class Stub : BaseCreature
    {
        public Stub() : base(AIType.AI_Archer) => Body = 0xC9;

        public override bool PlayerRangeSensitive => false;
    }

    [Fact]
    public void CrossbowArcherWithBolts_KeepsFighting()
    {
        var archer = new Stub();
        var target = new Stub();

        try
        {
            archer.MoveToWorld(new Point3D(1500, 1600, 0), Map.Felucca);
            target.MoveToWorld(new Point3D(1500, 1603, 0), Map.Felucca);
            archer.EquipItem(new Crossbow());
            var pack = new Backpack();
            archer.AddItem(pack);
            pack.DropItem(new Bolt(10));
            archer.Combatant = target;
            archer.AIObject.Action = ActionType.Combat;

            archer.AIObject.DoActionCombat();

            Assert.Equal(ActionType.Combat, archer.AIObject.Action);
        }
        finally
        {
            archer.Delete();
            target.Delete();
        }
    }
}
