using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HolidayTimepiece : Clock
{
    [Constructible]
    public HolidayTimepiece() : base(0x1086)
    {
        Weight = DefaultWeight;
        LootType = LootType.Blessed;
        Layer = Layer.Bracelet;
    }

    public override int LabelNumber => 1041113; // a holiday timepiece
    public override double DefaultWeight => 1.0;
}
