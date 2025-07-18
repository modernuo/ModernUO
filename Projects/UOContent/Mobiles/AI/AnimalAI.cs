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

        var hitPercent = (double)_mobile.Hits / _mobile.HitsMax;

        if (!_mobile.Summoned && !_mobile.Controlled && hitPercent < 0.1 && _mobile.CanFlee) // Less than 10% health
        {
            DebugSay("I am low on health!");

            Action = ActionType.Flee;
        }
        else if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I have detected {_mobile.FocusMob.Name}, attacking");

            _mobile.Combatant = _mobile.FocusMob;
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
        var combatant = _mobile.Combatant;

        if (combatant == null || combatant.Deleted || combatant.Map != _mobile.Map || !combatant.Alive ||
            combatant.IsDeadBondedPet)
        {
            DebugSay("My combatant is gone!");

            Action = ActionType.Wander;
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
        else if (Core.TickCount - _mobile.LastMoveTime > 400)
        {
            _mobile.Direction = _mobile.GetDirectionTo(combatant);
        }

        if (!_mobile.Controlled && !_mobile.Summoned && _mobile.CanFlee)
        {
            var hitPercent = (double)_mobile.Hits / _mobile.HitsMax;

            if (hitPercent <= 0.1)
            {
                DebugSay("I am low on health!");

                Action = ActionType.Flee;
                return true;
            }
        }

        if (_mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            this.DebugSayFormatted($"I used my abilities on {combatant.Name}!");
        }

        return true;
    }

    public override bool DoActionBackoff()
    {
        var hitPercent = (double)_mobile.Hits / _mobile.HitsMax;

        if (!_mobile.Summoned && !_mobile.Controlled && hitPercent < 0.1 && _mobile.CanFlee) // Less than 10% health
        {
            Action = ActionType.Flee;
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

    public override bool DoActionFlee()
    {
        AcquireFocusMob(_mobile.RangePerception * 2, _mobile.FightMode, true, false, true);

        _mobile.FocusMob ??= _mobile.Combatant;

        return base.DoActionFlee();
    }
}
