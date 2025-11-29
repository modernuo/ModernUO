using System;
using Server.Items;

namespace Server.Mobiles;

public class ThiefAI : BaseAI
{
    private Item _toDisarm;

    public ThiefAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        DebugSay("I have no combatant");

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

        if (combatant == null || combatant.Deleted || combatant.Map != Mobile.Map || !combatant.Alive ||
            combatant.IsDeadBondedPet)
        {
            DebugSay("My combatant is gone, so my guard is up");

            Action = ActionType.Guard;
            return true;
        }

        if (!WalkMobileRange(combatant, 1, false, Mobile.RangeFight, Mobile.RangeFight))
        {
            this.DebugSayFormatted($"I should be closer to {combatant.Name}");
        }
        else
        {
            if (_toDisarm?.IsChildOf(Mobile.Backpack) != false)
            {
                _toDisarm = combatant.FindItemOnLayer(Layer.OneHanded) ?? combatant.FindItemOnLayer(Layer.TwoHanded);
            }

            if (!Core.AOS && !Mobile.DisarmReady && Mobile.Skills.Wrestling.Value >= 80.0 &&
                Mobile.Skills.ArmsLore.Value >= 80.0 && _toDisarm != null)
            {
                Fists.DisarmRequest(Mobile);
            }

            if (_toDisarm?.IsChildOf(combatant.Backpack) == true &&
                Core.TickCount - Mobile.NextSkillTime >= 0 && _toDisarm.LootType != LootType.Blessed &&
                _toDisarm.LootType != LootType.Newbied)
            {
                DebugSay("Trying to steal from combatant.");

                Mobile.UseSkill(SkillName.Stealing);
                Mobile.Target?.Invoke(Mobile, _toDisarm);
            }
            else if (_toDisarm == null && Core.TickCount - Mobile.NextSkillTime >= 0)
            {
                this.DebugSayFormatted($"Trying to steal from {combatant.Name}.");

                bool didSteal = TryStealFrom<Bandage>(combatant);
                didSteal = TryStealFrom<Nightshade>(combatant) || didSteal;
                didSteal = TryStealFrom<BlackPearl>(combatant) || didSteal;
                didSteal = TryStealFrom<MandrakeRoot>(combatant) || didSteal;

                if (!didSteal)
                {
                    this.DebugSayFormatted($"I am going to flee from {combatant.Name}");

                    Action = ActionType.Flee;
                    return true;
                }
            }
        }

        // We are low on health, should we flee?
        // (10 + diff)% chance to flee
        if (Mobile.Hits < Mobile.HitsMax * 20 / 100 && Mobile.CanFlee)
        {
            var fleeChance = 10 + Math.Max(0, combatant.Hits - Mobile.Hits);

            if (Utility.Random(0, 100) > fleeChance)
            {
                this.DebugSayFormatted($"I am going to flee from {combatant.Name}");

                Action = ActionType.Flee;
            }

            return true;
        }

        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, Mobile.Combatant))
        {
            this.DebugSayFormatted($"I used my abilities on {Mobile.Combatant.Name}!");
        }
        return true;
    }

    private bool TryStealFrom<T>(Mobile combatant) where T : Item
    {
        Item steal = combatant.Backpack?.FindItemByType<T>();
        if (steal != null)
        {
            Mobile.UseSkill(SkillName.Stealing);
            Mobile.Target?.Invoke(Mobile, steal);
            return true;
        }

        return false;
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
