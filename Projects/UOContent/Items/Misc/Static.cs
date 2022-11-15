using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Static : Item
{
    public Static() : base(0x80) => Movable = false;

    [Constructible]
    public Static(int itemID) : base(itemID) => Movable = false;

    [Constructible]
    public Static(int itemID, int count) : this(Utility.Random(itemID, count))
    {
    }
}

[SerializationGenerator(0)]
public partial class LocalizedStatic : Static
{
    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _number;

    [Constructible]
    public LocalizedStatic(int itemID) : this(itemID, itemID < 0x4000 ? 1020000 + itemID : 1078872 + itemID)
    {
    }

    [Constructible]
    public LocalizedStatic(int itemID, int number) : base(itemID) => _number = number;

    public override int LabelNumber => _number;
}
