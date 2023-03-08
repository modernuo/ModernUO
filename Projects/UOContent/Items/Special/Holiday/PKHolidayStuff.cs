using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Coal : Item
{
    [Constructible]
    public Coal() : base(0x19b9)
    {
        Stackable = false;
        LootType = LootType.Blessed;
        Hue = 0x965;
    }

    public override int LabelNumber => 1041426;
}

[SerializationGenerator(0, false)]
public partial class BadCard : Item
{
    private static readonly int[] _cardHues = { 0x45, 0x27, 0x3d0 };

    [Constructible]
    public BadCard() : base(0x14ef)
    {
        Hue = _cardHues.RandomElement();
        Stackable = false;
        LootType = LootType.Blessed;
        Movable = true;
    }

    public override int LabelNumber => 1041428; // Maybe next year you will get a nicer gift.
}

[SerializationGenerator(0, false)]
public partial class Spam : Food
{
    [Constructible]
    public Spam() : base(0x1044)
    {
        Stackable = false;
        LootType = LootType.Blessed;
    }
}
