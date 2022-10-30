namespace Server.Factions;

public class Trinsic : Town
{
    public Trinsic() =>
        Definition =
            new TownDefinition(
                1,
                0x186A,
                "Trinsic",
                "Trinsic",
                1011434, // TRINSIC
                1011562, // TOWN STONE FOR TRINSIC
                1041035, // The Faction Sigil Monolith of Trinsic
                1041405, // The Faction Town Sigil Monolith of Trinsic
                1041414, // Faction Town Stone of Trinsic
                1041396, // Faction Town Sigil of Trinsic
                1041387, // Corrupted Faction Town Sigil of Trinsic
                new Point3D(1914, 2717, 20),
                new Point3D(1909, 2720, 20)
            );
}
