using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BlackDyeTub : DyeTub
    {
        [Constructible]
        public BlackDyeTub()
        {
            Hue = DyedHue = 0x0001;
            Redyable = false;
        }
    }
}
