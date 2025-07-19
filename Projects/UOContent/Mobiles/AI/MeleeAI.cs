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
            if (Mobile.Debug)
            {
                Mobile.DebugSay($"I have detected {Mobile.FocusMob.Name}, attacking");
            }

            Mobile.Combatant = Mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I am wandering");
            }

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
            if (Mobile.Debug)
            {
                Mobile.DebugSay("My combatant is gone, so my guard is up");
            }

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
                if (Mobile.Debug)
                {
                    Mobile.DebugSay("My combatant has fled, so I am on guard");
                }

                Action = ActionType.Guard;
                return true;
            }
        }

        if (!MoveTo(combatant, true, Mobile.RangeFight))
        {
            if (Mobile.InRange(combatant, 1))
            {
                Mobile.Direction = Mobile.GetDirectionTo(combatant);
            }

            if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
            {
                if (Mobile.Debug)
                {
                    Mobile.DebugSay($"My move is blocked, so I am going to attack {Mobile.FocusMob!.Name}");
                }

                Mobile.Combatant = Mobile.FocusMob;
                Action = ActionType.Combat;
                return true;
            }

            if (Mobile.GetDistanceToSqrt(combatant) > Mobile.RangePerception + 1)
            {
                if (Mobile.Debug)
                {
                    Mobile.DebugSay($"I cannot find {combatant.Name}, so my guard is up");
                }

                Action = ActionType.Guard;
                return true;
            }

            if (Mobile.Debug)
            {
                Mobile.DebugSay($"I cannot find {combatant.Name}, so my guard is up");
            }
        }
        else if (Core.TickCount - Mobile.LastMoveTime > 400)
        {
            Mobile.Direction = Mobile.GetDirectionTo(combatant);
        }

        if (!Mobile.Controlled && !Mobile.Summoned && Mobile.CanFlee)
        {
            if (Mobile.Hits < Mobile.HitsMax * 20 / 100)
            {
                // We are low on health, should we flee?

                var fleeChance = 10 + Math.Max(0, combatant.Hits - Mobile.Hits); // (10 + diff)% chance to flee;
                if (Utility.Random(0, 100) < fleeChance)
                {
                    if (Mobile.Debug)
                    {
                        Mobile.DebugSay($"I am going to flee from {combatant.Name}");
                    }

                    Action = ActionType.Flee;
                    return true;
                }
            }
        }

        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I used my abilities!");
            }
        }

        return true;
    }

    public override bool DoActionGuard()
    {
        if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay($"I have detected {Mobile.FocusMob.Name}, attacking");
            }

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
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I am stronger now, so I will continue fighting");
            }

            Mobile.PlaySound(Mobile.GetAttackSound());
            Mobile.CurrentSpeed = Mobile.ActiveSpeed;
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
