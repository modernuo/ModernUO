using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NightSightPotion : BasePotion
{
    [Constructible]
    public NightSightPotion() : base(0xF06, PotionEffect.Nightsight)
    {
    }

    public override bool CanDrink(Mobile from)
    {
        if (!base.CanDrink(from))
        {
            return false;
        }

        if (!from.BeginAction<LightCycle>())
        {
            from.SendMessage("You already have night sight.");
            return false;
        }

        return true;
    }

    public override void Drink(Mobile from)
    {
        new LightCycle.NightSightTimer(from).Start();
        from.LightLevel = LightCycle.DungeonLevel / 2;

        from.FixedParticles(0x376A, 9, 32, 5007, EffectLayer.Waist);
        from.PlaySound(0x1E3);

        PlayDrinkEffect(from);
    }
}
