using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Fish : Item, ICarvable
{
    [Constructible]
    public Fish(int amount = 1) : base(Utility.Random(0x09CC, 4))
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 1.0;

    public void Carve(Mobile from, Item item)
    {
        ScissorHelper(from, new RawFishSteak(), 4);
    }
}
