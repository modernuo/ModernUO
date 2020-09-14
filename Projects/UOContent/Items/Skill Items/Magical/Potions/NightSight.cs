using Server.Engines.ConPVP;

namespace Server.Items
{
    public class NightSightPotion : BasePotion
    {
        [Constructible]
        public NightSightPotion() : base(0xF06, PotionEffect.Nightsight)
        {
        }

        public NightSightPotion(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
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
}
