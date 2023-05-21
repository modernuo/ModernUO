using System;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Sixth
{
    public class ParalyzeFieldSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Paralyze Field",
            "In Ex Grav",
            230,
            9012,
            false,
            Reagent.BlackPearl,
            Reagent.Ginseng,
            Reagent.SpidersSilk
        );

        public ParalyzeFieldSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Sixth;

        public void Target(IPoint3D p)
        {
            if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);
                SpellHelper.GetSurfaceTop(ref p);

                var loc = new Point3D(p);
                var eastToWest = SpellHelper.GetEastToWest(Caster.Location, loc);

                Effects.PlaySound(loc, Caster.Map, 0x20B);

                var itemID = eastToWest ? 0x3967 : 0x3979;

                var duration = Core.Expansion switch
                {
                    Expansion.None  => TimeSpan.FromSeconds(20),
                    < Expansion.LBR => TimeSpan.FromSeconds(15.0 + Caster.Skills.Magery.Value / 3.0),
                    _               => TimeSpan.FromSeconds(3.0 + Caster.Skills.Magery.Value / 3.0)
                };

                for (var i = -2; i <= 2; ++i)
                {
                    var targetLoc = new Point3D(eastToWest ? loc.X + i : loc.X, eastToWest ? loc.Y : loc.Y + i, loc.Z);

                    if (!SpellHelper.AdjustField(ref targetLoc, Caster.Map, 12, false))
                    {
                        continue;
                    }

                    Item item = new InternalItem(Caster, itemID, targetLoc, Caster.Map, duration);
                    item.ProcessDelta();

                    Effects.SendLocationParticles(
                        EffectItem.Create(targetLoc, Caster.Map, EffectItem.DefaultDuration),
                        0x376A,
                        9,
                        10,
                        5048
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
        public class InternalItem : Item
        {
            private Mobile m_Caster;
            private DateTime m_End;
            private Timer m_Timer;

            public InternalItem(Mobile caster, int itemID, Point3D loc, Map map, TimeSpan duration) : base(itemID)
            {
                Visible = false;
                Movable = false;
                Light = LightType.Circle300;

                MoveToWorld(loc, map);

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

                m_Caster = caster;

                m_Timer = new InternalTimer(this, duration);
                m_Timer.Start();

                m_End = Core.Now + duration;
            }

            public InternalItem(Serial serial) : base(serial)
            {
            }

            public override bool BlocksFit => true;

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                m_Timer?.Stop();
            }

            public override void Serialize(IGenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write(0); // version

                writer.Write(m_Caster);
                writer.WriteDeltaTime(m_End);
            }

            public override void Deserialize(IGenericReader reader)
            {
                base.Deserialize(reader);

                var version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            m_Caster = reader.ReadEntity<Mobile>();
                            m_End = reader.ReadDeltaTime();

                            m_Timer = new InternalTimer(this, m_End - Core.Now);
                            m_Timer.Start();

                            break;
                        }
                }
            }

            public override bool OnMoveOver(Mobile m)
            {
                if (Visible && m_Caster != null && (!Core.AOS || m != m_Caster) &&
                    SpellHelper.ValidIndirectTarget(m_Caster, m) && m_Caster.CanBeHarmful(m, false))
                {
                    if (SpellHelper.CanRevealCaster(m))
                    {
                        m_Caster.RevealingAction();
                    }

                    m_Caster.DoHarmful(m);

                    double duration;

                    if (Core.AOS)
                    {
                        duration = Math.Max(
                            2.0 + ((int)(m_Caster.Skills.EvalInt.Value / 10) - (int)(m.Skills.MagicResist.Value / 10)),
                            0.0
                        );

                        if (!m.Player)
                        {
                            duration *= 3.0;
                        }
                    }
                    else
                    {
                        duration = 7.0 + m_Caster.Skills.Magery.Value / 5;
                    }

                    m.Paralyze(TimeSpan.FromSeconds(duration));

                    m.PlaySound(0x204);
                    m.FixedEffect(0x376A, 10, 16);

                    (m as BaseCreature)?.OnHarmfulSpell(m_Caster);
                }

                return true;
            }

            private class InternalTimer : Timer
            {
                private readonly Item m_Item;

                public InternalTimer(Item item, TimeSpan duration) : base(duration)
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
