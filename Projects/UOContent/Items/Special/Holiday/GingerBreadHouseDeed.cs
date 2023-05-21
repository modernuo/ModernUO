using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GingerBreadHouseAddon : BaseAddon
{
    public GingerBreadHouseAddon()
    {
        for (var i = 0x2be5; i < 0x2be8; i++)
        {
            var laoc = new LocalizedAddonComponent(i, 1077395); // Gingerbread House
            laoc.Light = LightType.SouthSmall;
            AddComponent(laoc, i == 0x2be5 ? -1 : 0, i == 0x2be7 ? -1 : 0, 0);
        }
    }

    public override BaseAddonDeed Deed => new GingerBreadHouseDeed();
}

[SerializationGenerator(0, false)]
public partial class GingerBreadHouseDeed : BaseAddonDeed
{
    [Constructible]
    public GingerBreadHouseDeed()
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1077394; // a Gingerbread House Deed
    public override BaseAddon Addon => new GingerBreadHouseAddon();
}
