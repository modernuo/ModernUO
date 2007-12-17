using System;
using System.Collections.Generic;

namespace Server.Engines.Harvest
{
	public class HarvestDefinition
	{
		private int m_BankWidth, m_BankHeight;
		private int m_MinTotal, m_MaxTotal;
		private int[] m_Tiles;
		private bool m_RangedTiles;
		private TimeSpan m_MinRespawn, m_MaxRespawn;
		private int m_MaxRange;
		private int m_ConsumedPerHarvest, m_ConsumedPerFeluccaHarvest;
		private bool m_PlaceAtFeetIfFull;
		private SkillName m_Skill;
		private int[] m_EffectActions;
		private int[] m_EffectCounts;
		private int[] m_EffectSounds;
		private TimeSpan m_EffectSoundDelay;
		private TimeSpan m_EffectDelay;
		private object m_NoResourcesMessage, m_OutOfRangeMessage, m_TimedOutOfRangeMessage, m_DoubleHarvestMessage, m_FailMessage, m_PackFullMessage, m_ToolBrokeMessage;
		private HarvestResource[] m_Resources;
		private HarvestVein[] m_Veins;
		private BonusHarvestResource[] m_BonusResources;
		private bool m_RaceBonus;
		private bool m_RandomizeVeins;

		public int BankWidth{ get{ return m_BankWidth; } set{ m_BankWidth = value; } }
		public int BankHeight{ get{ return m_BankHeight; } set{ m_BankHeight = value; } }
		public int MinTotal{ get{ return m_MinTotal; } set{ m_MinTotal = value; } }
		public int MaxTotal{ get{ return m_MaxTotal; } set{ m_MaxTotal = value; } }
		public int[] Tiles{ get{ return m_Tiles; } set{ m_Tiles = value; } }
		public bool RangedTiles{ get{ return m_RangedTiles; } set{ m_RangedTiles = value; } }
		public TimeSpan MinRespawn{ get{ return m_MinRespawn; } set{ m_MinRespawn = value; } }
		public TimeSpan MaxRespawn{ get{ return m_MaxRespawn; } set{ m_MaxRespawn = value; } }
		public int MaxRange{ get{ return m_MaxRange; } set{ m_MaxRange = value; } }
		public int ConsumedPerHarvest{ get{ return m_ConsumedPerHarvest; } set{ m_ConsumedPerHarvest = value; } }
		public int ConsumedPerFeluccaHarvest{ get{ return m_ConsumedPerFeluccaHarvest; } set{ m_ConsumedPerFeluccaHarvest = value; } }
		public bool PlaceAtFeetIfFull{ get{ return m_PlaceAtFeetIfFull; } set{ m_PlaceAtFeetIfFull = value; } }
		public SkillName Skill{ get{ return m_Skill; } set{ m_Skill = value; } }
		public int[] EffectActions{ get{ return m_EffectActions; } set{ m_EffectActions = value; } }
		public int[] EffectCounts{ get{ return m_EffectCounts; } set{ m_EffectCounts = value; } }
		public int[] EffectSounds{ get{ return m_EffectSounds; } set{ m_EffectSounds = value; } }
		public TimeSpan EffectSoundDelay{ get{ return m_EffectSoundDelay; } set{ m_EffectSoundDelay = value; } }
		public TimeSpan EffectDelay{ get{ return m_EffectDelay; } set{ m_EffectDelay = value; } }
		public object NoResourcesMessage{ get{ return m_NoResourcesMessage; } set{ m_NoResourcesMessage = value; } }
		public object OutOfRangeMessage{ get{ return m_OutOfRangeMessage; } set{ m_OutOfRangeMessage = value; } }
		public object TimedOutOfRangeMessage{ get{ return m_TimedOutOfRangeMessage; } set{ m_TimedOutOfRangeMessage = value; } }
		public object DoubleHarvestMessage{ get{ return m_DoubleHarvestMessage; } set{ m_DoubleHarvestMessage = value; } }
		public object FailMessage{ get{ return m_FailMessage; } set{ m_FailMessage = value; } }
		public object PackFullMessage{ get{ return m_PackFullMessage; } set{ m_PackFullMessage = value; } }
		public object ToolBrokeMessage{ get{ return m_ToolBrokeMessage; } set{ m_ToolBrokeMessage = value; } }
		public HarvestResource[] Resources{ get{ return m_Resources; } set{ m_Resources = value; } }
		public HarvestVein[] Veins{ get{ return m_Veins; } set{ m_Veins = value; } }
		public BonusHarvestResource[] BonusResources{ get { return m_BonusResources; } set { m_BonusResources = value; } }
		public bool RaceBonus { get { return m_RaceBonus; } set { m_RaceBonus = value; } }
		public bool RandomizeVeins { get { return m_RandomizeVeins; } set { m_RandomizeVeins = value; } }

