namespace Server.Items
{
    public class LesserPoisonPotion : BasePoisonPotion
    {
        [Constructible]
        public LesserPoisonPotion() : base(PotionEffect.PoisonLesser)
        {
        }

        public LesserPoisonPotion(Serial serial) : base(serial)
        {
        }

        public override Poison Poison => Poison.Lesser;

        public override double MinPoisoningSkill => 0.0;
        public override double MaxPoisoningSkill => 60.0;

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
