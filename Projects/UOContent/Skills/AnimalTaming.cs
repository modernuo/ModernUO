using System;
using System.Collections.Generic;
using Server.Engines.Virtues;
using Server.Factions;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Spellweaving;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class AnimalTaming
    {
        private static readonly HashSet<Mobile> m_BeingTamed = new();

        public static bool DisableMessage { get; set; }

        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.AnimalTaming].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.RevealingAction();

            m.Target = new InternalTarget();
            m.RevealingAction();

            if (!DisableMessage)
            {
                m.SendLocalizedMessage(502789); // Tame which animal?
            }

            return TimeSpan.FromSeconds(30);
        }

        public static bool CheckMastery(Mobile tamer, BaseCreature creature) =>
            SummonFamiliarSpell.Table.TryGetValue(tamer, out var bc)
            && bc is DarkWolfFamiliar { Deleted: false }
            && creature is DireWolf or GreyWolf or TimberWolf or WhiteWolf or BakeKitsune;

        public static bool MustBeSubdued(BaseCreature bc) =>
            bc.Owners.Count <= 0 && bc.SubdueBeforeTame && bc.Hits > bc.HitsMax / 10;

        public static void ScaleStats(BaseCreature bc, double scalar)
        {
            if (bc.RawStr > 0)
            {
                bc.RawStr = (int)Math.Max(1, bc.RawStr * scalar);
            }

            if (bc.RawDex > 0)
            {
                bc.RawDex = (int)Math.Max(1, bc.RawDex * scalar);
            }

            if (bc.RawInt > 0)
            {
                bc.RawInt = (int)Math.Max(1, bc.RawInt * scalar);
            }

            if (bc.HitsMaxSeed > 0)
            {
                bc.HitsMaxSeed = (int)Math.Max(1, bc.HitsMaxSeed * scalar);
                bc.Hits = bc.Hits;
            }

            if (bc.StamMaxSeed > 0)
            {
                bc.StamMaxSeed = (int)Math.Max(1, bc.StamMaxSeed * scalar);
                bc.Stam = bc.Stam;
            }
        }

        public static void ScaleSkills(BaseCreature bc, double scalar)
        {
            ScaleSkills(bc, scalar, scalar);
        }

        public static void ScaleSkills(BaseCreature bc, double scalar, double capScalar)
        {
            for (var i = 0; i < bc.Skills.Length; ++i)
            {
                bc.Skills[i].Base *= scalar;

                bc.Skills[i].Cap = Math.Max(100.0, bc.Skills[i].Cap * capScalar);

                if (bc.Skills[i].Base > bc.Skills[i].Cap)
                {
                    bc.Skills[i].Cap = bc.Skills[i].Base;
                }
            }
        }

        private class InternalTarget : Target
        {
            private bool m_SetSkillTime = true;

            public InternalTarget() : base(Core.AOS ? 3 : 2, false, TargetFlags.None)
            {
            }

            protected override void OnTargetFinish(Mobile from)
            {
                if (m_SetSkillTime)
                {
                    from.NextSkillTime = Core.TickCount;
                }
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                from.RevealingAction();

                if (targeted is not Mobile mobile)
                {
                    from.SendLocalizedMessage(502801); // You can't tame that!
                    return;
                }

                if (mobile is not BaseCreature creature)
                {
                    // That being cannot be tamed.
                    mobile.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502469, from.NetState);
                    return;
                }

                if (!creature.Tamable)
                {
                    // That creature cannot be tamed.
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1049655, from.NetState);
                    return;
                }

                if (creature.Controlled)
                {
                    // That animal looks tame already.
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502804, from.NetState);
                    return;
                }

                if (from.Female && !creature.AllowFemaleTamer)
                {
                    // That creature can only be tamed by males.
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1049653, from.NetState);
                    return;
                }

                if (!from.Female && !creature.AllowMaleTamer)
                {
                    // That creature can only be tamed by females.
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1049652, from.NetState);
                    return;
                }

                if (creature is CuSidhe && from.Race != Race.Elf)
                {
                    // You can't tame that!
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502801, from.NetState);
                    return;
                }

                if (from.Followers + creature.ControlSlots > from.FollowersMax)
                {
                    from.SendLocalizedMessage(1049611); // You have too many followers to tame that creature.
                    return;
                }

                if (creature.Owners.Count >= BaseCreature.MaxOwners && !creature.Owners.Contains(from))
                {
                    // This animal has had too many owners and is too upset for you to tame.
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1005615, from.NetState);
                    return;
                }

                if (MustBeSubdued(creature))
                {
                    // You must subdue this creature before you can tame it!
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1054025, from.NetState);
                    return;
                }

                if (!(CheckMastery(from, creature) || from.Skills.AnimalTaming.Value >= creature.MinTameSkill))
                {
                    // You have no chance of taming this creature.
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502806, from.NetState);
                    return;
                }

                if (creature is FactionWarHorse warHorse)
                {
                    var faction = Faction.Find(from);

                    if (faction == null || faction != warHorse.Faction)
                    {
                        // You cannot tame this creature.
                        creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042590, from.NetState);
                        return;
                    }
                }

                if (m_BeingTamed.Contains(creature))
                {
                    // Someone else is already taming this.
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502802, from.NetState);
                }
                else if (creature.CanAngerOnTame && Utility.RandomDouble() < 0.95)
                {
                    // You seem to anger the beast!
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502805, from.NetState);

                    creature.PlaySound(creature.GetAngerSound());
                    creature.Direction = creature.GetDirectionTo(from);

                    if (creature.BardPacified && Utility.RandomDouble() < 0.75)
                    {
                        Timer.StartTimer(TimeSpan.FromSeconds(2.0), () => Pacify(creature));
                    }
                    else
                    {
                        creature.BardEndTime = Core.Now;
                    }

                    creature.BardPacified = false;

                    creature.AIObject?.DoMove(creature.Direction);

                    if (from is PlayerMobile pm &&
                        !(VirtueSystem.GetVirtues(pm)?.HonorActive == true ||
                          TransformationSpellHelper.UnderTransformation(pm, typeof(EtherealVoyageSpell))))
                    {
                        creature.Combatant = pm;
                    }
                }
                else
                {
                    m_BeingTamed.Add(creature);

                    // You start to tame the creature.
                    from.LocalOverheadMessage(MessageType.Emote, 0x59, 1010597);

                    // *begins taming a creature.*
                    from.NonlocalOverheadMessage(MessageType.Emote, 0x59, 1010598);

                    new InternalTimer(from, creature, Utility.Random(3, 2)).Start();

                    m_SetSkillTime = false;
                }
            }

            private static void Pacify(BaseCreature bc) => bc.BardPacified = true; // Should use bc.Pacify with an end time?

            private class InternalTimer : Timer
            {
                private readonly BaseCreature m_Creature;
                private readonly int m_MaxCount;
                private readonly DateTime m_StartTime;
                private readonly Mobile m_Tamer;
                private int m_Count;
                private bool m_Paralyzed;

                public InternalTimer(Mobile tamer, BaseCreature creature, int count) : base(
                    TimeSpan.FromSeconds(3.0),
                    TimeSpan.FromSeconds(3.0),
                    count
                )
                {
                    m_Tamer = tamer;
                    m_Creature = creature;
                    m_MaxCount = count;
                    m_Paralyzed = creature.Paralyzed;
                    m_StartTime = Core.Now;
                }

                protected override void OnTick()
                {
                    m_Count++;

                    var de = m_Creature.FindMostRecentDamageEntry(false);
                    var alreadyOwned = m_Creature.Owners.Contains(m_Tamer);

                    if (!m_Tamer.InRange(m_Creature, Core.AOS ? 7 : 6))
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        // You are too far away to continue taming.
                        m_Creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502795, m_Tamer.NetState);
                        Stop();
                    }
                    else if (!m_Tamer.CheckAlive())
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        // You are dead, and cannot continue taming.
                        m_Creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502796, m_Tamer.NetState);
                        Stop();
                    }
                    else if (!m_Tamer.CanSee(m_Creature) || !m_Tamer.InLOS(m_Creature) || !CanPath())
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        // You do not have a clear path to the animal you are taming, and must cease your attempt.
                        m_Tamer.SendLocalizedMessage(1049654);
                        Stop();
                    }
                    else if (!m_Creature.Tamable)
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        // That creature cannot be tamed.
                        m_Creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1049655, m_Tamer.NetState);
                        Stop();
                    }
                    else if (m_Creature.Controlled)
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        // That animal looks tame already.
                        m_Creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502804, m_Tamer.NetState);
                        Stop();
                    }
                    else if (m_Creature.Owners.Count >= BaseCreature.MaxOwners && !m_Creature.Owners.Contains(m_Tamer))
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        // This animal has had too many owners and is too upset for you to tame.
                        m_Creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1005615, m_Tamer.NetState);
                        Stop();
                    }
                    else if (MustBeSubdued(m_Creature))
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        // You must subdue this creature before you can tame it!
                        m_Creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1054025, m_Tamer.NetState);
                        Stop();
                    }
                    else if (de?.LastDamage > m_StartTime)
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        // The animal is too angry to continue taming.
                        m_Creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502794, m_Tamer.NetState);
                        Stop();
                    }
                    else if (m_Count < m_MaxCount)
                    {
                        m_Tamer.RevealingAction();

                        switch (Utility.Random(3))
                        {
                            case 0:
                                {
                                    m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.Random(502790, 4));
                                    break;
                                }
                            case 1:
                                {
                                    m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.Random(1005608, 6));
                                    break;
                                }
                            case 2:
                                {
                                    m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.Random(1010593, 4));
                                    break;
                                }
                        }

                        if (!alreadyOwned) // Passively check animal lore for gain
                        {
                            m_Tamer.CheckTargetSkill(SkillName.AnimalLore, m_Creature, 0.0, 120.0);
                        }

                        if (m_Creature.Paralyzed)
                        {
                            m_Paralyzed = true;
                        }
                    }
                    else
                    {
                        m_Tamer.RevealingAction();
                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_BeingTamed.Remove(m_Creature);

                        if (m_Creature.Paralyzed)
                        {
                            m_Paralyzed = true;
                        }

                        if (!alreadyOwned) // Passively check animal lore for gain
                        {
                            m_Tamer.CheckTargetSkill(SkillName.AnimalLore, m_Creature, 0.0, 120.0);
                        }

                        var minSkill = m_Creature.MinTameSkill + m_Creature.Owners.Count * 6.0;

                        if (minSkill > -24.9 && CheckMastery(m_Tamer, m_Creature))
                        {
                            minSkill = -24.9; // 50% at 0.0?
                        }

                        minSkill += 24.9;

                        if (CheckMastery(m_Tamer, m_Creature) || alreadyOwned ||
                            m_Tamer.CheckTargetSkill(SkillName.AnimalTaming, m_Creature, minSkill - 25.0, minSkill + 25.0))
                        {
                            if (m_Creature.Owners.Count == 0) // First tame
                            {
                                if (m_Creature is GreaterDragon)
                                {
                                    ScaleSkills(m_Creature, 0.72, 0.90); // 72% of original skills trainable to 90%
                                    // Greater dragons have a 90% cap reduction and 90% skill reduction on magery
                                    m_Creature.Skills.Magery.Base = m_Creature.Skills.Magery.Cap;
                                }
                                else if (m_Paralyzed)
                                {
                                    // 86% of original skills if they were paralyzed during the taming
                                    ScaleSkills(m_Creature, 0.86);
                                }
                                else
                                {
                                    ScaleSkills(m_Creature, 0.90); // 90% of original skills
                                }

                                if (m_Creature.StatLossAfterTame)
                                {
                                    ScaleStats(m_Creature, 0.50);
                                }
                            }

                            if (alreadyOwned)
                            {
                                m_Tamer.SendLocalizedMessage(502797); // That wasn't even challenging.
                            }
                            else
                            {
                                // It seems to accept you as master.
                                m_Creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502799, m_Tamer.NetState);
                                m_Creature.Owners.Add(m_Tamer);
                            }

                            m_Creature.SetControlMaster(m_Tamer);
                            m_Creature.IsBonded = false;
                        }
                        else
                        {
                            // You fail to tame the creature.
                            m_Creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502798, m_Tamer.NetState);
                        }
                    }
                }

                private bool CanPath()
                {
                    IPoint3D p = m_Tamer;

                    return p != null && (m_Creature.InRange(new Point3D(p), 1) ||
                                         new MovementPath(m_Creature, new Point3D(p)).Success);
                }
            }
        }
    }
}
