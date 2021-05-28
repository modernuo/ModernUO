namespace Server.Items
{
    public class JellyBeans : CandyCane
    {
        [Constructible]
        public JellyBeans(int amount = 1)
            : base(0x468C) =>
            Stackable = true;

        public JellyBeans(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1096932; /* jellybeans */

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
