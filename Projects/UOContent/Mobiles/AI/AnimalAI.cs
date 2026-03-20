namespace Server.Mobiles;

public class AnimalAI : BaseAI
{
    public AnimalAI(BaseCreature m) : base(m)
    {
    }

    public override double FleeHealthThreshold => 0.1; // 10% is default
    public override double FleeChance => 0.1; // 10% is default
    public override double BackoffChance => 0.5; // 50% is default

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
            base.DoActionWander();
        }

        return true;
    }

    public override bool DoActionCombat()
    {
        var combatant = Mobile.Combatant;

        if (!IsValidCombatant(combatant))
        {
            DebugSay("My combatant is gone!");

            Action = ActionType.Wander;
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

    public override bool DoActionFlee()
    {
        AcquireFocusMob(Mobile.RangePerception * 2, Mobile.FightMode, true, false, true);

        Mobile.FocusMob ??= Mobile.Combatant;

        return base.DoActionFlee();
    }
}
