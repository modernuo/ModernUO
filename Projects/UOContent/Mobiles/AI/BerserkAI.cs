namespace Server.Mobiles;

public class BerserkAI : BaseAI
{
    public BerserkAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("I have no combatant");
        }

        if (AcquireFocusMob(m_Mobile.RangePerception, FightMode.Closest, false, true, true))
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
            base.DoActionWander();
        }

        return true;
    }

    public override bool DoActionCombat()
    {
        var combatant = m_Mobile.Combatant;

        if (combatant == null || combatant.Deleted || combatant.Map != m_Mobile.Map || !combatant.Alive ||
            combatant.IsDeadBondedPet)
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
                m_Mobile.DebugSay($"I am still not in range of {combatant.Name}");
            }

            if ((int)m_Mobile.GetDistanceToSqrt(combatant) > m_Mobile.RangePerception + 1)
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"I have lost {combatant.Name}");
                }

                Action = ActionType.Guard;
                return true;
            }
        }
        else if (Core.TickCount - m_Mobile.LastMoveTime > 400)
        {
            m_Mobile.Direction = m_Mobile.GetDirectionTo(combatant);
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

    public override bool DoActionGuard()
    {
        if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, true, true))
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
