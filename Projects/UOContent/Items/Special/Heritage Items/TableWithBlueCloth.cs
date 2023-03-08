using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class TableWithBlueClothAddon : BaseAddon
{
    [Constructible]
    public TableWithBlueClothAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x118C, 1076276), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new TableWithBlueClothDeed();
}

[SerializationGenerator(0)]
public partial class TableWithBlueClothDeed : BaseAddonDeed
{
    [Constructible]
    public TableWithBlueClothDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new TableWithBlueClothAddon();
    public override int LabelNumber => 1076276; // Table With A Blue Tablecloth
}
