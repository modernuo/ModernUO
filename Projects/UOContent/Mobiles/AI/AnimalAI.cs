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

        var hitPercent = (double)Mobile.Hits / Mobile.HitsMax;

        if (!Mobile.Summoned && !Mobile.Controlled && hitPercent < 0.1 && Mobile.CanFlee) // Less than 10% health
        {
            DebugSay("I am low on health!");

            Action = ActionType.Flee;
        }
        else if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
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

        if (combatant == null || combatant.Deleted || combatant.Map != Mobile.Map || !combatant.Alive ||
            combatant.IsDeadBondedPet)
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

            this.DebugSayFormatted($"I should be closer to {combatant.Name}");
        }
        else if (Core.TickCount - Mobile.LastMoveTime > 400)
        {
            Mobile.Direction = Mobile.GetDirectionTo(combatant);
        }

        if (!Mobile.Controlled && !Mobile.Summoned && Mobile.CanFlee)
        {
            var hitPercent = (double)Mobile.Hits / Mobile.HitsMax;

            if (hitPercent <= 0.1)
            {
                DebugSay("I am low on health!");

                Action = ActionType.Flee;
                return true;
            }
        }

        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            this.DebugSayFormatted($"I used my abilities on {combatant.Name}!");
        }

        return true;
    }

    public override bool DoActionBackoff()
    {
        var hitPercent = (double)Mobile.Hits / Mobile.HitsMax;

        if (!Mobile.Summoned && !Mobile.Controlled && hitPercent < 0.1 && Mobile.CanFlee) // Less than 10% health
        {
            Action = ActionType.Flee;
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

    public override bool DoActionFlee()
    {
        AcquireFocusMob(Mobile.RangePerception * 2, Mobile.FightMode, true, false, true);

        Mobile.FocusMob ??= Mobile.Combatant;

        return base.DoActionFlee();
    }
}
