namespace Server.Factions;

public class Moonglow : Town
{
    public Moonglow() =>
        Definition =
            new TownDefinition(
                3,
                0x186C,
                "Moonglow",
                "Moonglow",
                1011435, // MOONGLOW
                1011563, // TOWN STONE FOR MOONGLOW
                1041037, // The Faction Sigil Monolith of Moonglow
                1041407, // The Faction Town Sigil Monolith of Moonglow
                1041416, // Faction Town Stone of Moonglow
                1041398, // Faction Town Sigil of Moonglow
                1041389, // Corrupted Faction Town Sigil of Moonglow
                new Point3D(4436, 1083, 0),
                new Point3D(4432, 1086, 0)
            );
}
