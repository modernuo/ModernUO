namespace Server.Factions;

public class Vesper : Town
{
    public Vesper() =>
        Definition =
            new TownDefinition(
                5,
                0x186E,
                "Vesper",
                "Vesper",
                1016413, // VESPER
                1011566, // TOWN STONE FOR VESPER
                1041039, // The Faction Sigil Monolith of Vesper
                1041409, // The Faction Town Sigil Monolith of Vesper
                1041418, // Faction Town Stone of Vesper
                1041400, // Faction Town Sigil of Vesper
                1041391, // Corrupted Faction Town Sigil of Vesper
                new Point3D(2982, 818, 0),
                new Point3D(2985, 821, 0)
            );
}
