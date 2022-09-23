using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class WorldMap : MapItem
{
    [Constructible]
    public WorldMap()
    {
        SetDisplay(0, 0, 5119, 4095, 400, 400);
    }

    public override int LabelNumber => 1015233; // world map

    public override void CraftInit(Mobile from)
    {
        // Unlike the others, world map is not based on crafted location

        var skillValue = from.Skills.Cartography.Value;
        var x20 = (int)(skillValue * 20);
        var size = Math.Clamp(25 + (int)(skillValue * 6.6), 200, 400);

        SetDisplay(1344 - x20, 1600 - x20, 1472 + x20, 1728 + x20, size, size);
    }
}
