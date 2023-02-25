using System;
using ModernUO.Serialization;
using Server.Engines.ConPVP;
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

    public bool DoStrength(Mobile from)
    {
        // TODO: Verify scaled; is it offset, duration, or both?
        if (SpellHelper.AddStatOffset(from, StatType.Str, Scale(from, StrOffset), Duration))
        {
            from.FixedEffect(0x375A, 10, 15);
            from.PlaySound(0x1E7);
            return true;
        }

        from.SendLocalizedMessage(502173); // You are already under a similar effect.
        return false;
    }

    public override void Drink(Mobile from)
    {
        if (DoStrength(from))
        {
            PlayDrinkEffect(from);

            if (!DuelContext.IsFreeConsume(from))
            {
                Consume();
            }
        }
    }
}
