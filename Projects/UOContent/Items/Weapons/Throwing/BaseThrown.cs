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

    public override int DefMaxRange
    {
        get
        {
            int baseRange = MaxThrowRange;

            return Parent is Mobile attacker
                ? baseRange - 3 + (attacker.Str - AosStrengthReq) / ((140 - AosStrengthReq) / 3)
                : baseRange;
        }
    }

    public override int EffectID => ItemID;

    public override Type AmmoType => null;

    public override Item Ammo => null;

    public override int DefHitSound => 0x5D3;
    public override int DefMissSound => 0x5D4;

    public override SkillName DefSkill => SkillName.Throwing;

    public override WeaponAnimation DefAnimation => WeaponAnimation.Throwing;

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
}
