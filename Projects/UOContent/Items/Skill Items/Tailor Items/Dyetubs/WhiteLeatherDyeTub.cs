namespace Server.Items
{
    public class WhiteLeatherDyeTub : LeatherDyeTub /* OSI UO 13th anniv gift, from redeemable gift tickets */
    {
        [Constructible]
        public WhiteLeatherDyeTub()
        {
            DyedHue = Hue = 0x9C2;
            LootType = LootType.Blessed;
        }

        public WhiteLeatherDyeTub(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1149900; // White Leather Dye Tub

        public override bool Redyable => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
