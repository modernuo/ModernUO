using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class OrigamiPaper : Item
{
    [Constructible]
    public OrigamiPaper() : base(0x2830)
    {
    }

    public override int LabelNumber => 1030288; // origami paper

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        Delete();

        Item i = Utility.Random(from.BAC >= 5 ? 6 : 5) switch
        {
            0 => new OrigamiButterfly(),
            1 => new OrigamiSwan(),
            2 => new OrigamiFrog(),
            3 => new OrigamiShape(),
            4 => new OrigamiSongbird(),
            5 => new OrigamiFish(),
            _ => null
        };

        if (i != null)
        {
            from.AddToBackpack(i);
        }

        from.SendLocalizedMessage(1070822); // You fold the paper into an interesting shape.
    }
}

[SerializationGenerator(0)]
public partial class OrigamiButterfly : Item
{
    [Constructible]
    public OrigamiButterfly() : base(0x2838) => LootType = LootType.Blessed;

    public override int LabelNumber => 1030296; // a delicate origami butterfly
}

[SerializationGenerator(0)]
public partial class OrigamiSwan : Item
{
    [Constructible]
    public OrigamiSwan() : base(0x2839) => LootType = LootType.Blessed;

    public override int LabelNumber => 1030297; // a delicate origami swan
}

[SerializationGenerator(0)]
public partial class OrigamiFrog : Item
{
    [Constructible]
    public OrigamiFrog() : base(0x283A) => LootType = LootType.Blessed;

    public override int LabelNumber => 1030298; // a delicate origami frog
}

[SerializationGenerator(0)]
public partial class OrigamiShape : Item
{
    [Constructible]
    public OrigamiShape() : base(0x283B) => LootType = LootType.Blessed;

    public override int LabelNumber => 1030299; // an intricate geometric origami shape
}

[SerializationGenerator(0)]
public partial class OrigamiSongbird : Item
{
    [Constructible]
    public OrigamiSongbird() : base(0x283C) => LootType = LootType.Blessed;

    public override int LabelNumber => 1030300; // a delicate origami songbird
}

[SerializationGenerator(0)]
public partial class OrigamiFish : Item
{
    [Constructible]
    public OrigamiFish() : base(0x283D) => LootType = LootType.Blessed;

    public override int LabelNumber => 1030301; // a delicate origami fish
}
