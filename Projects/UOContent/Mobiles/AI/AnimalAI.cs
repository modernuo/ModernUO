// Ideas
// When you run on animals the panic
// When if (distance < 8 && Utility.RandomDouble() * Math.Sqrt( (8 - distance) / 6 ) >= incoming.Skills.AnimalTaming.Value)
// More your close, the more it can panic
/*
 * AnimalHunterAI, AnimalHidingAI, AnimalDomesticAI...
 *
 */

namespace Server.Mobiles;

public class AnimalAI : BaseAI
{
    public AnimalAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        // New, only flee @ 10%

        var hitPercent = (double)m_Mobile.Hits / m_Mobile.HitsMax;

        if (!m_Mobile.Summoned && !m_Mobile.Controlled && hitPercent < 0.1 && m_Mobile.CanFlee) // Less than 10% health
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I am low on health!");
            }

            Action = ActionType.Flee;
        }
        else if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
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
                m_Mobile.DebugSay("My combatant is gone..");
            }

            Action = ActionType.Wander;
            return true;
        }

        if (!WalkMobileRange(combatant, 1, true, m_Mobile.RangeFight, m_Mobile.RangeFight))
        {
            if (m_Mobile.GetDistanceToSqrt(combatant) > m_Mobile.RangePerception + 1)
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"I cannot find {combatant.Name}");
                }

                Action = ActionType.Wander;
                return true;
            }

            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"I should be closer to {combatant.Name}");
            }
        }

        if (!m_Mobile.Controlled && !m_Mobile.Summoned && m_Mobile.CanFlee)
        {
            var hitPercent = (double)m_Mobile.Hits / m_Mobile.HitsMax;

            if (hitPercent <= 0.1)
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("I am low on health!");
                }

                Action = ActionType.Flee;
                return true;
            }
        }

        if (m_Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"I used my abilities on {combatant.Name}!");
            }
        }

        return true;
    }

    public override bool DoActionBackoff()
    {
        var hitPercent = (double)m_Mobile.Hits / m_Mobile.HitsMax;

        if (!m_Mobile.Summoned && !m_Mobile.Controlled && hitPercent < 0.1 && m_Mobile.CanFlee) // Less than 10% health
        {
            Action = ActionType.Flee;
        }
        else if (AcquireFocusMob(m_Mobile.RangePerception * 2, FightMode.Closest, true, false, true))
        {
            if (WalkMobileRange(m_Mobile.FocusMob, 1, false, m_Mobile.RangePerception, m_Mobile.RangePerception * 2))
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("Well, here I am safe");
                }

                Action = ActionType.Wander;
            }
        }
        else
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I have lost my focus, lets relax");
            }

            Action = ActionType.Wander;
        }

        return true;
    }

    public override bool DoActionFlee()
    {
        AcquireFocusMob(m_Mobile.RangePerception * 2, m_Mobile.FightMode, true, false, true);

        m_Mobile.FocusMob ??= m_Mobile.Combatant;

        return base.DoActionFlee();
    }
}
