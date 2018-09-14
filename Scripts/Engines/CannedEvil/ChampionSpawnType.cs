using System;
using Server.Mobiles;

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
		Pestilence
	}

	public class ChampionSpawnInfo
	{
		private string m_Name;
		private Type m_Champion;
		private Type[][] m_SpawnTypes;
		private string[] m_LevelNames;

		public string Name  => m_Name;
		public Type Champion  => m_Champion;
		public Type[][] SpawnTypes  => m_SpawnTypes;
		public string[] LevelNames  => m_LevelNames;

		public ChampionSpawnInfo( string name, Type champion, string[] levelNames, Type[][] spawnTypes )
		{
			m_Name = name;
			m_Champion = champion;
			m_LevelNames = levelNames;
			m_SpawnTypes = spawnTypes;
		}

		public static ChampionSpawnInfo[] Table => m_Table;

		private static readonly ChampionSpawnInfo[] m_Table = {
				new ChampionSpawnInfo( "Abyss", typeof( Semidar ), new[]{ "Foe", "Assassin", "Conqueror" }, new[] // Abyss
				{																											// Abyss
					new[]{ typeof( GreaterMongbat ), typeof( Imp ) },													// Level 1
					new[]{ typeof( Gargoyle ), typeof( Harpy ) },														// Level 2
					new[]{ typeof( FireGargoyle ), typeof( StoneGargoyle ) },											// Level 3
					new[]{ typeof( Daemon ), typeof( Succubus ) }														// Level 4
				} ),
				new ChampionSpawnInfo( "Arachnid", typeof( Mephitis ), new[]{ "Bane", "Killer", "Vanquisher" }, new[] // Arachnid
				{																											// Arachnid
					new[]{ typeof( Scorpion ), typeof( GiantSpider ) },												// Level 1
					new[]{ typeof( TerathanDrone ), typeof( TerathanWarrior ) },										// Level 2
					new[]{ typeof( DreadSpider ), typeof( TerathanMatriarch ) },										// Level 3
					new[]{ typeof( PoisonElemental ), typeof( TerathanAvenger ) }										// Level 4
				} ),
				new ChampionSpawnInfo( "Cold Blood", typeof( Rikktor ), new[]{ "Blight", "Slayer", "Destroyer" }, new[] // Cold Blood
				{																											// Cold Blood
					new[]{ typeof( Lizardman ), typeof( Snake ) },														// Level 1
					new[]{ typeof( LavaLizard ), typeof( OphidianWarrior ) },											// Level 2
					new[]{ typeof( Drake ), typeof( OphidianArchmage ) },												// Level 3
					new[]{ typeof( Dragon ), typeof( OphidianKnight ) }												// Level 4
				} ),
				new ChampionSpawnInfo( "Forest Lord", typeof( LordOaks ), new[]{ "Enemy", "Curse", "Slaughterer" }, new[] // Forest Lord
				{																											// Forest Lord
					new[]{ typeof( Pixie ), typeof( ShadowWisp ) },													// Level 1
					new[]{ typeof( Kirin ), typeof( Wisp ) },															// Level 2
					new[]{ typeof( Centaur ), typeof( Unicorn ) },														// Level 3
					new[]{ typeof( EtherealWarrior ), typeof( SerpentineDragon ) }										// Level 4
				} ),
				new ChampionSpawnInfo( "Vermin Horde", typeof( Barracoon ), new[]{ "Adversary", "Subjugator", "Eradicator" }, new[] // Vermin Horde
				{																											// Vermin Horde
					new[]{ typeof( GiantRat ), typeof( Slime ) },														// Level 1
					new[]{ typeof( DireWolf ), typeof( Ratman ) },														// Level 2
					new[]{ typeof( HellHound ), typeof( RatmanMage ) },												// Level 3
					new[]{ typeof( RatmanArcher ), typeof( SilverSerpent ) }											// Level 4
				} ),
				new ChampionSpawnInfo( "Unholy Terror", typeof( Neira ), new[]{ "Scourge", "Punisher", "Nemesis" }, new[] // Unholy Terror
				{																											// Unholy Terror
					(Core.AOS ?
					new[]{ typeof( Bogle ), typeof( Ghoul ), typeof( Shade ), typeof( Spectre ), typeof( Wraith ) }	// Level 1 (Pre-AoS)
					: new[]{ typeof( Ghoul ), typeof( Shade ), typeof( Spectre ), typeof( Wraith ) } ),				// Level 1

					new[]{ typeof( BoneMagi ), typeof( Mummy ), typeof( SkeletalMage ) },								// Level 2
					new[]{ typeof( BoneKnight ), typeof( Lich ), typeof( SkeletalKnight ) },							// Level 3
					new[]{ typeof( LichLord ), typeof( RottingCorpse ) }												// Level 4
				} ),
				new ChampionSpawnInfo( "Sleeping Dragon", typeof( Serado ), new[]{ "Rival", "Challenger", "Antagonist" } , new[]
				{																											// Unholy Terror
					new[]{ typeof( DeathwatchBeetleHatchling ), typeof( Lizardman ) },
					new[]{ typeof( DeathwatchBeetle ), typeof( Kappa ) },
					new[]{ typeof( LesserHiryu ), typeof( RevenantLion ) },
					new[]{ typeof( Hiryu ), typeof( Oni ) }
				} ),
				new ChampionSpawnInfo( "Glade", typeof( Twaulo ), new[]{ "Banisher", "Enforcer", "Eradicator" } , new[]
				{																											// Glade
					new[]{ typeof( Pixie ), typeof( ShadowWisp ) },
					new[]{ typeof( Centaur ), typeof( MLDryad ) },
					new[]{ typeof( Satyr ), typeof( CuSidhe ) },
					new[]{ typeof( FeralTreefellow ), typeof( RagingGrizzlyBear ) }
				} ),
				new ChampionSpawnInfo( "The Corrupt", typeof( Ilhenir ), new[]{ "Cleanser", "Expunger", "Depurator" } , new[]
				{																											// Unholy Terror
					new[]{ typeof( PlagueSpawn ), typeof( Bogling ) },
					new[]{ typeof( PlagueBeast ), typeof( BogThing ) },
					new[]{ typeof( PlagueBeastLord ), typeof( InterredGrizzle ) },
					new[]{ typeof( FetidEssence ), typeof( PestilentBandage ) }
				} )
			};

		public static ChampionSpawnInfo GetInfo( ChampionSpawnType type )
		{
			int v = (int)type;

			if ( v < 0 || v >= m_Table.Length )
				v = 0;

			return m_Table[v];
		}
	}
}
