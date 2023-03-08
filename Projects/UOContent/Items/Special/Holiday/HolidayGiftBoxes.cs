using ModernUO.Serialization;

namespace Server.Items;

public static class GiftBoxHues
{
    /* there's possibly a couple more, but this is what we could verify on OSI */

    private static readonly int[] _normalHues =
    {
        0x672, 0x454, 0x507, 0x4ac,
        0x504, 0x84b, 0x495, 0x97c,
        0x493, 0x4a8, 0x494, 0x4aa,
        0xb8b, 0x84f, 0x491, 0x851,
        0x503, 0xb8c, 0x4ab, 0x84B
    };

    private static readonly int[] _neonHues =
    {
        0x438, 0x424, 0x433,
        0x445, 0x42b, 0x448
    };

    public static int RandomGiftBoxHue => _normalHues.RandomElement();
    public static int RandomNeonBoxHue => _neonHues.RandomElement();
}

[Flippable(0x46A5, 0x46A6)]
[SerializationGenerator(0, false)]
public partial class GiftBoxRectangle : BaseContainer
{
    [Constructible]
    public GiftBoxRectangle() : base(Utility.RandomBool() ? 0x46A5 : 0x46A6) => Hue = GiftBoxHues.RandomGiftBoxHue;

    public override int DefaultGumpID => 0x11E;
}

[SerializationGenerator(0, false)]
public partial class GiftBoxCube : BaseContainer
{
    [Constructible]
    public GiftBoxCube() : base(0x46A2) => Hue = GiftBoxHues.RandomGiftBoxHue;

    public override int DefaultGumpID => 0x11B;
}

[SerializationGenerator(0, false)]
public partial class GiftBoxCylinder : BaseContainer
{
    [Constructible]
    public GiftBoxCylinder() : base(0x46A3) => Hue = GiftBoxHues.RandomGiftBoxHue;

    public override int DefaultGumpID => 0x11C;
}

[SerializationGenerator(0, false)]
public partial class GiftBoxOctogon : BaseContainer
{
    [Constructible]
    public GiftBoxOctogon() : base(0x46A4) => Hue = GiftBoxHues.RandomGiftBoxHue;

    public override int DefaultGumpID => 0x11D;
}

[SerializationGenerator(0, false)]
public partial class GiftBoxAngel : BaseContainer
{
    [Constructible]
    public GiftBoxAngel() : base(0x46A7) => Hue = GiftBoxHues.RandomGiftBoxHue;

    public override int DefaultGumpID => 0x11F;
}

[Flippable(0x232A, 0x232B)]
[SerializationGenerator(0, false)]
public partial class GiftBoxNeon : BaseContainer
{
    [Constructible]
    public GiftBoxNeon() : base(Utility.RandomBool() ? 0x232A : 0x232B) => Hue = GiftBoxHues.RandomNeonBoxHue;
}
