namespace Server.Mobiles;

public class BerserkAI : BaseAI
{
    public BerserkAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        DebugSay("I have no combatant");

        if (AcquireFocusMob(_mobile.RangePerception, FightMode.Closest, false, true, true))
        {
            this.DebugSayFormatted($"I have detected {_mobile.FocusMob.Name} and I will attack");

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
            DebugSay("My combatant is gone, so my guard is up");

            Action = ActionType.Guard;
            return true;
        }

        if (!WalkMobileRange(combatant, 1, true, _mobile.RangeFight, _mobile.RangeFight))
        {
            this.DebugSayFormatted($"I am still not in range of {combatant.Name}");

            if ((int)_mobile.GetDistanceToSqrt(combatant) > _mobile.RangePerception + 1)
            {
                this.DebugSayFormatted($"I have lost {combatant.Name}");

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
        }

        return true;
    }

    public override bool DoActionGuard()
    {
        if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, true, true))
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
