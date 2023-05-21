namespace Server.Factions;

public class Magincia : Town
{
    public Magincia() =>
        Definition =
            new TownDefinition(
                7,
                0x1870,
                "Magincia",
                "Magincia",
                1011440, // MAGINCIA
                1011568, // TOWN STONE FOR MAGINCIA
                1041041, // The Faction Sigil Monolith of Magincia
                1041411, // The Faction Town Sigil Monolith of Magincia
                1041420, // Faction Town Stone of Magincia
                1041402, // Faction Town Sigil of Magincia
                1041393, // Corrupted Faction Town Sigil of Magincia
                new Point3D(3714, 2235, 20),
                new Point3D(3712, 2230, 20)
            );
}
