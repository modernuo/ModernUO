namespace Server.Mobiles;

public class BerserkAI : BaseAI
{
    public BerserkAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        DebugSay("I have no combatant");

        if (AcquireFocusMob(Mobile.RangePerception, FightMode.Closest, false, true, true))
        {
            this.DebugSayFormatted($"I have detected {Mobile.FocusMob.Name} and I will attack");

            Mobile.Combatant = Mobile.FocusMob;
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
            this.DebugSayFormatted($"I am still not in range of {combatant.Name}");

            if ((int)Mobile.GetDistanceToSqrt(combatant) > Mobile.RangePerception + 1)
            {
                this.DebugSayFormatted($"I have lost {combatant.Name}");

                Action = ActionType.Guard;
                return true;
            }
        }
        else if (Core.TickCount - Mobile.LastMoveTime > 400)
        {
            Mobile.Direction = Mobile.GetDirectionTo(combatant);
        }

        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            this.DebugSayFormatted($"I used my abilities on {combatant.Name}!");
        }

        return true;
    }

    public override bool DoActionGuard()
    {
        if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, true, true))
        {
            this.DebugSayFormatted($"I have detected {Mobile.FocusMob.Name}, attacking");

            Mobile.Combatant = Mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            base.DoActionGuard();
        }

        return true;
    }
}
