using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BackpackArtifact : BaseDecorationContainerArtifact
{
    [Constructible]
    public BackpackArtifact() : base(0x9B2)
    {
    }

    public override int ArtifactRarity => 5;
}

[SerializationGenerator(0)]
public partial class BloodyWaterArtifact : BaseDecorationArtifact
{
    [Constructible]
    public BloodyWaterArtifact() : base(0xE23)
    {
    }

    public override int ArtifactRarity => 5;
}

[SerializationGenerator(0)]
public partial class BooksWestArtifact : BaseDecorationArtifact
{
    [Constructible]
    public BooksWestArtifact() : base(0x1E25)
    {
    }

    public override int ArtifactRarity => 3;
}

[SerializationGenerator(0)]
public partial class BooksNorthArtifact : BaseDecorationArtifact
{
    [Constructible]
    public BooksNorthArtifact() : base(0x1E24)
    {
    }

    public override int ArtifactRarity => 3;
}

[SerializationGenerator(0)]
public partial class BooksFaceDownArtifact : BaseDecorationArtifact
{
    [Constructible]
    public BooksFaceDownArtifact() : base(0x1E21)
    {
    }

    public override int ArtifactRarity => 3;
}

[SerializationGenerator(0)]
public partial class BottleArtifact : BaseDecorationArtifact
{
    [Constructible]
    public BottleArtifact() : base(0xE28)
    {
    }

    public override int ArtifactRarity => 1;
}

[SerializationGenerator(0)]
public partial class BrazierArtifact : BaseDecorationArtifact
{
    [Constructible]
    public BrazierArtifact() : base(0xE31) => Light = LightType.Circle150;

    public override int ArtifactRarity => 2;
}

[SerializationGenerator(0)]
public partial class CocoonArtifact : BaseDecorationArtifact
{
    [Constructible]
    public CocoonArtifact() : base(0x10DA)
    {
    }

    public override int ArtifactRarity => 7;
}

[SerializationGenerator(0)]
public partial class DamagedBooksArtifact : BaseDecorationArtifact
{
    [Constructible]
    public DamagedBooksArtifact() : base(0xC16)
    {
    }

    public override int ArtifactRarity => 1;
}

[SerializationGenerator(0)]
public partial class EggCaseArtifact : BaseDecorationArtifact
{
    [Constructible]
    public EggCaseArtifact() : base(0x10D9)
    {
    }

    public override int ArtifactRarity => 5;
}

[SerializationGenerator(0)]
public partial class GruesomeStandardArtifact : BaseDecorationArtifact
{
    [Constructible]
    public GruesomeStandardArtifact() : base(0x428)
    {
    }

    public override int ArtifactRarity => 5;
}

[SerializationGenerator(0)]
public partial class LampPostArtifact : BaseDecorationArtifact
{
    [Constructible]
    public LampPostArtifact() : base(0xB24) => Light = LightType.Circle300;

    public override int ArtifactRarity => 3;
}

[SerializationGenerator(0)]
public partial class LeatherTunicArtifact : BaseDecorationArtifact
{
    [Constructible]
    public LeatherTunicArtifact() : base(0x13CA)
    {
    }

    public override int ArtifactRarity => 9;
}

[SerializationGenerator(0)]
public partial class RockArtifact : BaseDecorationArtifact
{
    [Constructible]
    public RockArtifact() : base(0x1363)
    {
    }

    public override int ArtifactRarity => 1;
}

[SerializationGenerator(0)]
public partial class RuinedPaintingArtifact : BaseDecorationArtifact
{
    [Constructible]
    public RuinedPaintingArtifact() : base(0xC2C)
    {
    }

    public override int ArtifactRarity => 12;
}

[SerializationGenerator(0)]
public partial class SaddleArtifact : BaseDecorationArtifact
{
    [Constructible]
    public SaddleArtifact() : base(0xF38)
    {
    }

    public override int ArtifactRarity => 9;
}

[SerializationGenerator(0)]
public partial class SkinnedDeerArtifact : BaseDecorationArtifact
{
    [Constructible]
    public SkinnedDeerArtifact() : base(0x1E91)
    {
    }

    public override int ArtifactRarity => 8;
}

[SerializationGenerator(0)]
public partial class SkinnedGoatArtifact : BaseDecorationArtifact
{
    [Constructible]
    public SkinnedGoatArtifact() : base(0x1E88)
    {
    }

    public override int ArtifactRarity => 5;
}

[SerializationGenerator(0)]
public partial class SkullCandleArtifact : BaseDecorationArtifact
{
    [Constructible]
    public SkullCandleArtifact() : base(0x1858) => Light = LightType.Circle150;

    public override int ArtifactRarity => 1;
}

[SerializationGenerator(0)]
public partial class StretchedHideArtifact : BaseDecorationArtifact
{
    [Constructible]
    public StretchedHideArtifact() : base(0x106B)
    {
    }

    public override int ArtifactRarity => 2;
}

[SerializationGenerator(0)]
public partial class StuddedLeggingsArtifact : BaseDecorationArtifact
{
    [Constructible]
    public StuddedLeggingsArtifact() : base(0x13D8)
    {
    }

    public override int ArtifactRarity => 5;
}

[SerializationGenerator(0)]
public partial class StuddedTunicArtifact : BaseDecorationArtifact
{
    [Constructible]
    public StuddedTunicArtifact() : base(0x13D9)
    {
    }

    public override int ArtifactRarity => 7;
}

[SerializationGenerator(0)]
public partial class TarotCardsArtifact : BaseDecorationArtifact
{
    [Constructible]
    public TarotCardsArtifact() : base(0x12A5)
    {
    }

    public override int ArtifactRarity => 5;
}