		private Dictionary<Map, Dictionary<Point2D, HarvestBank>> m_BanksByMap;

		public Dictionary<Map, Dictionary<Point2D, HarvestBank>> Banks{ get{ return m_BanksByMap; } set{ m_BanksByMap = value; } }

		public void SendMessageTo( Mobile from, object message )
		{
			if ( message is int )
				from.SendLocalizedMessage( (int)message );
			else if ( message is string )
				from.SendMessage( (string)message );
		}

		public HarvestBank GetBank( Map map, int x, int y )
		{
			if ( map == null || map == Map.Internal )
				return null;

			x /= m_BankWidth;
			y /= m_BankHeight;

			Dictionary<Point2D, HarvestBank> banks = null;
			m_BanksByMap.TryGetValue( map, out banks );

			if ( banks == null )
				m_BanksByMap[map] = banks = new Dictionary<Point2D, HarvestBank>();

			Point2D key = new Point2D( x, y );
			HarvestBank bank = null;
			banks.TryGetValue( key, out bank );

			if ( bank == null )
				banks[key] = bank = new HarvestBank( this, GetVeinAt( map, x, y ) );

			return bank;
		}

		public HarvestVein GetVeinAt( Map map, int x, int y )
		{
			if ( m_Veins.Length == 1 )
				return m_Veins[0];

			double randomValue;

			if ( m_RandomizeVeins )
			{
				randomValue = Utility.RandomDouble();
			}
			else
			{
				Random random = new Random( ( x * 17 ) + ( y * 11 ) + ( map.MapID * 3 ) );
				randomValue = random.NextDouble();
			}

			return GetVeinFrom( randomValue );
		}

		public HarvestVein GetVeinFrom( double randomValue )
		{
			if ( m_Veins.Length == 1 )
				return m_Veins[0];

			randomValue *= 100;

			for ( int i = 0; i < m_Veins.Length; ++i )
			{
				if ( randomValue <= m_Veins[i].VeinChance )
					return m_Veins[i];

				randomValue -= m_Veins[i].VeinChance;
			}

			return null;
		}

		public BonusHarvestResource GetBonusResource()
		{
			if ( m_BonusResources == null )
				return null;

			double randomValue = Utility.RandomDouble() * 100;

			for ( int i = 0; i < m_BonusResources.Length; ++i )
			{
				if ( randomValue <= m_BonusResources[i].Chance )
					return m_BonusResources[i];

				randomValue -= m_BonusResources[i].Chance;
			}

			return null;
		}

		public HarvestDefinition()
		{
			m_BanksByMap = new Dictionary<Map, Dictionary<Point2D, HarvestBank>>();
		}

		public bool Validate( int tileID )
		{
			if ( m_RangedTiles )
			{
				bool contains = false;

				for ( int i = 0; !contains && i < m_Tiles.Length; i += 2 )
					contains = ( tileID >= m_Tiles[i] && tileID <= m_Tiles[i + 1] );

				return contains;
			}
			else
			{
				int dist = -1;

				for ( int i = 0; dist < 0 && i < m_Tiles.Length; ++i )
					dist = ( m_Tiles[i] - tileID );

				return ( dist == 0 );
			}
		}
	}
}