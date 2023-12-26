using System;
using Server.Mobiles;

namespace Server.Engines.CannedEvil;

public enum ChampionSpawnType
{
    Abyss,
    Arachnid,
    ColdBlood,
    ForestLord,
    VerminHorde,
    UnholyTerror,
    SleepingDragon,
    Glade,
    Corrupt,
    // Unliving,
    // Pit,
    // DragonTurtle,
    // Khaldun
}

public class ChampionSpawnInfo
{
    public ChampionSpawnType Type { get; }

    public Type Champion { get; }

    public Type[][] SpawnTypes { get; }

    public TextDefinition[] LevelNames { get; }

    public ChampionSpawnInfo(ChampionSpawnType type, Type champion, TextDefinition[] levelNames, Type[][] spawnTypes)
    {
        Type = type;
        Champion = champion;
        LevelNames = levelNames;
        SpawnTypes = spawnTypes;
    }

    // After updating this table, make sure to update ChampionTitleContext.
    public static ChampionSpawnInfo[] Table { get; } =
    [
        new(ChampionSpawnType.Abyss, typeof(Semidar),
            [1113118, 1113107, 1113096],
            [
                [typeof(GreaterMongbat), typeof(Imp)],         // Level 1
                [typeof(Gargoyle), typeof(Harpy)],             // Level 2
                [typeof(FireGargoyle), typeof(StoneGargoyle)], // Level 3
                [typeof(Daemon), typeof(Succubus)]             // Level 4
            ]
        ),
        new(ChampionSpawnType.Arachnid, typeof(Mephitis),
            [1113117, 1113106, 1113095],
            [
                [typeof(Scorpion), typeof(GiantSpider)],           // Level 1
                [typeof(TerathanDrone), typeof(TerathanWarrior)],  // Level 2
                [typeof(DreadSpider), typeof(TerathanMatriarch)],  // Level 3
                [typeof(PoisonElemental), typeof(TerathanAvenger)] // Level 4
            ]
        ),
        new(ChampionSpawnType.ColdBlood, typeof(Rikktor),
            [1113115, 1113104, 1113093],
            [
                [typeof(Lizardman), typeof(GiantSerpent)],     // Level 1
                [typeof(LavaLizard), typeof(OphidianWarrior)], // Level 2
                [typeof(Drake), typeof(OphidianArchmage)],     // Level 3
                [typeof(Dragon), typeof(OphidianKnight)]       // Level 4
            ]
        ),
        new(ChampionSpawnType.ForestLord, typeof(LordOaks),
            [1113116, 1113105, 1113094],
            [
                [typeof(Pixie), typeof(ShadowWisp)],                // Level 1
                [typeof(Kirin), typeof(Wisp)],                      // Level 2
                [typeof(Centaur), typeof(Unicorn)],                 // Level 3
                [typeof(EtherealWarrior), typeof(SerpentineDragon)] // Level 4
            ]
        ),
        new(ChampionSpawnType.VerminHorde, typeof(Barracoon),
            [1113119, 1113108, 1113097],
            [
                [typeof(GiantRat), typeof(Slime)],            // Level 1
                [typeof(DireWolf), typeof(Ratman)],           // Level 2
                [typeof(HellHound), typeof(RatmanMage)],      // Level 3
                [typeof(RatmanArcher), typeof(SilverSerpent)] // Level 4
            ]
        ),
        new(ChampionSpawnType.UnholyTerror, typeof(Neira),
            [1113120, 1113109, 1113098],
            [
                Core.AOS ? // Level 1
                    [typeof(Bogle), typeof(Ghoul), typeof(Shade), typeof(Spectre), typeof(Wraith)]
                    : [typeof(Ghoul), typeof(Shade), typeof(Spectre), typeof(Wraith)],

                [typeof(BoneMagi), typeof(Mummy), typeof(SkeletalMage)],    // Level 2
                [typeof(BoneKnight), typeof(Lich), typeof(SkeletalKnight)], // Level 3
                [typeof(LichLord), typeof(RottingCorpse)]                   // Level 4
            ]
        ),
        new(ChampionSpawnType.SleepingDragon, typeof(Serado),
            [1113121, 1113110, 1113099],
            [
                [typeof(DeathwatchBeetleHatchling), typeof(Lizardman)], // Level 1
                [typeof(DeathwatchBeetle), typeof(Kappa)],              // Level 2
                [typeof(LesserHiryu), typeof(RevenantLion)],            // Level 3
                [typeof(Hiryu), typeof(Oni)]                            // Level 4
            ]
        ),
        new(ChampionSpawnType.Glade, typeof(Twaulo),
            [1113123, 1113112, 1113101],
            [
                [typeof(Pixie), typeof(ShadowWisp)],                 // Level 1
                [typeof(Centaur), typeof(MLDryad)],                  // Level 2
                [typeof(Satyr), typeof(CuSidhe)],                    // Level 3
                [typeof(FeralTreefellow), typeof(RagingGrizzlyBear)] // Level 4
            ]
        ),
        new(ChampionSpawnType.Corrupt, typeof(Ilhenir),
            [1113122, 1113111, 1113100],
            [
                [typeof(PlagueSpawn), typeof(Bogling)],             // Level 1
                [typeof(PlagueBeast), typeof(BogThing)],            // Level 2
                [typeof(PlagueBeastLord), typeof(InterredGrizzle)], // Level 3
                [typeof(FetidEssence), typeof(PestilentBandage)]    // Level 4
            ]
        )
        // new(ChampionSpawnType.Unliving, typeof(PrimevalLich), new TextDefinition[]{ 1113124, 1113113, 1113102 }, new[]
        // {
        //     new[]{ typeof(GoreFiend), typeof(VampireBat) },
        //     new[]{ typeof(FleshGolem), typeof(DarkWisp) },
        //     new[]{ typeof(UndeadGargoyle), typeof(Wight) },
        //     new[]{ typeof(SkeletalDrake), typeof(DreamWraith) }
        // }),
        // new(ChampionSpawnType.Pit, typeof(AbyssalInfernal), new TextDefinition[]{ 1113125, 1113114, 1113103 }, new[]
        // {
        //     new[]{ typeof(HordeMinion), typeof(ChaosDaemon) },
        //     new[]{ typeof(StoneHarpy), typeof(ArcaneDaemon) },
        //     new[]{ typeof(PitFiend), typeof(Moloch) },
        //     new[]{ typeof(ArchDaemon), typeof(AbyssalAbomination) }
        // }),
        // new(ChampionSpawnType.Valley, typeof(DragonTurtle), new TextDefinition[]{ "Explorer", "Huntsman", "Msafiri" }, new[]
        // {
        //     new[]{ typeof(MyrmidexDrone), typeof(MyrmidexLarvae) },
        //     new[]{ typeof(SilverbackGorilla), typeof(WildTiger) },
        //     new[]{ typeof(GreaterPhoenix ), typeof(Infernus) },
        //     new[]{ typeof(Dimetrosaur), typeof(Allosaurus) }
        // }),
        // new(ChampionSpawnType.Khaldun, typeof(KhalAnkur), new TextDefinition[]{ "Banisher", "Enforcer", "Eradicator" }, new[]
        // {
        //     new[]{ typeof(SkelementalKnight), typeof(KhaldunBlood) },
        //     new[]{ typeof(SkelementalMage), typeof(Viscera) },
        //     new[]{ typeof(CultistAmbusher), typeof(ShadowFiend) },
        //     new[]{ typeof(KhalAnkurWarriors) }
        // })
    ];

    public static ChampionSpawnInfo GetInfo(ChampionSpawnType type)
    {
        var v = (int)type;

        if (v < 0 || v >= Table.Length)
        {
            v = 0;
        }

        return Table[v];
    }
}
