using System;
using ModernUO.Serialization;
using Server.Engines.PartySystem;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MoonstoneGate : Moongate
{
    private Mobile _caster;

    public MoonstoneGate(Point3D loc, Map map, Map targetMap, Mobile caster, int hue) : base(loc, targetMap)
    {
        MoveToWorld(loc, map);
        Dispellable = false;
        Hue = hue;

        _caster = caster;

        new InternalTimer(this).Start();
        Effects.PlaySound(loc, map, 0x20E);
    }

    public override void CheckGate(Mobile m, int range)
    {
        if (m.Kills >= 5)
        {
            return;
        }

        var casterParty = Party.Get(_caster);
        var userParty = Party.Get(m);

        if (m == _caster || casterParty != null && userParty == casterParty)
        {
            base.CheckGate(m, range);
        }
    }

    public override void UseGate(Mobile m)
    {
        if (m.Kills >= 5)
        {
            return;
        }

        var casterParty = Party.Get(_caster);
        var userParty = Party.Get(m);

        if (m == _caster || casterParty != null && userParty == casterParty)
        {
            base.UseGate(m);
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Delete();
    }

    private class InternalTimer : Timer
    {
        private Item _item;

        public InternalTimer(Item item) : base(TimeSpan.FromSeconds(30.0)) => _item = item;

        protected override void OnTick() => _item.Delete();
    }
}
