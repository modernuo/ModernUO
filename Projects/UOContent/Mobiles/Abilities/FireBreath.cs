using System;
using Server.Spells.Bushido;

namespace Server.Mobiles;

public class FireBreath : MonsterAbility
{
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.Think;
    public override MonsterAbilityType AbilityType => MonsterAbilityType.FireBreath;
    public override double ChanceToTrigger => 0.5;

    // Min/max seconds until next breath
    public override TimeSpan MinTriggerCooldown => TimeSpan.FromSeconds(30.0);
    public override TimeSpan MaxTriggerCooldown => TimeSpan.FromSeconds(45.0);

    // Base damage given is: CurrentHitPoints * BreathDamageScalar
    public virtual double BreathDamageScalar => Core.AOS ? 0.16 : 0.05;

    // Creature stops moving for 1.0 seconds while breathing
    public virtual double BreathStallTime => 1.0;

    // Effect is sent 1.3 seconds after BreathAngerSound and BreathAngerAnimation is played
    public virtual double BreathEffectDelay => 1.3;

    // Damage is given 1.0 seconds after effect is sent
    public virtual double BreathDamageDelay => 1.0;
    public virtual int BreathRange(BaseCreature source) => source.RangePerception;

    // Damage Types
    public virtual int ChaosDamage => 0;
    public virtual int PhysicalDamage => 0;
    public virtual int FireDamage => 100;
    public virtual int ColdDamage => 0;
    public virtual int PoisonDamage => 0;
    public virtual int EnergyDamage => 0;

    // Effect details and sound
    public virtual int BreathEffectItemID => 0x36D4;
    public virtual int BreathEffectSpeed => 5;
    public virtual int BreathEffectDuration => 0;
    public virtual bool BreathEffectExplodes => false;
    public virtual bool BreathEffectFixedDir => false;
    public virtual int BreathEffectHue => 0;
    public virtual int BreathEffectRenderMode => 0;

    public virtual int BreathEffectSound => 0x227;

    public override bool CanTrigger(BaseCreature source, MonsterAbilityTrigger trigger) => !source.Summoned && base.CanTrigger(source, trigger);

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        if (CanFireBreathTarget(source, target))
        {
            BreathStallMovement(source);
            BreathPlayAngerSound(source);
            BreathPlayAngerAnimation(source);

            source.Direction = source.GetDirectionTo(target);

            Timer.StartTimer(TimeSpan.FromSeconds(BreathEffectDelay), () => BreathEffect_Callback(source, target));

            base.Trigger(trigger, source, target);
        }
    }

    public virtual bool CanFireBreathTarget(BaseCreature source, Mobile target)
    {
        var range = BreathRange(source);

        return !source.IsDeadBondedPet && !source.BardPacified && target is { Alive: true, IsDeadBondedPet: false } &&
               target.Map == source.Map && source.CanBeHarmful(target) && target.InRange(source, range) &&
               source.InLOS(target);
    }

    // Anger sound/animations
    public virtual int BreathAngerSound(BaseCreature source) => source.GetAngerSound();
    public virtual int BreathAngerAnimation => 12;

    public virtual void BreathStallMovement(BaseCreature source)
    {
        if (source.AIObject != null)
        {
            source.AIObject.NextMove = Core.TickCount + (int)(BreathStallTime * 1000);
        }
    }

    public virtual void BreathPlayAngerSound(BaseCreature source)
    {
        var sound = BreathAngerSound(source);
        source.PlaySound(sound);
    }

    public virtual void BreathPlayAngerAnimation(BaseCreature source)
    {
        source.Animate(BreathAngerAnimation, 5, 1, true, false, 0);
    }

    public virtual void BreathEffect_Callback(BaseCreature source, Mobile target)
    {
        if (!target.Alive || !source.CanBeHarmful(target))
        {
            return;
        }

        BreathPlayEffectSound(source);
        BreathPlayEffect(source, target);

        Timer.StartTimer(TimeSpan.FromSeconds(BreathDamageDelay), () => BreathDamage_Callback(source, target));
    }

    public virtual void BreathPlayEffectSound(BaseCreature source)
    {
        source.PlaySound(BreathEffectSound);
    }

    public virtual void BreathPlayEffect(BaseCreature source, Mobile target)
    {
        Effects.SendMovingEffect(
            source,
            target,
            BreathEffectItemID,
            BreathEffectSpeed,
            BreathEffectDuration,
            BreathEffectFixedDir,
            BreathEffectExplodes,
            BreathEffectHue,
            BreathEffectRenderMode
        );
    }

    public virtual void BreathDamage_Callback(BaseCreature source, Mobile target)
    {
        if (target is BaseCreature creature && creature.BreathImmune)
        {
            return;
        }

        if (source.CanBeHarmful(target))
        {
            source.DoHarmful(target);
            BreathDealDamage(source, target);
        }
    }

    public virtual void BreathDealDamage(BaseCreature source, Mobile target)
    {
        if (!Evasion.CheckSpellEvasion(target))
        {
            var physDamage = PhysicalDamage;
            var fireDamage = FireDamage;
            var coldDamage = ColdDamage;
            var poisDamage = PoisonDamage;
            var nrgyDamage = EnergyDamage;

            if (ChaosDamage > 0)
            {
                switch (Utility.Random(5))
                {
                    case 0:
                        {
                            physDamage += ChaosDamage;
                            break;
                        }
                    case 1:
                        {
                            fireDamage += ChaosDamage;
                            break;
                        }
                    case 2:
                        {
                            coldDamage += ChaosDamage;
                            break;
                        }
                    case 3:
                        {
                            poisDamage += ChaosDamage;
                            break;
                        }
                    case 4:
                        {
                            nrgyDamage += ChaosDamage;
                            break;
                        }
                }
            }

            if (physDamage == 0 && fireDamage == 0 && coldDamage == 0 && poisDamage == 0 && nrgyDamage == 0)
            {
                target.Damage(BreathComputeDamage(source), source); // Unresistable damage even in AOS
            }
            else
            {
                AOS.Damage(
                    target,
                    source,
                    BreathComputeDamage(source),
                    physDamage,
                    fireDamage,
                    coldDamage,
                    poisDamage,
                    nrgyDamage
                );
            }
        }
    }

    public virtual int BreathComputeDamage(BaseCreature source)
    {
        var damage = (int)(source.Hits * BreathDamageScalar);

        if (source.IsParagon)
        {
            damage = (int)(damage / Paragon.HitsBuff);
        }

        return Math.Min(damage, 200);
    }
}
