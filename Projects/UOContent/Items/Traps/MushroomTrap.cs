using System;
using ModernUO.Serialization;
using Server.Regions;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MushroomTrap : BaseTrap
{
    [Constructible]
    public MushroomTrap() : base(0x1125)
    {
    }

    public override bool PassivelyTriggered => true;
    public override TimeSpan PassiveTriggerDelay => TimeSpan.Zero;
    public override int PassiveTriggerRange => 2;
    public override TimeSpan ResetDelay => TimeSpan.Zero;

    public override void OnTrigger(Mobile from)
    {
        if (!from.Alive || ItemID != 0x1125 || from.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        ItemID = 0x1126;
        Effects.PlaySound(Location, Map, 0x306);

        SpellHelper.Damage(TimeSpan.FromSeconds(0.5), from, from, Utility.Dice(2, 4, 0));

        Timer.StartTimer(TimeSpan.FromSeconds(2.0), OnMushroomReset);
    }

    public virtual void OnMushroomReset()
    {
        if (Region.Find(Location, Map).IsPartOf<DungeonRegion>())
        {
            ItemID = 0x1125; // reset
        }
        else
        {
            Delete();
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (ItemID == 0x1126)
        {
            OnMushroomReset();
        }
    }
}
