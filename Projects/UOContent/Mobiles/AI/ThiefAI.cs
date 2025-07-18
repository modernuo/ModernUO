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

        if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
        {
            DebugSay($"I have detected {_mobile.FocusMob.Name}, attacking");

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
            DebugSay($"I should be closer to {combatant.Name}");
        }
        else
        {
            if (_toDisarm?.IsChildOf(_mobile.Backpack) != false)
            {
                _toDisarm = combatant.FindItemOnLayer(Layer.OneHanded) ?? combatant.FindItemOnLayer(Layer.TwoHanded);
            }

            if (!Core.AOS && !_mobile.DisarmReady && _mobile.Skills.Wrestling.Value >= 80.0 &&
                _mobile.Skills.ArmsLore.Value >= 80.0 && _toDisarm != null)
            {
                Fists.DisarmRequest(_mobile);
            }

            if (_toDisarm?.IsChildOf(combatant.Backpack) == true &&
                Core.TickCount - _mobile.NextSkillTime >= 0 && _toDisarm.LootType != LootType.Blessed &&
                _toDisarm.LootType != LootType.Newbied)
            {
                DebugSay("Trying to steal from combatant.");

                _mobile.UseSkill(SkillName.Stealing);
                _mobile.Target?.Invoke(_mobile, _toDisarm);
            }
            else if (_toDisarm == null && Core.TickCount - _mobile.NextSkillTime >= 0)
            {
                DebugSay($"Trying to steal from {combatant.Name}.");

                bool didSteal = TryStealFrom<Bandage>(combatant);
                didSteal = TryStealFrom<Nightshade>(combatant) || didSteal;
                didSteal = TryStealFrom<BlackPearl>(combatant) || didSteal;
                didSteal = TryStealFrom<MandrakeRoot>(combatant) || didSteal;

                if (!didSteal)
                {
                    DebugSay($"I am going to flee from {combatant.Name}");

                    Action = ActionType.Flee;
                    return true;
                }
            }
        }

        // We are low on health, should we flee?
        // (10 + diff)% chance to flee
        if (_mobile.Hits < _mobile.HitsMax * 20 / 100 && _mobile.CanFlee)
        {
            var fleeChance = 10 + Math.Max(0, combatant.Hits - _mobile.Hits);

            if (Utility.Random(0, 100) > fleeChance)
            {
                DebugSay($"I am going to flee from {combatant.Name}");

                Action = ActionType.Flee;
            }

            return true;
        }

        if (_mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, _mobile.Combatant))
        {
            DebugSay($"I used my abilities on {_mobile.Combatant.Name}!");
        }
        return true;
    }

    private bool TryStealFrom<T>(Mobile combatant) where T : Item
    {
        Item steal = combatant.Backpack?.FindItemByType<T>();
        if (steal != null)
        {
            _mobile.UseSkill(SkillName.Stealing);
            _mobile.Target?.Invoke(_mobile, steal);
            return true;
        }

        return false;
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
