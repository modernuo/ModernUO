using ModernUO.Serialization;

namespace Server.Items /* High seas, loot from merchant ship's hold, also a "uncommon" loot item */
{
    [SerializationGenerator(0, false)]
    public partial class WhiteClothDyeTub : DyeTub
    {
        [Constructible]
        public WhiteClothDyeTub() => DyedHue = Hue = 0x9C2;

        public override int LabelNumber => 1149984; // White Cloth Dye Tub

        public override bool Redyable => false;
    }
}
