using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecorativeTopiary : Item
{
    [Constructible]
    public DecorativeTopiary() : base(0x2378) => LootType = LootType.Blessed;

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
