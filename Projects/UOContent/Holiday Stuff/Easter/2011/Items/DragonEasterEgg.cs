using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class DragonEasterEgg : Item, IDyable
    {
        [Constructible]
        public DragonEasterEgg()
            : base(0x47E6)
        {
        }

        public override int LabelNumber => 1097278;

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted || !sender.AllowDyables)
            {
                return false;
            }

            Hue = sender.DyedHue;

            return true;
        }
    }
}
