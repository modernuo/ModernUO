using System;

namespace Server.Mobiles;

public class MeleeAI : BaseAI
{
    public MeleeAI(BaseCreature m) : base(m)
    {
    }

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

        if (combatant == null || combatant.Deleted || combatant.Map != Mobile.Map || !combatant.Alive ||
            combatant.IsDeadBondedPet)
        {
            DebugSay("My combatant is gone, so my guard is up");

            Action = ActionType.Guard;
            return true;
        }

        if (!Mobile.InRange(combatant, Mobile.RangePerception))
        {
            // They are somewhat far away, can we find something else?

            if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
            {
                Mobile.Combatant = Mobile.FocusMob;
                Mobile.FocusMob = null;
            }
            else if (!Mobile.InRange(combatant, Mobile.RangePerception * 3))
            {
                Mobile.Combatant = null;
            }

            combatant = Mobile.Combatant;

            if (combatant == null)
            {
                DebugSay("My combatant has fled, so I am on guard");

                Action = ActionType.Guard;
                return true;
            }
        }

        if (!MoveTo(combatant, false, Mobile.RangeFight))
        {
            if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
            {
                this.DebugSayFormatted($"My move is blocked, so I am going to attack {Mobile.FocusMob!.Name}");

                Mobile.Combatant = Mobile.FocusMob;
                Action = ActionType.Combat;
                return true;
            }

            if (Mobile.GetDistanceToSqrt(combatant) > Mobile.RangePerception + 1)
            {
                this.DebugSayFormatted($"I cannot find {combatant.Name}, so my guard is up");

                Action = ActionType.Guard;
                return true;
            }

            this.DebugSayFormatted($"I cannot find {combatant.Name}, so my guard is up");
        }
        else if (Core.TickCount - Mobile.LastMoveTime > 200)
        {
            Mobile.Direction = Mobile.GetDirectionTo(combatant);
        }

        // We are low on health, should we flee?
        if (!Mobile.Controlled && !Mobile.Summoned && Mobile.CanFlee && Mobile.Hits < Mobile.HitsMax * 20 / 100)
        {
            var fleeChance = 10 + Math.Max(0, combatant.Hits - Mobile.Hits); // (10 + diff)% chance to flee;
            if (Utility.Random(0, 100) < fleeChance)
            {
                this.DebugSayFormatted($"I am going to flee from {combatant.Name}");

                Action = ActionType.Flee;
                return true;
            }
        }

        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            DebugSay("I used my abilities!");
        }

        return true;
    }

    public override bool DoActionGuard()
    {
        if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
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

    public override bool DoActionFlee()
    {
        if (Mobile.Hits > Mobile.HitsMax / 2)
        {
            DebugSay("I am stronger now, so I will continue fighting");

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
