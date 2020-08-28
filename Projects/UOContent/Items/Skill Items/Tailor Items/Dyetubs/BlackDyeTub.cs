namespace Server.Items
{
    public class BlackDyeTub : DyeTub
    {
        [Constructible]
        public BlackDyeTub()
        {
            Hue = DyedHue = 0x0001;
            Redyable = false;
        }

        public BlackDyeTub(Serial serial) : base(serial)
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
