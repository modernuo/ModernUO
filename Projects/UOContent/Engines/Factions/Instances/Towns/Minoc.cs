namespace Server.Factions;

public class Minoc : Town
{
    public Minoc() =>
        Definition =
            new TownDefinition(
                2,
                0x186B,
                "Minoc",
                "Minoc",
                1011437, // MINOC
                1011564, // TOWN STONE FOR MINOC
                1041036, // The Faction Sigil Monolith of Minoc
                1041406, // The Faction Town Sigil Monolith Minoc
                1041415, // Faction Town Stone of Minoc
                1041397, // Faction Town Sigil of Minoc
                1041388, // Corrupted Faction Town Sigil of Minoc
                new Point3D(2471, 439, 15),
                new Point3D(2469, 445, 15)
            );
}
