using System;
using Server;

namespace Server.Engines.Plants
{
	public enum PlantType
	{
		CampionFlowers,
		Poppies,
		Snowdrops,
		Bulrushes,
		Lilies,
		PampasGrass,
		Rushes,
		ElephantEarPlant,
		Fern,
		PonytailPalm,
		SmallPalm,
		CenturyPlant,
		WaterPlant,
		SnakePlant,
		PricklyPearCactus,
		BarrelCactus,
		TribarrelCactus,
		CommonGreenBonsai,
		CommonPinkBonsai,
		UncommonGreenBonsai,
		UncommonPinkBonsai,
		RareGreenBonsai,
		RarePinkBonsai,
		ExceptionalBonsai,
		ExoticBonsai
	}

	public class PlantTypeInfo
	{
		private static PlantTypeInfo[] m_Table = new PlantTypeInfo[]
			{
				new PlantTypeInfo( 0xC83, 0, 0,		PlantType.CampionFlowers,		false, true, true ),
				new PlantTypeInfo( 0xC86, 0, 0,		PlantType.Poppies,				false, true, true ),
				new PlantTypeInfo( 0xC88, 0, 10,	PlantType.Snowdrops,			false, true, true ),
				new PlantTypeInfo( 0xC94, -15, 0,	PlantType.Bulrushes,			false, true, true ),
				new PlantTypeInfo( 0xC8B, 0, 0,		PlantType.Lilies,				false, true, true ),
				new PlantTypeInfo( 0xCA5, -8, 0,	PlantType.PampasGrass,			false, true, true ),
				new PlantTypeInfo( 0xCA7, -10, 0,	PlantType.Rushes,				false, true, true ),
				new PlantTypeInfo( 0xC97, -20, 0,	PlantType.ElephantEarPlant,		true, false, true ),
				new PlantTypeInfo( 0xC9F, -20, 0,	PlantType.Fern,					false, false, true ),
				new PlantTypeInfo( 0xCA6, -16, -5,	PlantType.PonytailPalm,			false, false, true ),
				new PlantTypeInfo( 0xC9C, -5, -10,	PlantType.SmallPalm,			false, false, true ),
				new PlantTypeInfo( 0xD31, 0, -27,	PlantType.CenturyPlant,			true, false, true ),
				new PlantTypeInfo( 0xD04, 0, 10,	PlantType.WaterPlant,			true, false, true ),
				new PlantTypeInfo( 0xCA9, 0, 0,		PlantType.SnakePlant,			true, false, true ),
				new PlantTypeInfo( 0xD2C, 0, 10,	PlantType.PricklyPearCactus,	false, false, true ),
				new PlantTypeInfo( 0xD26, 0, 10,	PlantType.BarrelCactus,			false, false, true ),
				new PlantTypeInfo( 0xD27, 0, 10,	PlantType.TribarrelCactus,		false, false, true ),
				new PlantTypeInfo( 0x28DC, -5, 5,	PlantType.CommonGreenBonsai,	true, false, false ),
				new PlantTypeInfo( 0x28DF, -5, 5,	PlantType.CommonPinkBonsai,		true, false, false ),
				new PlantTypeInfo( 0x28DD, -5, 5,	PlantType.UncommonGreenBonsai,	true, false, false ),
				new PlantTypeInfo( 0x28E0, -5, 5,	PlantType.UncommonPinkBonsai,	true, false, false ),
				new PlantTypeInfo( 0x28DE, -5, 5,	PlantType.RareGreenBonsai,		true, false, false ),
				new PlantTypeInfo( 0x28E1, -5, 5,	PlantType.RarePinkBonsai,		true, false, false ),
				new PlantTypeInfo( 0x28E2, -5, 5,	PlantType.ExceptionalBonsai,	true, false, false ),
				new PlantTypeInfo( 0x28E3, -5, 5,	PlantType.ExoticBonsai,			true, false, false )
			};

		public static PlantTypeInfo GetInfo( PlantType plantType )
		{
			int index = (int)plantType;

			if ( index >= 0 && index < m_Table.Length )
				return m_Table[index];
			else
				return m_Table[0];
		}

		public static PlantType RandomFirstGeneration()
		{
			switch ( Utility.Random( 3 ) )
			{
				case 0: return PlantType.CampionFlowers;
				case 1: return PlantType.Fern;
				default: return PlantType.TribarrelCactus;
			}
		}

