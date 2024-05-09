using System;
using ModernUO.Serialization;

namespace Server.Factions;

[SerializationGenerator(0, false)]
public partial class FactionSpikeTrap : BaseFactionTrap
{
    [Constructible]
    public FactionSpikeTrap(Faction f = null, Mobile m = null) : base(f, m, 0x11A0)
    {
    }

    public override int LabelNumber => 1044601; // faction spike trap

    public override int AttackMessage => 1010545; // Large spikes in the ground spring up piercing your skin!

    public override int DisarmMessage =>
        1010541; // You carefully dismantle the trigger on the spikes and disable the trap.

    public override int EffectSound => 0x22E;
    public override int MessageHue => 0x5A;

    public override AllowedPlacing AllowedPlacing => AllowedPlacing.ControlledFactionTown;

    public override void DoVisibleEffect()
    {
        Effects.SendLocationEffect(Location, Map, 0x11A4, 12, 6);
    }

    public override void DoAttackEffect(Mobile m)
    {
        m.Damage(Utility.Dice(6, 10, 40), m);
    }
}

[SerializationGenerator(0, false)]
public partial class FactionSpikeTrapDeed : BaseFactionTrapDeed
{
    public FactionSpikeTrapDeed() : base(0x11A5)
    {
    }

    public override Type TrapType => typeof(FactionSpikeTrap);
    public override int LabelNumber => 1044605; // faction spike trap deed
}
