namespace Server.Mobiles;

public class MeleeAI : BaseAI
{
    public MeleeAI(BaseCreature m) : base(m)
    {
    }

    public override double FleeHealthThreshold => 0.2; // 20% is default
    public override double FleeChance => 0.1; // 10% is default

    public override bool DoActionWander()
    {
        if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I have detected {Mobile.FocusMob.Name}, attacking");
            Mobile.Combatant = Mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            DebugSay("I am wandering");
            Mobile.Warmode = false;
            base.DoActionWander();
        }

        return true;
    }

    public override bool DoActionCombat()
    {
        var combatant = Mobile.Combatant;

        if (!IsValidCombatant(combatant))
        {
            DebugSay("My combatant is gone, so my guard is up");
            Action = ActionType.Guard;
            return true;
        }

        if (!Mobile.InRange(combatant, Mobile.RangePerception))
        {
            if (!HandleOutOfRangeCombatant(combatant))
            {
                return true;
            }
            combatant = Mobile.Combatant;
        }

        if (!AttemptMoveToCombatant(combatant))
        {
            return true;
        }

        if (Core.TickCount - Mobile.LastMoveTime > 400)
        {
            Mobile.Direction = Mobile.GetDirectionTo(combatant);
        }

        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            DebugSay("I used my abilities!");
        }

        return true;
    }

    private bool IsValidCombatant(Mobile combatant)
    {
        return combatant?.Deleted == false 
               && combatant.Map == Mobile.Map 
               && combatant.Alive 
               && !combatant.IsDeadBondedPet;
    }

    private bool HandleOutOfRangeCombatant(Mobile combatant)
    {
        if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            Mobile.Combatant = Mobile.FocusMob;
            Mobile.FocusMob = null;
            return true;
        }

        if (!Mobile.InRange(combatant, Mobile.RangePerception * 3))
        {
            Mobile.Combatant = null;
        }

        if (Mobile.Combatant == null)
        {
            DebugSay("My combatant has fled, so I am on guard.");
            Action = ActionType.Guard;
            return false;
        }

        return true;
    }

    private bool AttemptMoveToCombatant(Mobile combatant)
    {
        if (MoveTo(combatant, false, Mobile.RangeFight))
        {
            return true;
        }

        if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"My move is blocked, so I am going to attack {Mobile.FocusMob.Name}.");
            Mobile.Combatant = Mobile.FocusMob;
            Action = ActionType.Combat;
            return true;
        }

        if (Mobile.GetDistanceToSqrt(combatant) > Mobile.RangePerception + 1)
        {
            this.DebugSayFormatted($"I cannot find {combatant.Name}, so my guard is up.");
            Action = ActionType.Guard;
            return false;
        }

        this.DebugSayFormatted($"I cannot reach {combatant.Name} but continuing to try.");
        return true;
    }

    public override bool DoActionGuard()
    {
        if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I have detected {Mobile.FocusMob.Name}, attacking.");
            Mobile.Combatant = Mobile.FocusMob;
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
        if (Mobile.Hits > Mobile.HitsMax * FleeHealthThreshold)
        {
            DebugSay("I am stronger now, so I will continue fighting.");
            Action = ActionType.Combat;
        }
        else
        {
            Mobile.FocusMob = Mobile.Combatant;
            base.DoActionFlee();
        }

        return true;
    }
}
