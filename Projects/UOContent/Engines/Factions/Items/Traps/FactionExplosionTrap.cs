using System;
using ModernUO.Serialization;

namespace Server.Factions;

[SerializationGenerator(0, false)]
public partial class FactionExplosionTrap : BaseFactionTrap
{
    [Constructible]
    public FactionExplosionTrap(Faction f = null, Mobile m = null) : base(f, m, 0x11C1)
    {
    }

    public override int LabelNumber => 1044599; // faction explosion trap

    public override int AttackMessage => 1010543; // You are enveloped in an explosion of fire!
    public override int DisarmMessage => 1010539; // You carefully remove the pressure trigger and disable the trap.
    public override int EffectSound => 0x307;
    public override int MessageHue => 0x78;

    public override AllowedPlacing AllowedPlacing => AllowedPlacing.AnyFactionTown;

    public override void DoVisibleEffect()
    {
        Effects.SendLocationEffect(GetWorldLocation(), Map, 0x36BD, 15);
    }

    public override void DoAttackEffect(Mobile m)
    {
        m.Damage(Utility.Dice(6, 10, 40), m);
    }
}

[SerializationGenerator(0, false)]
public partial class FactionExplosionTrapDeed : BaseFactionTrapDeed
{
    public FactionExplosionTrapDeed() : base(0x36D2)
    {
    }

    public override Type TrapType => typeof(FactionExplosionTrap);
    public override int LabelNumber => 1044603; // faction explosion trap deed
}
