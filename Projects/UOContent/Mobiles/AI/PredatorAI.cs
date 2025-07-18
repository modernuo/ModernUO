namespace Server.Mobiles;

public class PredatorAI : BaseAI
{
    public PredatorAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        if (_mobile.Combatant != null)
        {
            DebugSay("I am hurt or being attacked, I kill him");

            Action = ActionType.Combat;
        }
        else if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, true, false, true))
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
        var combatant = _mobile.Combatant;

        if (combatant == null || combatant.Deleted || combatant.Map != _mobile.Map || !combatant.Alive ||
            combatant.IsDeadBondedPet)
        {
            DebugSay("My combatant is gone, so my guard is up");

            Action = ActionType.Guard;
            return true;
        }

        if (!WalkMobileRange(combatant, 1, true, _mobile.RangeFight, _mobile.RangeFight))
        {
            if (_mobile.GetDistanceToSqrt(combatant) > _mobile.RangePerception + 1)
            {
                this.DebugSayFormatted($"I cannot find {combatant.Name}");

                Action = ActionType.Wander;
                return true;
            }

            this.DebugSayFormatted($"I should be closer to {combatant.Name}");
        }

        if (_mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            this.DebugSayFormatted($"I used my abilities on {combatant.Name}!");
        }

        return true;
    }

    public override bool DoActionBackoff()
    {
        if (_mobile.IsHurt() || _mobile.Combatant != null)
        {
            Action = ActionType.Combat;
        }
        else if (AcquireFocusMob(_mobile.RangePerception * 2, FightMode.Closest, true, false, true))
        {
            if (WalkMobileRange(_mobile.FocusMob, 1, false, _mobile.RangePerception, _mobile.RangePerception * 2))
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
