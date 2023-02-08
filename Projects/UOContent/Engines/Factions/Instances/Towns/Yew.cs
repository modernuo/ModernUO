namespace Server.Factions;

public class Yew : Town
{
    public Yew() =>
        Definition =
            new TownDefinition(
                4,
                0x186D,
                "Yew",
                "Yew",
                1011438, // YEW
                1011565, // TOWN STONE FOR YEW
                1041038, // The Faction Sigil Monolith of Yew
                1041408, // The Faction Town Sigil Monolith of Yew
                1041417, // Faction Town Stone of Yew
                1041399, // Faction Town Sigil of Yew
                1041390, // Corrupted Faction Town Sigil of Yew
                new Point3D(548, 979, 0),
                new Point3D(542, 980, 0)
            );
}
