using System;
using Server.Items;

namespace Server.Mobiles;

public class ArcherAI : BaseAI
{
    public ArcherAI(BaseCreature m) : base(m)
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
                m_Mobile.DebugSay($"I have detected {m_Mobile.FocusMob.Name} and I will attack");
            }

            m_Mobile.Combatant = m_Mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            return base.DoActionWander();
        }

        return true;
    }

    public override bool DoActionCombat()
    {
        if (m_Mobile.Combatant?.Deleted != false || !m_Mobile.Combatant.Alive ||
            m_Mobile.Combatant.IsDeadBondedPet)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("My combatant is deleted");
            }

            Action = ActionType.Guard;
            return true;
        }

        if (Core.TickCount - m_Mobile.LastMoveTime > 1000)
        {
            if (
                m_Mobile.Combatant != null &&
                !WalkMobileRange(
                    m_Mobile.Combatant,
                    1,
                    true,
                    m_Mobile.RangeFight,
                    m_Mobile.Weapon.MaxRange
                )
            )
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"I am still not in range of {m_Mobile.Combatant.Name}");
                }

                if ((int)m_Mobile.GetDistanceToSqrt(m_Mobile.Combatant) > m_Mobile.RangePerception + 1)
                {
                    if (m_Mobile.Debug)
                    {
                        m_Mobile.DebugSay($"I have lost {m_Mobile.Combatant.Name}");
                    }

                    m_Mobile.Combatant = null;
                    Action = ActionType.Guard;
                    return true;
                }
            }
        }

        // When we have no ammo, we flee
        var pack = m_Mobile.Backpack;

        if (pack?.FindItemByType<Arrow>() == null)
        {
            Action = ActionType.Flee;
            return true;
        }

        // At 20% we should check if we must leave
        if (m_Mobile.Combatant != null && m_Mobile.Hits < m_Mobile.HitsMax * 20 / 100 && m_Mobile.CanFlee)
        {
            // 10% to flee + the diff of hits
            var fleeChance = 10 + Math.Max(0, m_Mobile.Combatant.Hits - m_Mobile.Hits);

            if (Utility.Random(0, 100) > fleeChance)
            {
                Action = ActionType.Flee;
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
}