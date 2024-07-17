using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseRefreshPotion : BasePotion
{
    public BaseRefreshPotion(PotionEffect effect) : base(0xF0B, effect)
    {
    }

    public abstract double Refresh { get; }

    public override bool CanDrink(Mobile from)
    {
        if (!base.CanDrink(from))
        {
            return false;
        }

        if (from.Stam >= from.StamMax)
        {
            from.SendMessage("You decide against drinking this potion, as you are already at full stamina.");
            return false;
        }

        return true;
    }

    public override void Drink(Mobile from)
    {
        from.Stam += Scale(from, (int)(Refresh * from.StamMax));

        PlayDrinkEffect(from);
    }
}
