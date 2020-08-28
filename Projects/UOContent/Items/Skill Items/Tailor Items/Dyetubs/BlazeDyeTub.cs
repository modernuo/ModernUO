namespace Server.Items
{
    public class BlazeDyeTub : DyeTub
    {
        [Constructible]
        public BlazeDyeTub()
        {
            Hue = DyedHue = 0x489;
            Redyable = false;
        }

        public BlazeDyeTub(Serial serial) : base(serial)
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
