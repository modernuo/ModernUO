using System;
using ModernUO.Serialization;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseStrengthPotion : BasePotion
{
    public BaseStrengthPotion(PotionEffect effect) : base(0xF09, effect)
    {
    }

    public abstract int StrOffset { get; }
    public abstract TimeSpan Duration { get; }

    public override bool CanDrink(Mobile from)
    {
        if (!base.CanDrink(from))
        {
            return false;
        }

        int scale = Scale(from, StrOffset);
        // TODO: Verify scaled; is it offset, duration, or both?
        if (!SpellHelper.AddStatOffset(from, StatType.Str, scale, Duration))
        {
            from.SendLocalizedMessage(502173); // You are already under a similar effect.
            return false;
        }

        BuffInfo.AddBuff(from, new BuffInfo(BuffIcon.Strength, 1075845, Duration, from, scale.ToString()));

        from.FixedEffect(0x375A, 10, 15);
        from.PlaySound(0x1E7);
        return true;
    }

    public override void Drink(Mobile from)
    {
        PlayDrinkEffect(from);
    }
}
