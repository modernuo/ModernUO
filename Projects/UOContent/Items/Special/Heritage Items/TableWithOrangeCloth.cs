using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class TableWithOrangeClothAddon : BaseAddon
{
    [Constructible]
    public TableWithOrangeClothAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x118E, 1076278), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new TableWithOrangeClothDeed();
}

[SerializationGenerator(0)]
public partial class TableWithOrangeClothDeed : BaseAddonDeed
{
    [Constructible]
    public TableWithOrangeClothDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new TableWithOrangeClothAddon();
    public override int LabelNumber => 1076278; // Table With An Orange Tablecloth
}
