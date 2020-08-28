namespace Server.Items
{
    public class AdminRobe : BaseSuit
    {
        [Constructible]
        public AdminRobe() : base(AccessLevel.Administrator, 0x0, 0x204F) // Blank hue
        {
        }

        public AdminRobe(Serial serial) : base(serial)
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
