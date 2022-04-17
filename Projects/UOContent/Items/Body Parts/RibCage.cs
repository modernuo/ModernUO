using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x1B17, 0x1B18)]
    public partial class RibCage : Item, IScissorable
    {
        [Constructible]
        public RibCage() : base(0x1B17 + Utility.Random(2))
        {
            Stackable = false;
            Weight = 5.0;
        }

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
            {
                return false;
            }

            ScissorHelper(from, new Bone(), Utility.RandomMinMax(3, 5));

            return true;
        }
    }
}
