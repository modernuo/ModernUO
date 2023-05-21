using ModernUO.Serialization;
using Server.Engines.ConPVP;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseRefreshPotion : BasePotion
{
    public BaseRefreshPotion(PotionEffect effect) : base(0xF0B, effect)
    {
    }

    public abstract double Refresh { get; }

    public override void Drink(Mobile from)
    {
        if (from.Stam < from.StamMax)
        {
            from.Stam += Scale(from, (int)(Refresh * from.StamMax));

            PlayDrinkEffect(from);

            if (!DuelContext.IsFreeConsume(from))
            {
                Consume();
            }
        }
        else
        {
            from.SendMessage("You decide against drinking this potion, as you are already at full stamina.");
        }
    }
}
