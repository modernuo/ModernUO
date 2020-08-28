namespace Server.Items
{
    public class RefreshPotion : BaseRefreshPotion
    {
        [Constructible]
        public RefreshPotion() : base(PotionEffect.Refresh)
        {
        }

        public RefreshPotion(Serial serial) : base(serial)
        {
        }

        public override double Refresh => 0.25;

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
