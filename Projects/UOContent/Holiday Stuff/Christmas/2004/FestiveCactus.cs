using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FestiveCactus : Item
{
    [Constructible]
    public FestiveCactus() : base(0x2376) => LootType = LootType.Blessed;

    public override double DefaultWeight => 1.0;

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        LabelTo(from, 1070880); // Winter 2004
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1070880); // Winter 2004
    }
}
