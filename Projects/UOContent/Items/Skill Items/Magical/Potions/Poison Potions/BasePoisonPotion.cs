using Server.Engines.ConPVP;

namespace Server.Items
{
    public abstract class BasePoisonPotion : BasePotion
    {
        public BasePoisonPotion(PotionEffect effect) : base(0xF0A, effect)
        {
        }

        public BasePoisonPotion(Serial serial) : base(serial)
        {
        }

        public abstract Poison Poison { get; }

        public abstract double MinPoisoningSkill { get; }
        public abstract double MaxPoisoningSkill { get; }

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
}
