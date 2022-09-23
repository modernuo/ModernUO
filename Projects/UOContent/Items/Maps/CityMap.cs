using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CityMap : MapItem
{
    [Constructible]
    public CityMap()
    {
        SetDisplay(0, 0, 5119, 4095, 400, 400);
    }

    public override int LabelNumber => 1015231; // city map

    public override void CraftInit(Mobile from)
    {
        var skillValue = from.Skills.Cartography.Value;
        var dist = Math.Max(64 + (int)(skillValue * 4), 200);
        var size = Math.Clamp(32 + (int)(skillValue * 2), 200, 400);

        SetDisplay(from.X - dist, from.Y - dist, from.X + dist, from.Y + dist, size, size);
    }
}
