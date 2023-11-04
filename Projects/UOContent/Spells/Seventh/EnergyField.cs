using System;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Seventh
{
    public class EnergyFieldSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Energy Field",
            "In Sanct Grav",
            221,
            9022,
            false,
            Reagent.BlackPearl,
            Reagent.MandrakeRoot,
            Reagent.SpidersSilk,
            Reagent.SulfurousAsh
        );

        public EnergyFieldSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Seventh;

        public void Target(IPoint3D p)
        {
            if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);
                SpellHelper.GetSurfaceTop(ref p);

                var loc = new Point3D(p);

                var eastToWest = SpellHelper.GetEastToWest(Caster.Location, loc);

                Effects.PlaySound(loc, Caster.Map, 0x20B);

                TimeSpan duration = Core.AOS
                    ? TimeSpan.FromSeconds((15 + Caster.Skills.Magery.Value * 2) / 7.0)
                    : TimeSpan.FromSeconds(Caster.Skills.Magery.Value * 0.28 + 2.0);

                var itemID = eastToWest ? 0x3946 : 0x3956;

                for (var i = -2; i <= 2; ++i)
                {
                    var targetLoc = new Point3D(eastToWest ? loc.X + i : loc.X, eastToWest ? loc.Y : loc.Y + i, loc.Z);
                    var canFit = SpellHelper.AdjustField(ref targetLoc, Caster.Map, 12, false);

                    if (!canFit)
                    {
                        continue;
                    }

                    Item item = new InternalItem(targetLoc, Caster.Map, duration, itemID, Caster);
                    item.ProcessDelta();

                    Effects.SendLocationParticles(
                        EffectItem.Create(targetLoc, Caster.Map, EffectItem.DefaultDuration),
                        0x376A,
                        9,
                        10,
                        5051
                    );
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this, range: Core.ML ? 10 : 12);
        }

        [DispellableField]
        private class InternalItem : Item
        {
            private readonly Mobile m_Caster;
            private readonly Timer m_Timer;

            public InternalItem(Point3D loc, Map map, TimeSpan duration, int itemID, Mobile caster) : base(itemID)
            {
                Visible = false;
                Movable = false;
                Light = LightType.Circle300;

                MoveToWorld(loc, map);

                m_Caster = caster;

                if (caster.InLOS(this))
                {
                    Visible = true;
                }
                else
                {
                    Delete();
                }

                if (Deleted)
                {
                    return;
                }

                m_Timer = new InternalTimer(this, duration);
                m_Timer.Start();
            }

            public InternalItem(Serial serial) : base(serial)
            {
                m_Timer = new InternalTimer(this, TimeSpan.FromSeconds(5.0));
                m_Timer.Start();
            }

            public override bool BlocksFit => true;

            public override void Serialize(IGenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write(0); // version
            }

            public override void Deserialize(IGenericReader reader)
            {
                base.Deserialize(reader);

                var version = reader.ReadInt();
            }

            public override bool OnMoveOver(Mobile m)
            {
                if (m is not PlayerMobile)
                {
                    return base.OnMoveOver(m);
                }

                var noto = Notoriety.Compute(m_Caster, m);
                return noto != Notoriety.Enemy && noto != Notoriety.Ally && base.OnMoveOver(m);
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                m_Timer?.Stop();
            }

            private class InternalTimer : Timer
            {
                private readonly InternalItem m_Item;

                public InternalTimer(InternalItem item, TimeSpan duration) : base(duration)
                {
                    m_Item = item;
                }

                protected override void OnTick()
                {
                    m_Item.Delete();
                }
            }
        }
    }
}
