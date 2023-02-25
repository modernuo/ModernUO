using ModernUO.Serialization;
using Server.Engines.ConPVP;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NightSightPotion : BasePotion
{
    [Constructible]
    public NightSightPotion() : base(0xF06, PotionEffect.Nightsight)
    {
    }

    public override void Drink(Mobile from)
    {
        if (from.BeginAction<LightCycle>())
        {
            new LightCycle.NightSightTimer(from).Start();
            from.LightLevel = LightCycle.DungeonLevel / 2;

            from.FixedParticles(0x376A, 9, 32, 5007, EffectLayer.Waist);
            from.PlaySound(0x1E3);

            PlayDrinkEffect(from);

            if (!DuelContext.IsFreeConsume(from))
            {
                Consume();
            }
        }
        else
        {
            from.SendMessage("You already have nightsight.");
        }
    }
}
