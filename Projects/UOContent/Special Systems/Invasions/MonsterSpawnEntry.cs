using Server.Mobiles;
using System;


namespace Server
{
    public class MonsterSpawnEntry
    {
        #region MonsterSpawnEntries
        public static MonsterSpawnEntry[] Undead = new MonsterSpawnEntry[]
        {
			//Monster													//Amount
			new MonsterSpawnEntry( typeof( Zombie ),                        165 ),
            new MonsterSpawnEntry( typeof( Skeleton ),                      65 ),
            new MonsterSpawnEntry( typeof( SkeletalMage ),                  40 ),
            new MonsterSpawnEntry( typeof( BoneKnight ),                    45 ),
            new MonsterSpawnEntry( typeof( SkeletalKnight ),                45 ),
            new MonsterSpawnEntry( typeof( Lich ),                          45 ),
            new MonsterSpawnEntry( typeof( Ghoul ),                         40 ),
            new MonsterSpawnEntry( typeof( BoneMagi ),                      40 ),
            new MonsterSpawnEntry( typeof( Wraith ),                        35 ),
            new MonsterSpawnEntry( typeof( RottingCorpse ),                 35 ),
            new MonsterSpawnEntry( typeof( LichLord ),                      55 ),
            new MonsterSpawnEntry( typeof( Spectre ),                       30 ),
            new MonsterSpawnEntry( typeof( Shade ),                         30 ),
            new MonsterSpawnEntry( typeof( AncientLich ),                   50 )
        };

        public static MonsterSpawnEntry[] Humanoid = new MonsterSpawnEntry[]
        {
			//Monster														//Amount
			new MonsterSpawnEntry( typeof( Brigand ),                       60 ),
            new MonsterSpawnEntry( typeof( Executioner ),                   30 ),
            new MonsterSpawnEntry( typeof( EvilMage ),                      70 ),
            new MonsterSpawnEntry( typeof( EvilMageLord ),                  40 ),
            new MonsterSpawnEntry( typeof( Ettin ),                         45 ),
            new MonsterSpawnEntry( typeof( Ogre ),                          45 ),
            new MonsterSpawnEntry( typeof( OgreLord ),                      40 ),
            new MonsterSpawnEntry( typeof( ArcticOgreLord ),                40 ),
            new MonsterSpawnEntry( typeof( Troll ),                         55 ),
            new MonsterSpawnEntry( typeof( Cyclops ),                       55 ),
            new MonsterSpawnEntry( typeof( Titan ),                         40 )
        };

        public static MonsterSpawnEntry[] OrcsandRatmen = new MonsterSpawnEntry[]
        {
			//Monster														//Amount
			new MonsterSpawnEntry( typeof( Orc ),                           80 ),
            new MonsterSpawnEntry( typeof( OrcishMage ),                    45 ),
            new MonsterSpawnEntry( typeof( OrcishLord ),                    55 ),
            new MonsterSpawnEntry( typeof( OrcCaptain ),                    50 ),
            new MonsterSpawnEntry( typeof( OrcBomber ),                     55 ),
            new MonsterSpawnEntry( typeof( OrcBrute ),                      40 ),
            new MonsterSpawnEntry( typeof( Ratman ),                        80 ),
            new MonsterSpawnEntry( typeof( RatmanArcher ),                  50 ),
            new MonsterSpawnEntry( typeof( RatmanMage ),                    45 )
        };

        public static MonsterSpawnEntry[] Elementals = new MonsterSpawnEntry[]
        {
			//Monster														//Amount
			new MonsterSpawnEntry( typeof( EarthElemental ),                95 ),
            new MonsterSpawnEntry( typeof( AirElemental ),                  70 ),
            new MonsterSpawnEntry( typeof( FireElemental ),                 60 ),
            new MonsterSpawnEntry( typeof( WaterElemental ),                60 ),
            new MonsterSpawnEntry( typeof( SnowElemental ),                 40 ),
            new MonsterSpawnEntry( typeof( IceElemental ),                  40 ),
            new MonsterSpawnEntry( typeof( Efreet ),                        45 ),
            new MonsterSpawnEntry( typeof( PoisonElemental ),               35 ),
            new MonsterSpawnEntry( typeof( BloodElemental ),                35 )
        };

