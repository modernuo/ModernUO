using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class TableWithRedClothAddon : BaseAddon
{
    [Constructible]
    public TableWithRedClothAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x118D, 1076277), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new TableWithRedClothDeed();
}

[SerializationGenerator(0)]
public partial class TableWithRedClothDeed : BaseAddonDeed
{
    [Constructible]
    public TableWithRedClothDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new TableWithRedClothAddon();
    public override int LabelNumber => 1076277; // Table With A Red Tablecloth
}
