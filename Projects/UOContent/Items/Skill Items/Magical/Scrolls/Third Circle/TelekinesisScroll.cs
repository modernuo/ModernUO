namespace Server.Items
{
    [TypeAlias("Server.Items.TelekinisisScroll")]
    public class TelekinesisScroll : SpellScroll
    {
        [Constructible]
        public TelekinesisScroll(int amount = 1) : base(20, 0x1F41, amount)
        {
        }

        public TelekinesisScroll(Serial serial) : base(serial)
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
    }
}
