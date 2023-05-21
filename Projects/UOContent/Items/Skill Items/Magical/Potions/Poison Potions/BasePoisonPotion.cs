using ModernUO.Serialization;
using Server.Engines.ConPVP;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BasePoisonPotion : BasePotion
{
    public BasePoisonPotion(PotionEffect effect) : base(0xF0A, effect)
    {
    }

    public abstract Poison Poison { get; }

    public abstract double MinPoisoningSkill { get; }
    public abstract double MaxPoisoningSkill { get; }

    public void DoPoison(Mobile from)
    {
        from.ApplyPoison(from, Poison);
    }

    public override void Drink(Mobile from)
    {
        DoPoison(from);

        PlayDrinkEffect(from);

        if (!DuelContext.IsFreeConsume(from))
        {
            Consume();
        }
    }
}
