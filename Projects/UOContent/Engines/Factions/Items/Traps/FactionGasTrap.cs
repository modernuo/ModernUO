using System;
using ModernUO.Serialization;

namespace Server.Factions;

[SerializationGenerator(0, false)]
public partial class FactionGasTrap : BaseFactionTrap
{
    [Constructible]
    public FactionGasTrap(Faction f = null, Mobile m = null) : base(f, m, 0x113C)
    {
    }

    public override int LabelNumber => 1044598; // faction gas trap

    public override int AttackMessage => 1010542; // A noxious green cloud of poison gas envelops you!
    public override int DisarmMessage => 502376;  // The poison leaks harmlessly away due to your deft touch.
    public override int EffectSound => 0x230;
    public override int MessageHue => 0x44;

    public override AllowedPlacing AllowedPlacing => AllowedPlacing.FactionStronghold;

    public override void DoVisibleEffect()
    {
        Effects.SendLocationEffect(Location, Map, 0x3709, 28, 10, 0x1D3, 5);
    }

    public override void DoAttackEffect(Mobile m)
    {
        m.ApplyPoison(m, Poison.Lethal);
    }
}

[SerializationGenerator(0, false)]
public partial class FactionGasTrapDeed : BaseFactionTrapDeed
{
    public FactionGasTrapDeed() : base(0x11AB)
    {
    }

    public override Type TrapType => typeof(FactionGasTrap);
    public override int LabelNumber => 1044602; // faction gas trap deed
}
