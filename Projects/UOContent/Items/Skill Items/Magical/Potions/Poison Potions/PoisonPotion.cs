namespace Server.Items
{
    public class PoisonPotion : BasePoisonPotion
    {
        [Constructible]
        public PoisonPotion() : base(PotionEffect.Poison)
        {
        }

        public PoisonPotion(Serial serial) : base(serial)
        {
        }

        public override Poison Poison => Poison.Regular;

        public override double MinPoisoningSkill => 30.0;
        public override double MaxPoisoningSkill => 70.0;

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
    }
}
