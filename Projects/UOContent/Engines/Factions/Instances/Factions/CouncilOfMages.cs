namespace Server.Factions;

public class CouncilOfMages : Faction
{
    public CouncilOfMages()
    {
        Instance = this;

        Definition =
            new FactionDefinition(
                1,
                1325, // blue
                1310, // bluish white
                1325, // join stone : blue
                1325, // broadcast : blue
                0x77,
                0x3EB1, // war horse
                "Council of Mages",
                "council",
                "CoM",
                1011535, // COUNCIL OF MAGES
                1060770, // Council of Mages faction
                1011422, // <center>COUNCIL OF MAGES</center>
                /*
                 * The council of Mages have their roots in the city of Moonglow, where
                 * they once convened. They began as a small movement, dedicated to
                 * calling forth the Stranger, who saved the lands once before.  A
                 * series of war and murders and misbegotten trials by those loyal to
                 * Lord British has caused the group to take up the banner of war.
                 */
                1011449,
                1011455, // This city is controlled by the Council of Mages.
                1042253, // This sigil has been corrupted by the Council of Mages
                1041044, // The faction signup stone for the Council of Mages
                1041382, // The Faction Stone of the Council of Mages
                1011464, // : Council of Mages
                1005187, // Members of the Council of Mages will now be ignored.
                1005188, // Members of the Council of Mages will now be warned to leave.
                1005189, // Members of the Council of Mages will now be beaten with a stick.
                // Moonglow
                new StrongholdDefinition(
                    new[]
                    {
                        new Rectangle2D(4463, 1487, 15, 35),
                        new Rectangle2D(4450, 1522, 35, 48)
                    },
                    new Point3D(4469, 1486, 0),
                    new Point3D(4457, 1544, 0),
                    new[]
                    {
                        new Point3D(4464, 1534, 21),
                        new Point3D(4470, 1536, 21),
                        new Point3D(4468, 1534, 21),
                        new Point3D(4470, 1534, 21),
                        new Point3D(4468, 1536, 21),
                        new Point3D(4466, 1534, 21),
                        new Point3D(4466, 1536, 21),
                        new Point3D(4464, 1536, 21)
                    }
                ),
                // Magincia
                /* new StrongholdDefinition(
                  new Rectangle2D[]
                  {
                    new Rectangle2D( 3756, 2232, 4, 23 ),
                    new Rectangle2D( 3760, 2227, 60, 28 ),
                    new Rectangle2D( 3782, 2219, 18, 8 ),
                    new Rectangle2D( 3778, 2255, 35, 17 )
                  },
                  new Point3D( 3750, 2241, 20 ),
                  new Point3D( 3795, 2259, 20 ),
                  new Point3D[]
                  {
                    new Point3D( 3793, 2255, 20 ),
                    new Point3D( 3793, 2252, 20 ),
                    new Point3D( 3793, 2249, 20 ),
                    new Point3D( 3793, 2246, 20 ),
                    new Point3D( 3797, 2255, 20 ),
                    new Point3D( 3797, 2252, 20 ),
                    new Point3D( 3797, 2249, 20 ),
                    new Point3D( 3797, 2246, 20 )
                  } ), */
                new[]
                {
                    new RankDefinition(10, 991, 8, 1060789), // Inquisitor of the Council
                    new RankDefinition(9, 950, 7, 1060788),  // Archon of Principle
                    new RankDefinition(8, 900, 6, 1060787),  // Luminary
                    new RankDefinition(7, 800, 6, 1060787),  // Luminary
                    new RankDefinition(6, 700, 5, 1060786),  // Diviner
                    new RankDefinition(5, 600, 5, 1060786),  // Diviner
                    new RankDefinition(4, 500, 5, 1060786),  // Diviner
                    new RankDefinition(3, 400, 4, 1060785),  // Mystic
                    new RankDefinition(2, 200, 4, 1060785),  // Mystic
                    new RankDefinition(1, 0, 4, 1060785)     // Mystic
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
                        typeof(FactionSorceress),
                        0x0E89,
                        7000,
                        3000,
                        10,
                        1011507, // SORCERESS
                        1011501  // Hire Sorceress
                    ),
                    new GuardDefinition(
                        typeof(FactionWizard),
                        0x13F8,
                        8000,
                        4000,
                        10,
                        1011508, // ELDER WIZARD
                        1011502  // Hire Elder Wizard
                    )
                }
            );
    }

    public static Faction Instance { get; private set; }
}
