using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.ConPVP;

public enum DuelTeleporterType
{
    Squares = 6095,
    Buds = 6104,
    Flowers = 6113,
    Spikes = 6122,
    Arrows = 6140,
    Links = 6149
}

[SerializationGenerator(0, false)]
public partial class DuelTeleporterAddon : BaseAddon
{
    [Constructible]
    public DuelTeleporterAddon(DuelTeleporterType type = DuelTeleporterType.Squares)
    {
        var itemID = (int)type;

        AddComponent(new AddonComponent(itemID + 0), -1, -1, 5);
        AddComponent(new AddonComponent(itemID + 1), -1, 0, 5);
        AddComponent(new AddonComponent(itemID + 2), 0, -1, 5);
        AddComponent(new AddonComponent(itemID + 3), -1, +1, 5);
        AddComponent(new AddonComponent(itemID + 4), 0, 0, 5);
        AddComponent(new AddonComponent(itemID + 5), +1, -1, 5);
        AddComponent(new AddonComponent(itemID + 6), 0, +1, 5);
        AddComponent(new AddonComponent(itemID + 7), +1, 0, 5);
        AddComponent(new AddonComponent(itemID + 8), +1, +1, 5);

        AddComponent(new AddonComponent(0x759), -2, -2, 0);
        AddComponent(new AddonComponent(0x75A), +2, +2, 0);
        AddComponent(new AddonComponent(0x75B), -2, +2, 0);
        AddComponent(new AddonComponent(0x75C), +2, -2, 0);

        AddComponent(new AddonComponent(0x751), -1, +2, 0);
        AddComponent(new AddonComponent(0x751), 0, +2, 0);
        AddComponent(new AddonComponent(0x751), +1, +2, 0);

        AddComponent(new AddonComponent(0x752), +2, -1, 0);
        AddComponent(new AddonComponent(0x752), +2, 0, 0);
        AddComponent(new AddonComponent(0x752), +2, +1, 0);

        AddComponent(new AddonComponent(0x753), -1, -2, 0);
        AddComponent(new AddonComponent(0x753), 0, -2, 0);
        AddComponent(new AddonComponent(0x753), +1, -2, 0);

        AddComponent(new AddonComponent(0x754), -2, -1, 0);
        AddComponent(new AddonComponent(0x754), -2, 0, 0);
        AddComponent(new AddonComponent(0x754), -2, +1, 0);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DuelTeleporterType Type
    {
        get => Components.Count > 0 ? (DuelTeleporterType)Components[0].ItemID : DuelTeleporterType.Squares;
        set
        {
            for (var i = 0; i < Components.Count && i < 9; ++i)
            {
                Components[i].ItemID = i + (int)value;
            }
        }
    }
}
