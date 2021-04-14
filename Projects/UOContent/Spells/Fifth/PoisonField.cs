using System;
using System.Collections.Generic;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Fifth
{
    public class PoisonFieldSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo m_Info = new(
            "Poison Field",
            "In Nox Grav",
            230,
            9052,
            false,
            Reagent.BlackPearl,
            Reagent.Nightshade,
            Reagent.SpidersSilk
        );

        public PoisonFieldSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fifth;

        public void Target(IPoint3D p)
        {
            if (!Caster.CanSee(p))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                var dx = Caster.Location.X - p.X;
                var dy = Caster.Location.Y - p.Y;
                var rx = (dx - dy) * 44;
                var ry = (dx + dy) * 44;

                bool eastToWest;

                if (rx >= 0 && ry >= 0)
                {
                    eastToWest = false;
                }
                else if (rx >= 0)
                {
                    eastToWest = true;
                }
                else if (ry >= 0)
                {
                    eastToWest = true;
                }
                else
                {
                    eastToWest = false;
                }

                Effects.PlaySound(new Point3D(p), Caster.Map, 0x20B);

                var itemID = eastToWest ? 0x3915 : 0x3922;

                var duration = TimeSpan.FromSeconds(3 + Caster.Skills.Magery.Fixed / 25);

                for (var i = -2; i <= 2; ++i)
                {
                    var loc = new Point3D(eastToWest ? p.X + i : p.X, eastToWest ? p.Y : p.Y + i, p.Z);

                    new InternalItem(itemID, loc, Caster, Caster.Map, duration, i);
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this, TargetFlags.None, Core.ML ? 10 : 12, false);
        }

        [DispellableField]
        public class InternalItem : Item
        {
            private Mobile m_Caster;
            private DateTime m_End;
            private Timer m_Timer;

            public InternalItem(int itemID, Point3D loc, Mobile caster, Map map, TimeSpan duration, int val) : base(itemID)
            {
                var canFit = SpellHelper.AdjustField(ref loc, map, 12, false);

                Visible = false;
                Movable = false;
                Light = LightType.Circle300;

                MoveToWorld(loc, map);

                m_Caster = caster;

                m_End = Core.Now + duration;

                m_Timer = new InternalTimer(this, TimeSpan.FromSeconds(val.Abs() * 0.2), caster.InLOS(this), canFit);
                m_Timer.Start();
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

                writer.Write(1); // version

                writer.Write(m_Caster);
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
                            m_Caster = reader.ReadEntity<Mobile>();

                            goto case 0;
                        }
                    case 0:
                        {
                            m_End = reader.ReadDeltaTime();

                            m_Timer = new InternalTimer(this, TimeSpan.Zero, true, true);
                            m_Timer.Start();

                            break;
                        }
                }
            }

            public void ApplyPoisonTo(Mobile m)
            {
                if (m_Caster == null)
                {
                    return;
                }

                Poison p;

                if (Core.AOS)
                {
                    var total = (m_Caster.Skills.Magery.Fixed + m_Caster.Skills.Poisoning.Fixed) / 2;

                    if (total >= 1000)
                    {
                        p = Poison.Deadly;
                    }
                    else if (total > 850)
                    {
                        p = Poison.Greater;
                    }
                    else if (total > 650)
                    {
                        p = Poison.Regular;
                    }
                    else
                    {
                        p = Poison.Lesser;
                    }
                }
                else
                {
                    p = Poison.Regular;
                }

                if (m.ApplyPoison(m_Caster, p) == ApplyPoisonResult.Poisoned)
                {
                    if (SpellHelper.CanRevealCaster(m))
                    {
                        m_Caster.RevealingAction();
                    }
                }

                (m as BaseCreature)?.OnHarmfulSpell(m_Caster);
            }

            public override bool OnMoveOver(Mobile m)
            {
                if (Visible && m_Caster != null && (!Core.AOS || m != m_Caster) &&
                    SpellHelper.ValidIndirectTarget(m_Caster, m) && m_Caster.CanBeHarmful(m, false))
                {
                    m_Caster.DoHarmful(m);

                    ApplyPoisonTo(m);
                    m.PlaySound(0x474);
                }

                return true;
            }

            private class InternalTimer : Timer
            {
                private static readonly Queue<Mobile> m_Queue = new();
                private readonly bool m_CanFit;
                private readonly bool m_InLOS;
                private readonly InternalItem m_Item;

                public InternalTimer(InternalItem item, TimeSpan delay, bool inLOS, bool canFit) : base(
                    delay,
                    TimeSpan.FromSeconds(1.5)
                )
                {
                    m_Item = item;
                    m_InLOS = inLOS;
                    m_CanFit = canFit;

                    Priority = TimerPriority.FiftyMS;
                }

                protected override void OnTick()
                {
                    if (m_Item.Deleted)
                    {
                        return;
                    }

                    if (!m_Item.Visible)
                    {
                        if (m_InLOS && m_CanFit)
                        {
                            m_Item.Visible = true;
                        }
                        else
                        {
                            m_Item.Delete();
                        }

                        if (!m_Item.Deleted)
                        {
                            m_Item.ProcessDelta();
                            Effects.SendLocationParticles(
                                EffectItem.Create(m_Item.Location, m_Item.Map, EffectItem.DefaultDuration),
                                0x376A,
                                9,
                                10,
                                5040
                            );
                        }
                    }
                    else if (Core.Now > m_Item.m_End)
                    {
                        m_Item.Delete();
                        Stop();
                    }
                    else
                    {
                        var map = m_Item.Map;
                        var caster = m_Item.m_Caster;

                        if (map != null && caster != null)
                        {
                            var eastToWest = m_Item.ItemID == 0x3915;
                            var eable = map.GetMobilesInBounds(
                                new Rectangle2D(
                                    m_Item.X - (eastToWest ? 0 : 1),
                                    m_Item.Y - (eastToWest ? 1 : 0),
                                    eastToWest ? 1 : 2,
                                    eastToWest ? 2 : 1
                                )
                            );

                            foreach (var m in eable)
                            {
                                if (m.Z + 16 > m_Item.Z && m_Item.Z + 12 > m.Z && (!Core.AOS || m != caster) &&
                                    SpellHelper.ValidIndirectTarget(caster, m) && caster.CanBeHarmful(m, false))
                                {
                                    m_Queue.Enqueue(m);
                                }
                            }

                            eable.Free();

                            while (m_Queue.Count > 0)
                            {
                                var m = m_Queue.Dequeue();

                                caster.DoHarmful(m);

                                m_Item.ApplyPoisonTo(m);
                                m.PlaySound(0x474);
                            }
                        }
                    }
                }
            }
        }
    }
}
