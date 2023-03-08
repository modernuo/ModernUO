using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class TableWithPurpleClothAddon : BaseAddon
{
    [Constructible]
    public TableWithPurpleClothAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x118B, 1076275), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new TableWithPurpleClothDeed();
}

[SerializationGenerator(0)]
public partial class TableWithPurpleClothDeed : BaseAddonDeed
{
    [Constructible]
    public TableWithPurpleClothDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new TableWithPurpleClothAddon();
    public override int LabelNumber => 1076275; // Table With A Purple Tablecloth
}
