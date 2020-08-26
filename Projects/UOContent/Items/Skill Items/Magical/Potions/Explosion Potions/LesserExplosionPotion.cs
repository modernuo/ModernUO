namespace Server.Items
{
    public class LesserExplosionPotion : BaseExplosionPotion
    {
        [Constructible]
        public LesserExplosionPotion() : base(PotionEffect.ExplosionLesser)
        {
        }

        public LesserExplosionPotion(Serial serial) : base(serial)
        {
        }

        public override int MinDamage => 5;
        public override int MaxDamage => 10;

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
