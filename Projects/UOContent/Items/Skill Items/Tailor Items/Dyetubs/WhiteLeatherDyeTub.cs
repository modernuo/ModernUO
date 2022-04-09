using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class WhiteLeatherDyeTub : LeatherDyeTub /* OSI UO 13th anniv gift, from redeemable gift tickets */
    {
        [Constructible]
        public WhiteLeatherDyeTub()
        {
            DyedHue = Hue = 0x9C2;
            LootType = LootType.Blessed;
        }

        public override int LabelNumber => 1149900; // White Leather Dye Tub

        public override bool Redyable => false;
    }
}