        public static MonsterSpawnEntry[] OreElementals = new MonsterSpawnEntry[]
        {
			//Monster														//Amount
			new MonsterSpawnEntry( typeof( DullCopperElemental ),           90 ),
            new MonsterSpawnEntry( typeof( CopperElemental ),               80 ),
            new MonsterSpawnEntry( typeof( BronzeElemental ),               50 ),
            new MonsterSpawnEntry( typeof( ShadowIronElemental ),           60 ),
            new MonsterSpawnEntry( typeof( GoldenElemental ),               55 ),
            new MonsterSpawnEntry( typeof( AgapiteElemental ),              45 ),
            new MonsterSpawnEntry( typeof( VeriteElemental ),               40 ),
            new MonsterSpawnEntry( typeof( ValoriteElemental ),             40 )
        };

        public static MonsterSpawnEntry[] Ophidian = new MonsterSpawnEntry[]
        {
			//Monster														//Amount
			new MonsterSpawnEntry( typeof( OphidianWarrior ),               100 ),
            new MonsterSpawnEntry( typeof( OphidianMage ),                  70 ),
            new MonsterSpawnEntry( typeof( OphidianArchmage ),              30 ),
            new MonsterSpawnEntry( typeof( OphidianKnight ),                35 ),
            new MonsterSpawnEntry( typeof( OphidianMatriarch ),             35 )
        };

        public static MonsterSpawnEntry[] Arachnid = new MonsterSpawnEntry[]
        {
			//Monster														//Amount
			new MonsterSpawnEntry( typeof( Scorpion ),                      75 ),
            new MonsterSpawnEntry( typeof( GiantSpider ),                   75 ),
            new MonsterSpawnEntry( typeof( TerathanDrone ),                 45 ),
            new MonsterSpawnEntry( typeof( TerathanWarrior ),               30 ),
            new MonsterSpawnEntry( typeof( TerathanMatriarch ),             45 ),
            new MonsterSpawnEntry( typeof( TerathanAvenger ),               45 ),
            new MonsterSpawnEntry( typeof( DreadSpider ),                   40 ),
            new MonsterSpawnEntry( typeof( FrostSpider ),                   35 )
        };

        public static MonsterSpawnEntry[] Snakes = new MonsterSpawnEntry[]
        {
			//Monster														//Amount
			new MonsterSpawnEntry( typeof( Snake ),                         95 ),
            new MonsterSpawnEntry( typeof( GiantSerpent ),                  95 ),
            new MonsterSpawnEntry( typeof( LavaSnake ),                     50 ),
            new MonsterSpawnEntry( typeof( LavaSerpent ),                   55 ),
            new MonsterSpawnEntry( typeof( IceSnake ),                      50 ),
            new MonsterSpawnEntry( typeof( IceSerpent ),                    55 ),
            new MonsterSpawnEntry( typeof( SilverSerpent ),                 40 )
        };

        public static MonsterSpawnEntry[] Abyss = new MonsterSpawnEntry[]
        {
			//Monster														//Amount
			new MonsterSpawnEntry( typeof( Gargoyle ),                      100 ),
            new MonsterSpawnEntry( typeof( StoneGargoyle ),                 60 ),
            new MonsterSpawnEntry( typeof( FireGargoyle ),                  60 ),
            new MonsterSpawnEntry( typeof( Daemon ),                        60 ),
            new MonsterSpawnEntry( typeof( IceFiend ),                      50 ),
            new MonsterSpawnEntry( typeof( Balron ),                        30 )
        };

        public static MonsterSpawnEntry[] DragonKind = new MonsterSpawnEntry[]
        {
			//Monster														//Amount
			new MonsterSpawnEntry( typeof( Wyvern ),                        100 ),
            new MonsterSpawnEntry( typeof( Drake ),                         60 ),
            new MonsterSpawnEntry( typeof( Dragon ),                        60 ),
            new MonsterSpawnEntry( typeof( WhiteWyrm ),                     60 ),
            new MonsterSpawnEntry( typeof( ShadowWyrm ),                    10 ),
            new MonsterSpawnEntry( typeof( AncientWyrm ),                   30 )
        };

#endregion

        private Type m_Monster;
        private int m_Amount;

        public Type Monster { get { return m_Monster; } set { m_Monster = value; } }
        public int Amount { get { return m_Amount; } set { m_Amount = value; } }

        public MonsterSpawnEntry(Type monster, int amount)
        {
            m_Monster = monster;
            m_Amount = amount;
        }
    }
}
