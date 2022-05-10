using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x1B09, 0x1B10)]
    public partial class BonePile : Item, IScissorable
    {
        [Constructible]
        public BonePile() : base(0x1B09 + Utility.Random(8))
        {
            Stackable = false;
            Weight = 10.0;
        }

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
            {
                return false;
            }

            ScissorHelper(from, new Bone(), Utility.RandomMinMax(10, 15));

            return true;
        }
    }
}
