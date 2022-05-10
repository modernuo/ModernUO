using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Fish : Item, ICarvable
    {
        [Constructible]
        public Fish(int amount = 1) : base(Utility.Random(0x09CC, 4))
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public void Carve(Mobile from, Item item)
        {
            ScissorHelper(from, new RawFishSteak(), 4);
        }
    }
}
