namespace Server.Factions;

public class Minax : Faction
{
    public Minax()
    {
        Instance = this;

        Definition =
            new FactionDefinition(
                0,
                1645, // dark red
                1109, // shadow
                1645, // join stone : dark red
                1645, // broadcast : dark red
                0x78,
                0x3EAF, // war horse
                "Minax",
                "minax",
                "Min",
                1011534, // MINAX
                1060769, // Minax faction
                1011421, // <center>FOLLOWERS OF MINAX</center>
                /*
                 * The followers of Minax have taken control in the old lands,
                 * and intend to hold it for as long as they can. Allying themselves
                 * with orcs, headless, gazers, trolls, and other beasts, they seek
                 * revenge against Lord British, for slights both real and imagined,
                 * though some of the followers wish only to wreak havoc on the
                 * unsuspecting populace."
                 */
                1011448,
                1011453, // This city is controlled by Minax.
                1042252, // This sigil has been corrupted by the Followers of Minax
                1041043, // The faction signup stone for the Followers of Minax
                1041381, // The Faction Stone of Minax
                1011463, // : Minax
                1005190, // Followers of Minax will now be ignored.
                1005191, // Followers of Minax will now be told to go away.
                1005192, // Followers of Minax will now be hanged by their toes.
                new StrongholdDefinition(
                    new[]
                    {
                        new Rectangle2D(1097, 2570, 70, 50)
                    },
                    new Point3D(1172, 2593, 0),
                    new Point3D(1117, 2587, 18),
                    new[]
                    {
                        new Point3D(1113, 2601, 18),
                        new Point3D(1113, 2598, 18),
                        new Point3D(1113, 2595, 18),
                        new Point3D(1113, 2592, 18),
                        new Point3D(1116, 2601, 18),
                        new Point3D(1116, 2598, 18),
                        new Point3D(1116, 2595, 18),
                        new Point3D(1116, 2592, 18)
                    }
                ),
                new[]
                {
                    new RankDefinition(10, 991, 8, 1060784), // Avenger of Mondain
                    new RankDefinition(9, 950, 7, 1060783),  // Dread Knight
                    new RankDefinition(8, 900, 6, 1060782),  // Warlord
                    new RankDefinition(7, 800, 6, 1060782),  // Warlord
                    new RankDefinition(6, 700, 5, 1060781),  // Executioner
                    new RankDefinition(5, 600, 5, 1060781),  // Executioner
                    new RankDefinition(4, 500, 5, 1060781),  // Executioner
                    new RankDefinition(3, 400, 4, 1060780),  // Defiler
                    new RankDefinition(2, 200, 4, 1060780),  // Defiler
                    new RankDefinition(1, 0, 4, 1060780)     // Defiler
                },
                new[]
                {
                    new GuardDefinition(
                        typeof(FactionHenchman),
                        0x1403,
                        5000,
                        1000,
                        10,
                        1011526, // HENCHMAN
                        1011510  // Hire Henchman
                    ),
                    new GuardDefinition(
                        typeof(FactionMercenary),
                        0x0F62,
                        6000,
                        2000,
                        10,
                        1011527, // MERCENARY
                        1011511  // Hire Mercenary
                    ),
                    new GuardDefinition(
                        typeof(FactionBerserker),
                        0x0F4B,
                        7000,
                        3000,
                        10,
                        1011505, // BERSERKER
                        1011499  // Hire Berserker
                    ),
                    new GuardDefinition(
                        typeof(FactionDragoon),
                        0x1439,
                        8000,
                        4000,
                        10,
                        1011506, // DRAGOON
                        1011500  // Hire Dragoon
                    )
                }
            );
    }

    public static Faction Instance { get; private set; }
}
