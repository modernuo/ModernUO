using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ParrotWafer : Item
    {
        [Constructible]
        public ParrotWafer()
            : base(0x2FD6)
        {
            Hue = 0x38;
            Stackable = true;
        }

        public ParrotWafer(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1072904;// Parrot Wafers
   }
}
