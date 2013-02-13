using System;
using Server;
using Server.Mobiles;
using Server.Items;

namespace Server.Engines.CannedEvil
{
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
		Pestilence,
		Custom
	}

	[NoSort]
	[Parsable]
	[PropertyObject]
	public class PlatformData
	{
		[CommandProperty( AccessLevel.GameMaster )]
		public Type Addon { get { return m_Addon; } set { m_Addon = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int AddonHueActive { get { return m_AddonHueActive; } set { m_AddonHueActive = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int AddonHueInactive { get { return m_AddonHueInactive; } set { m_AddonHueInactive = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int AddonHueChampion { get { return m_AddonHueChampion; } set { m_AddonHueChampion = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int PlatformHueActive { get { return m_PlaformHueActive; } set { m_PlaformHueActive = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int PlatformHueInactive { get { return m_PlatformHueInactive; } set { m_PlatformHueInactive = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDPlaformBlocks { get { return m_ItemIDPlaformBlocks; } set { m_ItemIDPlaformBlocks = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDCornerNW { get { return m_ItemIDCornerNW; } set { m_ItemIDCornerNW = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDCornerSE { get { return m_ItemIDCornerSE; } set { m_ItemIDCornerSE = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDCornerSW { get { return m_ItemIDCornerSW; } set { m_ItemIDCornerSW = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDCornerNE { get { return m_ItemIDCornerNE; } set { m_ItemIDCornerNE = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDStepsS { get { return m_ItemIDStepsS; } set { m_ItemIDStepsS = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDStepsE { get { return m_ItemIDStepsE; } set { m_ItemIDStepsE = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDStepsN { get { return m_ItemIDStepsN; } set { m_ItemIDStepsN = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDStepsW { get { return m_ItemIDStepsW; } set { m_ItemIDStepsW = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDRedCandles { get { return m_ItemIDRedCandles; } set { m_ItemIDRedCandles = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HueRedCandles { get { return m_HueRedCandles; } set { m_HueRedCandles = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDWhiteCandles { get { return m_ItemIDWhiteCandles; } set { m_ItemIDWhiteCandles = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HueWhiteCandles { get { return m_HueWhiteCandles; } set { m_HueWhiteCandles = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ItemIDIdol { get { return m_ItemIDIdol; } set { m_ItemIDIdol = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HueIdol { get { return m_HueIdol; } set { m_HueIdol = value; } }

		private int m_ItemIDWhiteCandles;
		private int m_ItemIDRedCandles;
		private int m_ItemIDPlaformBlocks;
		private int m_ItemIDCornerNE;
		private int m_ItemIDCornerNW;
		private int m_ItemIDCornerSW;
		private int m_ItemIDCornerSE;
		private int m_ItemIDStepsS;
		private int m_ItemIDStepsE;
		private int m_ItemIDStepsN;
		private int m_ItemIDStepsW;
		private int m_ItemIDIdol;
		private int m_HueWhiteCandles;
		private int m_HueRedCandles;
		private int m_PlaformHueActive;
		private int m_PlatformHueInactive;
		private Type m_Addon;
		private int m_AddonHueActive;
		private int m_AddonHueInactive;
		private int m_AddonHueChampion;
		private int m_HueIdol;

		public PlatformData()
		{
			m_Addon = typeof( PentagramAddon );
			m_AddonHueInactive = 0x455;
			m_AddonHueActive = 0;
			m_AddonHueChampion = 0x26;
			m_ItemIDPlaformBlocks = 0x3ee;
			m_ItemIDCornerNW = 0x3f7;
			m_ItemIDCornerSE = 0x3f8;
			m_ItemIDCornerSW = 0x3f9;
			m_ItemIDCornerNE = 0x3fa;
			m_ItemIDStepsS = 0x3ef;
			m_ItemIDStepsE = 0x3f0;
			m_ItemIDStepsN = 0x3f1;
			m_ItemIDStepsW = 0x3f2;
			m_PlaformHueActive = 0x455;
			m_PlatformHueInactive = 0x454;
			m_ItemIDRedCandles = 0x1854;
			m_HueRedCandles = 0x26;
			m_ItemIDWhiteCandles = 0x1854;
			m_HueWhiteCandles = 0x26;
			m_ItemIDIdol = 0x1f18;
			m_HueIdol = 0;
		}
	}

	[NoSort]
	[Parsable]
	[PropertyObject]
	public class CustomSpawnData
	{
		[CommandProperty( AccessLevel.GameMaster )]
		public string SpawnName { get { return m_SpawnName; } set { m_SpawnName = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string ChampType { get { return m_ChampType; } set { m_ChampType = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string TitleLevel1 { get { return m_TitleLevel1; } set { m_TitleLevel1 = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string TitleLevel2 { get { return m_TitleLevel2; } set { m_TitleLevel2 = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string TitleLevel3 { get { return m_TitleLevel3; } set { m_TitleLevel3 = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string MobLevel1A { get { return m_MobLevel1A; } set { m_MobLevel1A = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string MobLevel1B { get { return m_MobLevel1B; } set { m_MobLevel1B = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string MobLevel2A { get { return m_MobLevel2A; } set { m_MobLevel2A = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string MobLevel2B { get { return m_MobLevel2B; } set { m_MobLevel2B = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string MobLevel3A { get { return m_MobLevel3A; } set { m_MobLevel3A = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string MobLevel3B { get { return m_MobLevel3B; } set { m_MobLevel3B = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string MobLevel4A { get { return m_MobLevel4A; } set { m_MobLevel4A = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string MobLevel4B { get { return m_MobLevel4B; } set { m_MobLevel4B = value; } }

		private string m_SpawnName;
		private string m_ChampType;
		private string m_TitleLevel1;
		private string m_TitleLevel2;
		private string m_TitleLevel3;
		private string m_MobLevel1A;
		private string m_MobLevel1B;
		private string m_MobLevel2A;
		private string m_MobLevel2B;
		private string m_MobLevel3A;
		private string m_MobLevel3B;
		private string m_MobLevel4A;
		private string m_MobLevel4B;

		public CustomSpawnData()
		{
			m_SpawnName = "Demise";
			m_ChampType = "GreaterMongbat";
			m_TitleLevel1 = "Challenger";
			m_TitleLevel2 = "Conqueror";
			m_TitleLevel3 = "Champion";
			m_MobLevel1A = "Mongbat";
			m_MobLevel1B = "Mongbat";
			m_MobLevel2A = "Mongbat";
			m_MobLevel2B = "Mongbat";
			m_MobLevel3A = "Mongbat";
			m_MobLevel3B = "Mongbat";
			m_MobLevel4A = "Mongbat";
			m_MobLevel4B = "Mongbat";
		}
	}

	public class ChampionSpawnInfo
	{
		private string m_Name;
		private Type m_Champion;
		private Type[][] m_SpawnTypes;
		private string[] m_LevelNames;

		public string Name { get { return m_Name; } }
		public Type Champion { get { return m_Champion; } }
		public Type[][] SpawnTypes { get { return m_SpawnTypes; } }
		public string[] LevelNames { get { return m_LevelNames; } }

		public ChampionSpawnInfo( string name, Type champion, string[] levelNames, Type[][] spawnTypes )
		{
			m_Name = name;
			m_Champion = champion;
			m_LevelNames = levelNames;
			m_SpawnTypes = spawnTypes;
		}

		public static ChampionSpawnInfo[] Table{ get { return m_Table; } }

		private static readonly ChampionSpawnInfo[] m_Table = new ChampionSpawnInfo[]
			{
				new ChampionSpawnInfo( "Abyss", typeof( Semidar ), new string[]{ "Foe", "Assassin", "Conqueror" }, new Type[][]	// Abyss
				{																											// Abyss
					new Type[]{ typeof( GreaterMongbat ), typeof( Imp ) },													// Level 1
					new Type[]{ typeof( Gargoyle ), typeof( Harpy ) },														// Level 2
					new Type[]{ typeof( FireGargoyle ), typeof( StoneGargoyle ) },											// Level 3
					new Type[]{ typeof( Daemon ), typeof( Succubus ) }														// Level 4
				} ),
				new ChampionSpawnInfo( "Arachnid", typeof( Mephitis ), new string[]{ "Bane", "Killer", "Vanquisher" }, new Type[][]	// Arachnid
				{																											// Arachnid
					new Type[]{ typeof( Scorpion ), typeof( GiantSpider ) },												// Level 1
					new Type[]{ typeof( TerathanDrone ), typeof( TerathanWarrior ) },										// Level 2
					new Type[]{ typeof( DreadSpider ), typeof( TerathanMatriarch ) },										// Level 3
					new Type[]{ typeof( PoisonElemental ), typeof( TerathanAvenger ) }										// Level 4
				} ),
				new ChampionSpawnInfo( "Cold Blood", typeof( Rikktor ), new string[]{ "Blight", "Slayer", "Destroyer" }, new Type[][]	// Cold Blood
				{																											// Cold Blood
					new Type[]{ typeof( Lizardman ), typeof( Snake ) },														// Level 1
					new Type[]{ typeof( LavaLizard ), typeof( OphidianWarrior ) },											// Level 2
					new Type[]{ typeof( Drake ), typeof( OphidianArchmage ) },												// Level 3
					new Type[]{ typeof( Dragon ), typeof( OphidianKnight ) }												// Level 4
				} ),
				new ChampionSpawnInfo( "Forest Lord", typeof( LordOaks ), new string[]{ "Enemy", "Curse", "Slaughterer" }, new Type[][]	// Forest Lord
				{																											// Forest Lord
					new Type[]{ typeof( Pixie ), typeof( ShadowWisp ) },													// Level 1
					new Type[]{ typeof( Kirin ), typeof( Wisp ) },															// Level 2
					new Type[]{ typeof( Centaur ), typeof( Unicorn ) },														// Level 3
					new Type[]{ typeof( EtherealWarrior ), typeof( SerpentineDragon ) }										// Level 4
				} ),
				new ChampionSpawnInfo( "Vermin Horde", typeof( Barracoon ), new string[]{ "Adversary", "Subjugator", "Eradicator" }, new Type[][]	// Vermin Horde
				{																											// Vermin Horde
					new Type[]{ typeof( GiantRat ), typeof( Slime ) },														// Level 1
					new Type[]{ typeof( DireWolf ), typeof( Ratman ) },														// Level 2
					new Type[]{ typeof( HellHound ), typeof( RatmanMage ) },												// Level 3
					new Type[]{ typeof( RatmanArcher ), typeof( SilverSerpent ) }											// Level 4
				} ),
				new ChampionSpawnInfo( "Unholy Terror", typeof( Neira ), new string[]{ "Scourge", "Punisher", "Nemesis" }, new Type[][]	// Unholy Terror
				{																											// Unholy Terror
					(Core.AOS ? 
					new Type[]{ typeof( Bogle ), typeof( Ghoul ), typeof( Shade ), typeof( Spectre ), typeof( Wraith ) }	// Level 1 (Pre-AoS)
					: new Type[]{ typeof( Ghoul ), typeof( Shade ), typeof( Spectre ), typeof( Wraith ) } ),				// Level 1

					new Type[]{ typeof( BoneMagi ), typeof( Mummy ), typeof( SkeletalMage ) },								// Level 2
					new Type[]{ typeof( BoneKnight ), typeof( Lich ), typeof( SkeletalKnight ) },							// Level 3
					new Type[]{ typeof( LichLord ), typeof( RottingCorpse ) }												// Level 4
				} ),
				new ChampionSpawnInfo( "Sleeping Dragon", typeof( Serado ), new string[]{ "Rival", "Challenger", "Antagonist" } , new Type[][]
				{																											// Unholy Terror
					new Type[]{ typeof( DeathwatchBeetleHatchling ), typeof( Lizardman ) },
					new Type[]{ typeof( DeathwatchBeetle ), typeof( Kappa ) },
					new Type[]{ typeof( LesserHiryu ), typeof( RevenantLion ) },
					new Type[]{ typeof( Hiryu ), typeof( Oni ) }
				} ),
				new ChampionSpawnInfo( "Glade", typeof( Twaulo ), new string[]{ "Banisher", "Enforcer", "Eradicator" } , new Type[][]
				{																											// Glade
					new Type[]{ typeof( Pixie ), typeof( ShadowWisp ) },
					new Type[]{ typeof( Centaur ), typeof( MLDryad ) },
					new Type[]{ typeof( Satyr ), typeof( CuSidhe ) },
					new Type[]{ typeof( FerelTreefellow ), typeof( RagingGrizzlyBear ) }
				} ),
				new ChampionSpawnInfo( "The Corrupt", typeof( Ilhenir ), new string[]{ "Cleanser", "Expunger", "Depurator" } , new Type[][]
				{																											// Unholy Terror
					new Type[]{ typeof( PlagueSpawn ), typeof( Bogling ) },
					new Type[]{ typeof( PlagueBeast ), typeof( BogThing ) },
					new Type[]{ typeof( PlagueBeastLord ), typeof( InterredGrizzle ) },
					new Type[]{ typeof( FetidEssence ), typeof( PestilentBandage ) }
				} )
			};

		public static ChampionSpawnInfo GetInfo( ChampionSpawnType type )
		{
			int v = (int)type;

			if( v < 0 || v >= m_Table.Length )
				v = 0;

			return m_Table[v];
		}
	}
}