		public static PlantType RandomBonsai( double increaseRatio )
		{
			/* Chances of each plant type are equal to the chances of the previous plant type * increaseRatio:
			 * E.g.:
			 *  chances_of_uncommon = chances_of_common * increaseRatio
			 *  chances_of_rare = chances_of_uncommon * increaseRatio
			 *  ...
			 * 
			 * If increaseRatio < 1 -> rare plants are actually rarer than the others
			 * If increaseRatio > 1 -> rare plants are actually more common than the others (it might be the case with certain monsters)
			 * 
			 * If a plant type (common, uncommon, ...) has 2 different colors, they have the same chances:
			 *  chances_of_green_common = chances_of_pink_common = chances_of_common / 2
			 *  ...
			 */

			double k1 = increaseRatio >= 0.0 ? increaseRatio : 0.0;
			double k2 = k1 * k1;
			double k3 = k2 * k1;
			double k4 = k3 * k1;

			double exp1 = k1 + 1.0;
			double exp2 = k2 + exp1;
			double exp3 = k3 + exp2;
			double exp4 = k4 + exp3;

			double rand = Utility.RandomDouble();

			if ( rand < 0.5 / exp4 )
				return PlantType.CommonGreenBonsai;
			else if ( rand < 1.0 / exp4 )
				return PlantType.CommonPinkBonsai;
			else if ( rand < (k1 * 0.5 + 1.0) / exp4 )
				return PlantType.UncommonGreenBonsai;
			else if ( rand < exp1 / exp4 )
				return PlantType.UncommonPinkBonsai;
			else if ( rand < (k2 * 0.5 + exp1) / exp4 )
				return PlantType.RareGreenBonsai;
			else if ( rand < exp2 / exp4 )
				return PlantType.RarePinkBonsai;
			else if ( rand < exp3 / exp4 )
				return PlantType.ExceptionalBonsai;
			else
				return PlantType.ExoticBonsai;
		}

		public static bool IsCrossable( PlantType plantType )
		{
			return GetInfo( plantType ).Crossable;
		}

		public static PlantType Cross( PlantType first, PlantType second )
		{
			if ( !IsCrossable( first ) || !IsCrossable( second ) )
				return PlantType.CampionFlowers;

			int firstIndex = (int)first;
			int secondIndex = (int)second;

			if ( firstIndex + 1 == secondIndex || firstIndex == secondIndex + 1 )
				return Utility.RandomBool() ? first : second;
			else
				return (PlantType)( (firstIndex + secondIndex) / 2 );
		}

		public static int GetBonsaiTitle( PlantType plantType )
		{
			switch ( plantType )
			{
				case PlantType.CommonGreenBonsai:
				case PlantType.CommonPinkBonsai:
					return 1063335; // common

				case PlantType.UncommonGreenBonsai:
				case PlantType.UncommonPinkBonsai:
					return 1063336; // uncommon

				case PlantType.RareGreenBonsai:
				case PlantType.RarePinkBonsai:
					return 1063337; // rare

				case PlantType.ExceptionalBonsai:
					return 1063341; // exceptional

				case PlantType.ExoticBonsai:
					return 1063342; // exotic

				default:
					return 0;
			}
		}

		private int m_ItemID;
		private int m_OffsetX;
		private int m_OffsetY;
		private PlantType m_PlantType;
		private bool m_ContainsPlant;
		private bool m_Flowery;
		private bool m_Crossable;

		public int ItemID { get { return m_ItemID; } }
		public int OffsetX { get { return m_OffsetX; } }
		public int OffsetY { get { return m_OffsetY; } }
		public PlantType PlantType { get { return m_PlantType; } }
		public int Name { get { return 1020000 + m_ItemID; } }
		public bool ContainsPlant { get { return m_ContainsPlant; } }
		public bool Flowery { get { return m_Flowery; } }
		public bool Crossable { get { return m_Crossable; } }

		private PlantTypeInfo( int itemID, int offsetX, int offsetY, PlantType plantType, bool containsPlant, bool flowery, bool crossable )
		{
			m_ItemID = itemID;
			m_OffsetX = offsetX;
			m_OffsetY = offsetY;
			m_PlantType = plantType;
			m_ContainsPlant = containsPlant;
			m_Flowery = flowery;
			m_Crossable = crossable;
		}
	}
}