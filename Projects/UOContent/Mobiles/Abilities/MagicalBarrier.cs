using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server.Mobiles;

public class MagicalBarrier : MonsterAbility
{
    private HashSet<Mobile> _inactiveField;

    public bool HasField(Mobile source) => _inactiveField?.Contains(source) != true;

    public override MonsterAbilityType AbilityType => MonsterAbilityType.MagicalBarrier;

    public override MonsterAbilityTrigger AbilityTrigger =>
        MonsterAbilityTrigger.TakeDamage | MonsterAbilityTrigger.GiveDamage | MonsterAbilityTrigger.Think;

    public override TimeSpan MinTriggerCooldown => TimeSpan.FromSeconds(10.0);
    public override TimeSpan MaxTriggerCooldown => TimeSpan.FromSeconds(10.0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CanUseField(BaseCreature source) => source.Hits >= source.HitsMax * 9 / 10;

    // Regeneration is subject to the cooldown, the rest are not.
    public override bool CanTrigger(BaseCreature source, MonsterAbilityTrigger trigger) =>
        trigger != MonsterAbilityTrigger.Think || base.CanTrigger(source, trigger);

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        if (trigger == MonsterAbilityTrigger.Think)
        {
            if (CanUseField(source) && _inactiveField?.Remove(source) == true)
            {
                // Field going up!
                source.FixedParticles(0, 10, 0, 0x2530, EffectLayer.Waist);

                if (_inactiveField?.Count == 0)
                {
                    _inactiveField = null;
                }
            }
        }
        else if (trigger is MonsterAbilityTrigger.TakeSpellDamage && !CanUseField(source))
        {
            _inactiveField ??= [];
            if (_inactiveField.Add(source))
            {
                // TODO: message and effect when field turns down; cannot be verified on OSI due to a bug
                source.FixedParticles(0x3735, 1, 30, 0x251F, EffectLayer.Waist);
            }
        }

        base.Trigger(trigger, source, target);
    }

    public override void AlterMeleeDamageFrom(BaseCreature source, Mobile target, ref int damage)
    {
        if (HasField(source))
        {
            damage = 0; // no melee damage when the field is up
            source.FixedParticles(0x376A, 20, 10, 0x2530, EffectLayer.Waist);
            source.PlaySound(0x2F4);
            target.SendLocalizedMessage(1114360); // Your weapon cannot penetrate the creature's magical barrier.
        }
    }

    public override void AlterSpellDamageFrom(BaseCreature source, Mobile target, ref int damage)
    {
        if (!HasField(source))
        {
            damage = 0; // no spell damage when the field is down
            // should there be an effect when spells nullifying is on?
            source.FixedParticles(0, 10, 0, 0x2522, EffectLayer.Waist);
            target.SendLocalizedMessage(1114359); // Your attack has no effect on the creature's armor.
        }
    }

    public override void Move(BaseCreature source, Direction d)
    {
        base.Move(source, d);

        if (HasField(source) && source.Combatant != null)
        {
            source.FixedParticles(0, 10, 0, 0x2530, EffectLayer.Waist);
        }
    }
}
