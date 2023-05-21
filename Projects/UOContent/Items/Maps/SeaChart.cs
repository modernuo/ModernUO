using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SeaChart : MapItem
{
    [Constructible]
    public SeaChart()
    {
        SetDisplay(0, 0, 5119, 4095, 400, 400);
    }

    public override int LabelNumber => 1015232; // sea chart

    public override void CraftInit(Mobile from)
    {
        var skillValue = from.Skills.Cartography.Value;
        var dist = Math.Max(64 + (int)(skillValue * 10), 200);
        var size = Math.Clamp(24 + (int)(skillValue * 3.3), 200, 400);

        SetDisplay(from.X - dist, from.Y - dist, from.X + dist, from.Y + dist, size, size);
    }
}
