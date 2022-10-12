using System;
using Server.Collections;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Fifth
{
    public class PoisonFieldSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Poison Field",
            "In Nox Grav",
            230,
            9052,
            false,
            Reagent.BlackPearl,
            Reagent.Nightshade,
            Reagent.SpidersSilk
        );

        public PoisonFieldSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fifth;

        public void Target(IPoint3D p)
        {
            if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);
                SpellHelper.GetSurfaceTop(ref p);

                var loc = new Point3D(p);
                var eastToWest = SpellHelper.GetEastToWest(Caster.Location, loc);

                Effects.PlaySound(loc, Caster.Map, 0x20B);

                var itemID = eastToWest ? 0x3915 : 0x3922;
                var duration = Core.Expansion switch
                {
                    Expansion.None  => TimeSpan.FromSeconds(20),
                    < Expansion.LBR => TimeSpan.FromSeconds(15 + Caster.Skills.Magery.Value * 0.4),
                    _               => TimeSpan.FromSeconds(3 + Caster.Skills.Magery.Fixed * 0.4)
                };

                for (var i = -2; i <= 2; ++i)
                {
                    var targetLoc = new Point3D(eastToWest ? loc.X + i : loc.X, eastToWest ? loc.Y : loc.Y + i, loc.Z);

                    new InternalItem(itemID, targetLoc, Caster, Caster.Map, duration, i);
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
                    p = ((m_Caster.Skills.Magery.Fixed + m_Caster.Skills.Poisoning.Fixed) / 2) switch
                    {
                        >= 1000 => Poison.Deadly,
                        > 850   => Poison.Greater,
                        > 650   => Poison.Regular,
                        _       => Poison.Lesser
                    };
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

                            using var queue = PooledRefQueue<Mobile>.Create();
                            foreach (var m in eable)
                            {
                                if (m.Z + 16 > m_Item.Z && m_Item.Z + 12 > m.Z && (!Core.AOS || m != caster) &&
                                    SpellHelper.ValidIndirectTarget(caster, m) && caster.CanBeHarmful(m, false))
                                {
                                    queue.Enqueue(m);
                                }
                            }

                            eable.Free();

                            while (queue.Count > 0)
                            {
                                var m = queue.Dequeue();

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
