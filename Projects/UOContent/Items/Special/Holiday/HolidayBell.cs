using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HolidayBell : Item
{
    private static readonly int[] _hues =
    {
        0xA, 0x24, 0x42, 0x56, 0x1A, 0x4C, 0x3C, 0x60, 0x2E, 0x55, 0x23, 0x38, 0x482, 0x6, 0x10
    };

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TextDefinition _maker;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _soundID;

    [Constructible]
    public HolidayBell() : this(1041441 + Utility.Random(22))
    {
    }

    [Constructible]
    public HolidayBell(TextDefinition maker) : base(0x1C12)
    {
        _maker = maker;

        LootType = LootType.Blessed;
        Hue = _hues.RandomElement();
        _soundID = 0x0F5 + Utility.Random(14);
    }

    public override int LabelNumber => _maker?.Number > 0 ? _maker.Number : base.LabelNumber;

    public override string DefaultName => _maker?.String != null
        ? $"a holiday bell from {_maker.String}"
        : "a holiday bell";

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }
        else
        {
            from.PlaySound(_soundID);
        }
    }
}
