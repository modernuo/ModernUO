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

        if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I have detected {Mobile.FocusMob.Name} and I will attack");

            Mobile.Combatant = Mobile.FocusMob;
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
        var combatant = Mobile.Combatant;

        if (combatant == null || combatant.Deleted || combatant.Map != Mobile.Map || !combatant.Alive ||
            combatant.IsDeadBondedPet)
        {
            DebugSay("My combatant is gone, so my guard is up");

            Action = ActionType.Guard;
            return true;
        }

        if (!WalkMobileRange(combatant, 1, false, Mobile.RangeFight, Mobile.Weapon.MaxRange))
        {
            this.DebugSayFormatted($"I am still not in range of {combatant.Name}");

            if ((int)Mobile.GetDistanceToSqrt(combatant) > Mobile.RangePerception + 1)
            {
                this.DebugSayFormatted($"I have lost {combatant.Name}");

                Mobile.Combatant = null;
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

            return true;
        }

        // When we have no ammo, we flee
        var pack = Mobile.Backpack;

        if (pack?.FindItemByType<Arrow>() == null)
        {
            Action = ActionType.Flee;
            return true;
        }

        // At 20% we should check if we must leave
        if (Mobile.Combatant != null && Mobile.Hits < Mobile.HitsMax * 20 / 100 && Mobile.CanFlee)
        {
            // 10% to flee + the diff of hits
            var fleeChance = 10 + Math.Max(0, Mobile.Combatant.Hits - Mobile.Hits);

            if (Utility.Random(0, 100) > fleeChance)
            {
                Action = ActionType.Flee;
            }
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
}
