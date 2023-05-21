namespace Server.Factions;

public class SkaraBrae : Town
{
    public SkaraBrae() =>
        Definition =
            new TownDefinition(
                6,
                0x186F,
                "Skara Brae",
                "Skara Brae",
                1011439, // SKARA BRAE
                1011567, // TOWN STONE FOR SKARA BRAE
                1041040, // The Faction Sigil Monolith of Skara Brae
                1041410, // The Faction Town Sigil Monolith of Skara Brae
                1041419, // Faction Town Stone of Skara Brae
                1041401, // Faction Town Sigil of Skara Brae
                1041392, // Corrupted Faction Town Sigil of Skara Brae
                new Point3D(576, 2200, 0),
                new Point3D(572, 2196, 0)
            );
}
