using System;
using Server.Engines.PartySystem;

namespace Server.Items
{
    public class MoonstoneGate : Moongate
    {
        private readonly Mobile m_Caster;

        public MoonstoneGate(Point3D loc, Map map, Map targetMap, Mobile caster, int hue) : base(loc, targetMap)
        {
            MoveToWorld(loc, map);
            Dispellable = false;
            Hue = hue;

            m_Caster = caster;

            new InternalTimer(this).Start();

            Effects.PlaySound(loc, map, 0x20E);
        }

        public MoonstoneGate(Serial serial) : base(serial)
        {
        }

        public override void CheckGate(Mobile m, int range)
        {
            if (m.Kills >= 5)
            {
                return;
            }

            var casterParty = Party.Get(m_Caster);
            var userParty = Party.Get(m);

            if (m == m_Caster || casterParty != null && userParty == casterParty)
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

            var casterParty = Party.Get(m_Caster);
            var userParty = Party.Get(m);

            if (m == m_Caster || casterParty != null && userParty == casterParty)
            {
                base.UseGate(m);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Delete();
        }

        private class InternalTimer : Timer
        {
            private readonly Item m_Item;

            public InternalTimer(Item item) : base(TimeSpan.FromSeconds(30.0))
            {
                m_Item = item;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                m_Item.Delete();
            }
        }
    }
}
