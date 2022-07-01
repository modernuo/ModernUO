using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CocoaLiquor : Item
{
    [Constructible]
    public CocoaLiquor() : base(0x103F) => Hue = 0x46A;

    public override int LabelNumber => 1080007; // Cocoa liquor
    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class SackOfSugar : Item
{
    [Constructible]
    public SackOfSugar(int amount = 1) : base(0x1039)
    {
        Hue = 0x461;
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1080003; // Sack of sugar
    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class CocoaButter : Item
{
    [Constructible]
    public CocoaButter() : base(0x1044) => Hue = 0x457;

    public override int LabelNumber => 1080005; // Cocoa butter
    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Vanilla : Item
{
    [Constructible]
    public Vanilla(int amount = 1) : base(0xE2A)
    {
        Hue = 0x462;
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1080009; // Vanilla
    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class CocoaPulp : Item
{
    [Constructible]
    public CocoaPulp(int amount = 1) : base(0xF7C)
    {
        Hue = 0x219;
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1080530; // cocoa pulp
    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class DarkChocolate : CandyCane
{
    [Constructible]
    public DarkChocolate() : base(0xF10)
    {
        Hue = 0x465;
        LootType = LootType.Regular;
    }

    public override int LabelNumber => 1079994; // Dark chocolate
    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class MilkChocolate : CandyCane
{
    [Constructible]
    public MilkChocolate() : base(0xF18)
    {
        Hue = 0x461;
        LootType = LootType.Regular;
    }

    public override int LabelNumber => 1079995; // Milk chocolate
    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class WhiteChocolate : CandyCane
{
    [Constructible]
    public WhiteChocolate() : base(0xF11)
    {
        Hue = 0x47E;
        LootType = LootType.Regular;
    }

    public override int LabelNumber => 1079996; // White chocolate
    public override double DefaultWeight => 1.0;
}
