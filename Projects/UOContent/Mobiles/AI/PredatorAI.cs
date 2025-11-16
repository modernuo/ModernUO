namespace Server.Mobiles;

public class PredatorAI : BaseAI
{
    public PredatorAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        if (Mobile.Combatant != null)
        {
            DebugSay("I am hurt or being attacked, I kill him");

            Action = ActionType.Combat;
        }
        else if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, true, false, true))
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
        var combatant = Mobile.Combatant;

        if (combatant == null || combatant.Deleted || combatant.Map != Mobile.Map || !combatant.Alive ||
            combatant.IsDeadBondedPet)
        {
            DebugSay("My combatant is gone, so my guard is up");

            Action = ActionType.Guard;
            return true;
        }

        if (!WalkMobileRange(combatant, 1, false, Mobile.RangeFight, Mobile.RangeFight))
        {
            if (Mobile.GetDistanceToSqrt(combatant) > Mobile.RangePerception + 1)
            {
                this.DebugSayFormatted($"I cannot find {combatant.Name}");

                Action = ActionType.Wander;
                return true;
            }

            this.DebugSayFormatted($"I should be closer to {combatant.Name}");
        }

        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            this.DebugSayFormatted($"I used my abilities on {combatant.Name}!");
        }

        return true;
    }

    public override bool DoActionBackoff()
    {
        if (Mobile.IsHurt() || Mobile.Combatant != null)
        {
            Action = ActionType.Combat;
        }
        else if (AcquireFocusMob(Mobile.RangePerception * 2, FightMode.Closest, true, false, true))
        {
            if (WalkMobileRange(Mobile.FocusMob, 1, false, Mobile.RangePerception, Mobile.RangePerception * 2))
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
