using System;
using ModernUO.Serialization;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GiantSpikeTrap : BaseTrap
{
    [Constructible]
    public GiantSpikeTrap() : base(1)
    {
    }

    public override bool PassivelyTriggered => true;
    public override TimeSpan PassiveTriggerDelay => TimeSpan.Zero;
    public override int PassiveTriggerRange => 3;
    public override TimeSpan ResetDelay => TimeSpan.FromSeconds(0.0);

    public override void OnTrigger(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        Effects.SendLocationEffect(Location, Map, 0x1D99, 48, 2, GetEffectHue());

        if (from.Alive && CheckRange(from.Location, 0))
        {
            SpellHelper.Damage(TimeSpan.FromTicks(1), from, from, Utility.Dice(10, 7, 0));
        }
    }
}
