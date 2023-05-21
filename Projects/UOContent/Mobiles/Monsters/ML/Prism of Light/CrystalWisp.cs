using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class CrystalWisp : Wisp
    {
        [Constructible]
        public CrystalWisp()
        {
            Hue = 0x482;

            PackArcaneScroll(0, 1);
        }

        public override string DefaultName => "a crystal wisp";
    }
}
