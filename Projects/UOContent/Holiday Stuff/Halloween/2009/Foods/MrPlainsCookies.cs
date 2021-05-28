namespace Server.Items
{
    public class MrPlainsCookies : Food
    {
        [Constructible]
        public MrPlainsCookies() : base(0x160C, 1)
        {
            Weight = 1.0;
            FillFactor = 4;
            Hue = 0xF4;
        }

        public MrPlainsCookies(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Mr Plain's Cookies";

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
