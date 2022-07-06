using System;

namespace Server.Mobiles;

public class MeleeAI : BaseAI
{
    public MeleeAI(BaseCreature m) : base(m)
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

        if (combatant?.Deleted != false || combatant.Map != m_Mobile.Map || !combatant.Alive ||
            combatant.IsDeadBondedPet)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("My combatant is gone, so my guard is up");
            }

            Action = ActionType.Guard;

            return true;
        }

        if (!m_Mobile.InRange(combatant, m_Mobile.RangePerception))
        {
            // They are somewhat far away, can we find something else?

            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                m_Mobile.Combatant = m_Mobile.FocusMob;
                m_Mobile.FocusMob = null;
            }
            else if (!m_Mobile.InRange(combatant, m_Mobile.RangePerception * 3))
            {
                m_Mobile.Combatant = null;
            }

            combatant = m_Mobile.Combatant;

            if (combatant == null)
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("My combatant has fled, so I am on guard");
                }

                Action = ActionType.Guard;

                return true;
            }
        }

        if (!MoveTo(combatant, true, m_Mobile.RangeFight))
        {
            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"My move is blocked, so I am going to attack {m_Mobile.FocusMob!.Name}");
                }

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;

                return true;
            }

            if (m_Mobile.GetDistanceToSqrt(combatant) > m_Mobile.RangePerception + 1)
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"I cannot find {combatant.Name}, so my guard is up");
                }

                Action = ActionType.Guard;

                return true;
            }

            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"I cannot find {combatant.Name}, so my guard is up");
            }
        }

        if (!m_Mobile.Controlled && !m_Mobile.Summoned && m_Mobile.CanFlee)
        {
            if (m_Mobile.Hits < m_Mobile.HitsMax * 20 / 100)
            {
                // We are low on health, should we flee?

                var fleeChance = 10 + Math.Max(0, combatant.Hits - m_Mobile.Hits); // (10 + diff)% chance to flee;
                if (Utility.Random(0, 100) < fleeChance)
                {
                    if (m_Mobile.Debug)
                    {
                        m_Mobile.DebugSay($"I am going to flee from {combatant.Name}");
                    }

                    Action = ActionType.Flee;
                }
            }
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