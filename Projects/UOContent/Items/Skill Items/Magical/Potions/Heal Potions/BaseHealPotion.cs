using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseHealPotion : BasePotion
{
    public BaseHealPotion(PotionEffect effect) : base(0xF0C, effect)
    {
    }

    public abstract int MinHeal { get; }
    public abstract int MaxHeal { get; }
    public abstract double Delay { get; }

    public void DoHeal(Mobile from)
    {
        var min = Scale(from, MinHeal);
        var max = Scale(from, MaxHeal);

        from.Heal(Utility.RandomMinMax(min, max));
    }

    public override bool CanDrink(Mobile from)
    {
        if (!base.CanDrink(from))
        {
            return false;
        }

        if (from.Hits >= from.HitsMax)
        {
            // You decide against drinking this potion, as you are already at full health.
            from.SendLocalizedMessage(1049547);
            return false;
        }

        if (from.Poisoned || MortalStrike.IsWounded(from))
        {
            // You can not heal yourself in your current state.
            from.LocalOverheadMessage(MessageType.Regular, 0x22, 1005000);
            return false;
        }

        if (!from.BeginAction<BaseHealPotion>())
        {
            // You must wait 10 seconds before using another healing potion.
            from.LocalOverheadMessage(MessageType.Regular, 0x22, 500235);
            return false;
        }

        return true;
    }

    public override void Drink(Mobile from)
    {
        DoHeal(from);

        PlayDrinkEffect(from);

        Timer.StartTimer(TimeSpan.FromSeconds(Delay), from.EndAction<BaseHealPotion>);
    }
}
