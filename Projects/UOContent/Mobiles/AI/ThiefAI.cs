using System;
using Server.Items;

namespace Server.Mobiles;

public class ThiefAI : BaseAI
{
    private Item m_toDisarm;

    public ThiefAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("I have no combatant");
        }

        if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"I have detected {m_Mobile.FocusMob.Name}, attacking");
            }

            m_Mobile.Combatant = m_Mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            base.DoActionWander();
        }

        return true;
    }

    public override bool DoActionCombat()
    {
        var combatant = m_Mobile.Combatant;

        if (combatant?.Deleted != false || combatant.Map != m_Mobile.Map)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("My combatant is gone, so my guard is up");
            }

            Action = ActionType.Guard;

            return true;
        }

        if (!WalkMobileRange(combatant, 1, true, m_Mobile.RangeFight, m_Mobile.RangeFight))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"I should be closer to {combatant.Name}");
            }
        }
        else
        {
            if (m_toDisarm?.IsChildOf(m_Mobile.Backpack) != false)
            {
                m_toDisarm = combatant.FindItemOnLayer(Layer.OneHanded) ?? combatant.FindItemOnLayer(Layer.TwoHanded);
            }

            if (!Core.AOS && !m_Mobile.DisarmReady && m_Mobile.Skills.Wrestling.Value >= 80.0 &&
                m_Mobile.Skills.ArmsLore.Value >= 80.0 && m_toDisarm != null)
            {
                EventSink.InvokeDisarmRequest(m_Mobile);
            }

            if (m_toDisarm?.IsChildOf(combatant.Backpack) == true &&
                Core.TickCount - m_Mobile.NextSkillTime >= 0 && m_toDisarm.LootType != LootType.Blessed &&
                m_toDisarm.LootType != LootType.Newbied)
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("Trying to steal from combatant.");
                }

                m_Mobile.UseSkill(SkillName.Stealing);
                m_Mobile.Target?.Invoke(m_Mobile, m_toDisarm);
            }
            else if (m_toDisarm == null && Core.TickCount - m_Mobile.NextSkillTime >= 0)
            {
                var cpack = combatant.Backpack;

                if (cpack != null)
                {
                    Item steala = cpack.FindItemByType<Bandage>();
                    if (steala != null)
                    {
                        if (m_Mobile.Debug)
                        {
                            m_Mobile.DebugSay("Trying to steal from combatant.");
                        }

                        m_Mobile.UseSkill(SkillName.Stealing);
                        m_Mobile.Target?.Invoke(m_Mobile, steala);
                    }

                    Item stealb = cpack.FindItemByType<Nightshade>();
                    if (stealb != null)
                    {
                        if (m_Mobile.Debug)
                        {
                            m_Mobile.DebugSay("Trying to steal from combatant.");
                        }

                        m_Mobile.UseSkill(SkillName.Stealing);
                        m_Mobile.Target?.Invoke(m_Mobile, stealb);
                    }

                    Item stealc = cpack.FindItemByType<BlackPearl>();
                    if (stealc != null)
                    {
                        if (m_Mobile.Debug)
                        {
                            m_Mobile.DebugSay("Trying to steal from combatant.");
                        }

                        m_Mobile.UseSkill(SkillName.Stealing);
                        m_Mobile.Target?.Invoke(m_Mobile, stealc);
                    }

                    Item steald = cpack.FindItemByType<MandrakeRoot>();
                    if (steald != null)
                    {
                        if (m_Mobile.Debug)
                        {
                            m_Mobile.DebugSay("Trying to steal from combatant.");
                        }

                        m_Mobile.UseSkill(SkillName.Stealing);
                        m_Mobile.Target?.Invoke(m_Mobile, steald);
                    }
                    else if (steala == null && stealb == null && stealc == null)
                    {
                        if (m_Mobile.Debug)
                        {
                            m_Mobile.DebugSay($"I am going to flee from {combatant.Name}");
                        }

                        Action = ActionType.Flee;
                    }
                }
            }
        }

        if (m_Mobile.Hits >= m_Mobile.HitsMax * 20 / 100 || !m_Mobile.CanFlee)
        {
            return true;
        }

        // We are low on health, should we flee?
        // (10 + diff)% chance to flee
        var fleeChance = 10 + Math.Max(0, combatant.Hits - m_Mobile.Hits);

        if (Utility.Random(0, 100) > fleeChance)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"I am going to flee from {combatant.Name}");
            }

            Action = ActionType.Flee;
        }

        return true;
    }

    public override bool DoActionGuard()
    {
        if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"I have detected {m_Mobile.FocusMob.Name}, attacking");
            }

            m_Mobile.Combatant = m_Mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            base.DoActionGuard();
        }

        return true;
    }

    public override bool DoActionFlee()
    {
        if (m_Mobile.Hits > m_Mobile.HitsMax / 2)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I am stronger now, so I will continue fighting");
            }

            Action = ActionType.Combat;
        }
        else
        {
            m_Mobile.FocusMob = m_Mobile.Combatant;
            base.DoActionFlee();
        }

        return true;
    }
}