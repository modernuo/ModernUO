using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xec6, 0xec7)]
[SerializationGenerator(0, false)]
public partial class Dressform : Item
{
    [Constructible]
    public Dressform() : base(0xec6) => Weight = 10;

    public Dressform(int itemId) : base(itemId) => Weight = 10;
}

[Flippable(0xec6, 0xec7)]
[SerializationGenerator(0, false)]
public partial class DressformSide : Dressform
{
    [Constructible]
    public DressformSide() : base(0xec7) => Weight = 10;
}
