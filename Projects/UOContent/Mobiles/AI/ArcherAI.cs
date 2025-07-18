using System;
using Server.Items;

namespace Server.Mobiles;

public class ArcherAI : BaseAI
{
    public ArcherAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        DebugSay("I have no combatant");

        if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I have detected {_mobile.FocusMob.Name} and I will attack");

            _mobile.Combatant = _mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            return base.DoActionWander();
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

        if (Core.TickCount - _mobile.LastMoveTime > 1000 && !WalkMobileRange(
                combatant,
                1,
                true,
                _mobile.RangeFight,
                _mobile.Weapon.MaxRange
            ))
        {
            this.DebugSayFormatted($"I am still not in range of {combatant.Name}");

            if ((int)_mobile.GetDistanceToSqrt(combatant) > _mobile.RangePerception + 1)
            {
                this.DebugSayFormatted($"I have lost {combatant.Name}");

                _mobile.Combatant = null;
                Action = ActionType.Guard;
                return true;
            }
        }
        else if (Core.TickCount - _mobile.LastMoveTime > 400)
        {
            _mobile.Direction = _mobile.GetDirectionTo(combatant);
        }

        if (_mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            this.DebugSayFormatted($"I used my abilities on {combatant.Name}!");

            return true;
        }

        // When we have no ammo, we flee
        var pack = _mobile.Backpack;

        if (pack?.FindItemByType<Arrow>() == null)
        {
            Action = ActionType.Flee;
            return true;
        }

        // At 20% we should check if we must leave
        if (_mobile.Combatant != null && _mobile.Hits < _mobile.HitsMax * 20 / 100 && _mobile.CanFlee)
        {
            // 10% to flee + the diff of hits
            var fleeChance = 10 + Math.Max(0, _mobile.Combatant.Hits - _mobile.Hits);

            if (Utility.Random(0, 100) > fleeChance)
            {
                Action = ActionType.Flee;
            }
        }

        return true;
    }

    public override bool DoActionGuard()
    {
        if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I have detected {_mobile.FocusMob.Name}, attacking");

            _mobile.Combatant = _mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            base.DoActionGuard();
        }

        return true;
    }
}
