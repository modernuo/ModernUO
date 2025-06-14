namespace Server.Mobiles;

public class PredatorAI : BaseAI
{
    public PredatorAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        if (m_Mobile.Combatant != null)
        {
            DebugSay("I am hurt or being attacked, I kill him");

            Action = ActionType.Combat;
        }
        else if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, true, false, true))
        {
            DebugSay("There is something near, I go away");

            Action = ActionType.Backoff;
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
            DebugSay("My combatant is gone, so my guard is up");

            Action = ActionType.Guard;
            return true;
        }

        if (!WalkMobileRange(combatant, 1, true, m_Mobile.RangeFight, m_Mobile.RangeFight))
        {
            if (m_Mobile.GetDistanceToSqrt(combatant) > m_Mobile.RangePerception + 1)
            {
                DebugSay($"I cannot find {combatant.Name}");

                Action = ActionType.Wander;
                return true;
            }

            DebugSay($"I should be closer to {combatant.Name}");
        }

        if (m_Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            DebugSay($"I used my abilities on {combatant.Name}!");
        }

        return true;
    }

    public override bool DoActionBackoff()
    {
        if (m_Mobile.IsHurt() || m_Mobile.Combatant != null)
        {
            Action = ActionType.Combat;
        }
        else if (AcquireFocusMob(m_Mobile.RangePerception * 2, FightMode.Closest, true, false, true))
        {
            if (WalkMobileRange(m_Mobile.FocusMob, 1, false, m_Mobile.RangePerception, m_Mobile.RangePerception * 2))
            {
                DebugSay("Well, here I am safe");

                Action = ActionType.Wander;
            }
        }
        else
        {
            DebugSay("I have lost my focus, lets relax");

            Action = ActionType.Wander;
        }

        return true;
    }
}
