using System;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Third
{
    public class WallOfStoneSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Wall of Stone",
            "In Sanct Ylem",
            227,
            9011,
            false,
            Reagent.Bloodmoss,
            Reagent.Garlic
        );

        public WallOfStoneSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Third;

        public void Target(IPoint3D p)
        {
            if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                var loc = new Point3D(p);

                var eastToWest = SpellHelper.GetEastToWest(Caster.Location, loc);

                Effects.PlaySound(loc, Caster.Map, 0x1F6);

                for (var i = -1; i <= 1; ++i)
                {
                    var targetLoc = new Point3D(eastToWest ? p.X + i : p.X, eastToWest ? p.Y : p.Y + i, p.Z);
                    var canFit = SpellHelper.AdjustField(ref targetLoc, Caster.Map, 22, true);

                    // Effects.SendLocationParticles( EffectItem.Create( loc, Caster.Map, EffectItem.DefaultDuration ), 0x376A, 9, 10, 5025 );

                    if (!canFit)
                    {
                        continue;
                    }

                    Item item = new InternalItem(targetLoc, Caster.Map, Caster);

                    Effects.SendLocationParticles(item, 0x376A, 9, 10, 5025);

                    // new InternalItem( loc, Caster.Map, Caster );
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
            private DateTime m_End;
            private Timer m_Timer;

            public InternalItem(Point3D loc, Map map, Mobile caster) : base(0x82)
            {
                Visible = false;
                Movable = false;

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

                m_Timer = new InternalTimer(this, TimeSpan.FromSeconds(10.0));
                m_Timer.Start();

                m_End = Core.Now + TimeSpan.FromSeconds(10.0);
            }

            public InternalItem(Serial serial) : base(serial)
            {
            }

            public override bool BlocksFit => true;

            public override void Serialize(IGenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write(1); // version

                writer.WriteDeltaTime(m_End);
            }

            public override void Deserialize(IGenericReader reader)
            {
                base.Deserialize(reader);

                var version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            m_End = reader.ReadDeltaTime();

                            m_Timer = new InternalTimer(this, m_End - Core.Now);
                            m_Timer.Start();

                            break;
                        }
                    case 0:
                        {
                            var duration = TimeSpan.FromSeconds(10.0);

                            m_Timer = new InternalTimer(this, duration);
                            m_Timer.Start();

                            m_End = Core.Now + duration;

                            break;
                        }
                }
            }

            public override bool OnMoveOver(Mobile m)
            {
                if (m is PlayerMobile)
                {
                    var noto = Notoriety.Compute(m_Caster, m);
                    if (noto is Notoriety.Enemy or Notoriety.Ally)
                    {
                        return false;
                    }
                }

                return base.OnMoveOver(m);
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
