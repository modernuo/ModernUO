using System;
using ModernUO.Serialization;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseAgilityPotion : BasePotion
{
    public BaseAgilityPotion(PotionEffect effect) : base(0xF08, effect)
    {
    }

    public abstract int DexOffset { get; }
    public abstract TimeSpan Duration { get; }

    public override bool CanDrink(Mobile from)
    {
        if (!base.CanDrink(from))
        {
            return false;
        }

        // TODO: Verify scaled; is it offset, duration, or both?
        var scale = Scale( from, DexOffset );
        if (!SpellHelper.AddStatOffset(from, StatType.Dex, scale, Duration))
        {
            from.SendLocalizedMessage(502173); // You are already under a similar effect.
            return false;
        }

        from.FixedEffect(0x375A, 10, 15);
        from.PlaySound(0x1E7);

        BuffInfo.AddBuff(from, new BuffInfo(BuffIcon.Agility, 1075841, Duration, from, scale.ToString()));

        return true;
    }

    public override void Drink(Mobile from)
    {
        PlayDrinkEffect(from);
    }
}
