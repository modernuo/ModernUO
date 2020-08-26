namespace Server.Items
{
    public class TotalRefreshPotion : BaseRefreshPotion
    {
        [Constructible]
        public TotalRefreshPotion() : base(PotionEffect.RefreshTotal)
        {
        }

        public TotalRefreshPotion(Serial serial) : base(serial)
        {
        }

        public override double Refresh => 1.0;

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
