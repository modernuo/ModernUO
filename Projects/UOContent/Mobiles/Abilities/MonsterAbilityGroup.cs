using System;
using Server.Random;
using WeightedMonsterAbility = Server.Random.WeightedValue<Server.Mobiles.MonsterAbility>;

namespace Server.Mobiles;

public class MonsterAbilityGroup : MonsterAbility
{
    private WeightedMonsterAbility[] _weightedAbilities;
    private WeightedMonsterAbility[] _availableToTrigger;
    private int _availableToTriggerCount;
    private MonsterAbilityTrigger _triggers;

    public MonsterAbilityGroup(params WeightedMonsterAbility[] weightedAbilities)
    {
        _weightedAbilities = weightedAbilities;
        _availableToTrigger = new WeightedMonsterAbility[weightedAbilities.Length];

        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            var weightedAbility = _weightedAbilities[i];
            _triggers |= weightedAbility.Value.AbilityTrigger;
        }
    }

    public override MonsterAbilityTrigger AbilityTrigger => _triggers;

    public bool HasAbility(MonsterAbility ability)
    {
        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            if (_weightedAbilities[i].Value == ability)
            {
                return true;
            }
        }

        return false;
    }

    // Note: Does not get an ability randomly by weight
    // This is used for passives or lookups
    public MonsterAbility GetAbilityWithType(MonsterAbilityType type)
    {
        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            var ability = _weightedAbilities[i].Value;
            if (ability.AbilityType == type)
            {
                return ability;
            }
        }

        return null;
    }

    public override bool CanTrigger(BaseCreature source, MonsterAbilityTrigger trigger)
    {
        if (!base.CanTrigger(source, trigger))
        {
            return false;
        }

        _availableToTriggerCount = 0;

        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            var weightedAbility = _weightedAbilities[i];
            if (weightedAbility.Value.WillTrigger(trigger) && weightedAbility.Value.CanTrigger(source, trigger))
            {
                _availableToTrigger[_availableToTriggerCount++] = weightedAbility;
            }
        }

        return _availableToTriggerCount > 0;
    }

    // Note: Trigger will choose an ability at random by weight
    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        if (_availableToTriggerCount <= 0)
        {
            return;
        }

        var slice = new ReadOnlySpan<WeightedValue<MonsterAbility>>(_availableToTrigger, 0, _availableToTriggerCount);
        var chosenAbility = slice.RandomWeightedElement().Value;

        // Just in case?
        if (chosenAbility == null)
        {
            return;
        }

        base.Trigger(trigger, source, target);
        chosenAbility.Trigger(trigger, source, target);
        _availableToTriggerCount = 0; // Just in case trigger is called without CanTrigger
    }

    public override void AlterMeleeDamageFrom(BaseCreature source, Mobile target, ref int damage)
    {
        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            _weightedAbilities[i].Value.AlterMeleeDamageFrom(source, target, ref damage);
        }
    }

    public override void AlterMeleeDamageTo(BaseCreature source, Mobile target, ref int damage)
    {
        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            _weightedAbilities[i].Value.AlterMeleeDamageTo(source, target, ref damage);
        }
    }

    public override void AlterSpellDamageScalarFrom(BaseCreature source, Mobile target, ref double scalar)
    {
        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            _weightedAbilities[i].Value.AlterSpellDamageScalarFrom(source, target, ref scalar);
        }
    }

    public override void AlterSpellDamageScalarTo(BaseCreature source, Mobile target, ref double scalar)
    {
        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            _weightedAbilities[i].Value.AlterSpellDamageScalarTo(source, target, ref scalar);
        }
    }

    public override void AlterSpellDamageFrom(BaseCreature source, Mobile target, ref int damage)
    {
        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            _weightedAbilities[i].Value.AlterSpellDamageFrom(source, target, ref damage);
        }
    }

    public override void AlterSpellDamageTo(BaseCreature source, Mobile target, ref int damage)
    {
        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            _weightedAbilities[i].Value.AlterSpellDamageTo(source, target, ref damage);
        }
    }

    public override void Move(BaseCreature creature, Direction d)
    {
        for (var i = 0; i < _weightedAbilities.Length; i++)
        {
            _weightedAbilities[i].Value.Move(creature, d);
        }
    }
}
