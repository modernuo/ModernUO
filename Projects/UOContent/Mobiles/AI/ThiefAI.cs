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
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"Trying to steal from {combatant.Name}.");
                }

                bool didSteal = TryStealFrom<Bandage>(combatant);
                didSteal = TryStealFrom<Nightshade>(combatant) || didSteal;
                didSteal = TryStealFrom<BlackPearl>(combatant) || didSteal;
                didSteal = TryStealFrom<MandrakeRoot>(combatant) || didSteal;

                if (!didSteal)
                {
                    if (m_Mobile.Debug)
                    {
                        m_Mobile.DebugSay($"I am going to flee from {combatant.Name}");
                    }

                    Action = ActionType.Flee;
                    return true;
                }
            }
        }

        // We are low on health, should we flee?
        // (10 + diff)% chance to flee
        if (m_Mobile.Hits < m_Mobile.HitsMax * 20 / 100 && m_Mobile.CanFlee)
        {
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

        if (m_Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, m_Mobile.Combatant))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"I used my abilities on {m_Mobile.Combatant.Name}!");
            }
        }
        return true;
    }

    private bool TryStealFrom<T>(Mobile combatant) where T : Item
    {
        Item steal = combatant.Backpack?.FindItemByType<T>();
        if (steal != null)
        {
            m_Mobile.UseSkill(SkillName.Stealing);
            m_Mobile.Target?.Invoke(m_Mobile, steal);
            return true;
        }

        return false;
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
