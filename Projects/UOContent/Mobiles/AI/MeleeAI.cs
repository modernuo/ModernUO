using System;

namespace Server.Mobiles;

public class MeleeAI : BaseAI
{
    public MeleeAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
        {
            DebugSay($"I have detected {_mobile.FocusMob.Name}, attacking");

            _mobile.Combatant = _mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            DebugSay("I am wandering");

            _mobile.Warmode = false;

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

        if (!_mobile.InRange(combatant, _mobile.RangePerception))
        {
            // They are somewhat far away, can we find something else?

            if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
            {
                _mobile.Combatant = _mobile.FocusMob;
                _mobile.FocusMob = null;
            }
            else if (!_mobile.InRange(combatant, _mobile.RangePerception * 3))
            {
                _mobile.Combatant = null;
            }

            combatant = _mobile.Combatant;

            if (combatant == null)
            {
                DebugSay("My combatant has fled, so I am on guard");

                Action = ActionType.Guard;
                return true;
            }
        }

        if (!MoveTo(combatant, true, _mobile.RangeFight))
        {
            _mobile.Direction = _mobile.GetDirectionTo(combatant);
            if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
            {
                DebugSay($"My move is blocked, so I am going to attack {_mobile.FocusMob!.Name}");

                _mobile.Combatant = _mobile.FocusMob;
                Action = ActionType.Combat;
                return true;
            }

            if (_mobile.GetDistanceToSqrt(combatant) > _mobile.RangePerception + 1)
            {
                DebugSay($"I cannot find {combatant.Name}, so my guard is up");

                Action = ActionType.Guard;
                return true;
            }

            DebugSay($"I cannot find {combatant.Name}, so my guard is up");
        }
        else if (Core.TickCount - _mobile.LastMoveTime > 400)
        {
            _mobile.Direction = _mobile.GetDirectionTo(combatant);
        }

        if (!_mobile.Controlled && !_mobile.Summoned && _mobile.CanFlee)
        {
            if (_mobile.Hits < _mobile.HitsMax * 20 / 100)
            {
                // We are low on health, should we flee?

                var fleeChance = 10 + Math.Max(0, combatant.Hits - _mobile.Hits); // (10 + diff)% chance to flee;
                if (Utility.Random(0, 100) < fleeChance)
                {
                    DebugSay($"I am going to flee from {combatant.Name}");

                    Action = ActionType.Flee;
                    return true;
                }
            }
        }

        if (_mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            DebugSay("I used my abilities!");
        }

        return true;
    }

    public override bool DoActionGuard()
    {
        if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
        {
            DebugSay($"I have detected {_mobile.FocusMob.Name}, attacking");

            _mobile.Combatant = _mobile.FocusMob;
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
        if (_mobile.Hits > _mobile.HitsMax / 2)
        {
            DebugSay("I am stronger now, so I will continue fighting");

            Action = ActionType.Combat;
        }
        else
        {
            _mobile.FocusMob = _mobile.Combatant;
            base.DoActionFlee();
        }

        return true;
    }
}
