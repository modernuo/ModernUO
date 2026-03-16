using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public abstract partial class BaseThrown : BaseRanged
{
    public BaseThrown(int itemID) : base(itemID)
    {
    }

    public abstract int MinThrowRange { get; }

    public virtual int MaxThrowRange => MinThrowRange + 3;

    // Dynamic max range scaled by attacker Strength.
    // At StrReq the effective range equals MinThrowRange; at 140 Str it reaches MaxThrowRange.
    public override int DefMaxRange
    {
        get
        {
            var baseRange = MaxThrowRange;

            return Parent is Mobile attacker
                ? baseRange - 3 + (attacker.Str - AosStrengthReq) / ((140 - AosStrengthReq) / 3)
                : baseRange;
        }
    }

    public override int EffectID => ItemID;

    // Throwing weapons require no ammo — the weapon itself is the projectile.
    public override Type AmmoType => null;
    public override Item Ammo => null;

    public override int DefHitSound => 0x5D3;
    public override int DefMissSound => 0x5D4;

    public override SkillName DefSkill => SkillName.Throwing;
    public override SkillName AccuracySkill => SkillName.Throwing;

    public override WeaponAnimation DefAnimation => WeaponAnimation.Throwing;

    // Throwing-specific hit chance modifiers (applied via BaseWeapon.ModifyHitChance hook).
    protected override double ModifyHitChance(Mobile attacker, Mobile defender, double chance)
    {
        // Use Chebyshev distance — consistent with all UO range mechanics.
        var distance = Math.Max(
            Math.Abs(attacker.X - defender.X),
            Math.Abs(attacker.Y - defender.Y)
        );

        if (distance <= 1)
        {
            // Close-quarters penalty: up to -12%, mitigated by (Throwing + Dex) / 20.
            // At 240 combined (120 skill + 120 dex) the penalty is fully mitigated.
            var throwSkill = attacker.Skills[SkillName.Throwing].Value;
            var mitigation = Math.Min(12.0, (throwSkill + attacker.Dex) / 20.0);
            chance -= (12.0 - mitigation) / 100.0;
        }
        else if (distance < MinThrowRange)
        {
            // Below minimum throw range (but not melee): flat -12% hit chance.
            chance -= 0.12;
        }

        // Shield penalty: equipping a shield while throwing reduces hit chance.
        // Penalty = 1200 / Parry (capped at 90%), applied multiplicatively.
        // High Parrying skill reduces the penalty significantly.
        if (attacker.FindItemOnLayer<BaseShield>(Layer.TwoHanded) != null)
        {
            var parry = attacker.Skills[SkillName.Parry].Value;
            var penalty = parry > 0.0 ? 1200.0 / parry : 90.0;
            chance *= 1.0 - Math.Min(90.0, penalty) / 100.0;
        }

        return chance;
    }

    // Overthrow penalty: -47% damage when target is beyond the attacker's current max range.
    // MaxRange returns DefMaxRange (STR-scaled), so this reflects the dynamic per-attack value.
    public override int ComputeDamage(Mobile attacker, Mobile defender)
    {
        var damage = base.ComputeDamage(attacker, defender);

        if (!attacker.InRange(defender.Location, MaxRange))
        {
            damage = (int)(damage * 0.53);
        }

        return damage;
    }

    public override bool OnFired(Mobile attacker, Mobile defender)
    {
        if (!attacker.InRange(defender, 1))
        {
            attacker.MovingEffect(defender, EffectID, 18, 1, false, false, Hue, 0);
        }

        return true;
    }

    public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
    {
        if (WeaponAbility.GetCurrentAbility(attacker) is not MysticArc)
        {
            var location = new WorldLocation(defender.Location, attacker.Map);
            Timer.StartTimer(TimeSpan.FromSeconds(0.3), () => Return(attacker, defender, location));
        }

        base.OnHit(attacker, defender, damageBonus);
    }

    public override void OnMiss(Mobile attacker, Mobile defender)
    {
        if (WeaponAbility.GetCurrentAbility(attacker) is not MysticArc)
        {
            var location = new WorldLocation(defender.Location, attacker.Map);
            Timer.StartTimer(TimeSpan.FromSeconds(0.3), () => Return(attacker, defender, location));
        }

        base.OnMiss(attacker, defender);
    }

    public virtual void Return(Mobile thrower, Mobile target, WorldLocation worldLocation)
    {
        if (thrower == null)
        {
            return;
        }

        if (target != null)
        {
            target.MovingEffect(thrower, EffectID, 18, 1, false, false, Hue, 0);
        }
        else
        {
            Effects.SendMovingParticles(
                new Entity(Serial.Zero, worldLocation.Location, worldLocation.Map),
                thrower,
                ItemID,
                18,
                0,
                false,
                false,
                Hue,
                0,
                9502,
                1,
                0,
                (EffectLayer)255,
                0x100
            );
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1149791, MinThrowRange); // Min Throw Range: ~1_val~
    }
}
