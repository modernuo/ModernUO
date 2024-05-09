using System;
using ModernUO.Serialization;

namespace Server.Factions;

[SerializationGenerator(0, false)]
public partial class FactionSawTrap : BaseFactionTrap
{
    [Constructible]
    public FactionSawTrap(Faction f = null, Mobile m = null) : base(f, m, 0x11AC)
    {
    }

    public override int LabelNumber => 1041047; // faction saw trap

    public override int AttackMessage => 1010544; // The blade cuts deep into your skin!
    public override int DisarmMessage => 1010540; // You carefully dismantle the saw mechanism and disable the trap.
    public override int EffectSound => 0x218;
    public override int MessageHue => 0x5A;

    public override AllowedPlacing AllowedPlacing => AllowedPlacing.ControlledFactionTown;

    public override void DoVisibleEffect()
    {
        Effects.SendLocationEffect(Location, Map, 0x11AD, 25);
    }

    public override void DoAttackEffect(Mobile m)
    {
        m.Damage(Utility.Dice(6, 10, 40), m);
    }
}

[SerializationGenerator(0, false)]
public partial class FactionSawTrapDeed : BaseFactionTrapDeed
{
    public FactionSawTrapDeed() : base(0x1107)
    {
    }

    public override Type TrapType => typeof(FactionSawTrap);
    public override int LabelNumber => 1044604; // faction saw trap deed
}
