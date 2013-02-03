using System;
using Server;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;

namespace Server.Engines.MLQuests.Definitions
{
	public class TownEscort : BaseEscort
	{
		// Escort reward
		private static readonly BaseReward m_Reward = new ItemReward( "Gold", typeof( Gold ), 500 );

		public TownEscort( int title, int progress, int destination, string region )
		{
			Activated = true;
			Title = title;
			Description = 1072287; // I seek a worthy escort.  I can offer some small pay to any able bodied adventurer who can assist me.  It is imperative that I reach my destination.
			RefusalMessage = 1072288; // I wish you would reconsider my offer.  I'll be waiting right here for someone brave enough to assist me.
			InProgressMessage = progress;

			Objectives.Add( new EscortObjective( new QuestArea( destination, region ) ) );

			Rewards.Add( m_Reward );
		}
	}

	public class EscortToYew : TownEscort
	{
		public EscortToYew()
			: base( 1072275, 1072289, 1072227, "Yew" )
		{
		}
	}

	public class EscortToVesper : TownEscort
	{
		public EscortToVesper()
			: base( 1072276, 1072290, 1072229, "Vesper" )
		{
		}
	}

	public class EscortToTrinsic : TownEscort
	{
		public EscortToTrinsic()
			: base( 1072277, 1072291, 1072236, "Trinsic" )
		{
		}
	}

	public class EscortToSkaraBrae : TownEscort
	{
		public EscortToSkaraBrae()
			: base( 1072278, 1072292, 1072235, "Skara Brae" )
		{
		}
	}

	public class EscortToSerpentsHold : TownEscort
	{
		public EscortToSerpentsHold()
			: base( 1072279, 1072293, 1072238, "Serpent's Hold" )
		{
		}
	}

	public class EscortToNujelm : TownEscort
	{
		public EscortToNujelm()
			: base( 1072280, 1072294, 1072237, "Nujel'm" )
		{
		}
	}

	public class EscortToMoonglow : TownEscort
	{
		public EscortToMoonglow()
			: base( 1072281, 1072295, 1072232, "Moonglow" )
		{
		}
	}

	public class EscortToMinoc : TownEscort
	{
		public EscortToMinoc()
			: base( 1072282, 1072296, 1072228, "Minoc" )
		{
		}
	}

	public class EscortToMagincia : TownEscort
	{
		public EscortToMagincia()
			: base( 1072283, 1072297, 1072233, "Magincia" )
		{
		}
	}

	public class EscortToJhelom : TownEscort
	{
		public EscortToJhelom()
			: base( 1072284, 1072298, 1072239, "Jhelom" )
		{
		}
	}

	public class EscortToCove : TownEscort
	{
		public EscortToCove()
			: base( 1072285, 1072299, 1072230, "Cove" )
		{
		}
	}

	public class EscortToBritain : TownEscort
	{
		public EscortToBritain()
			: base( 1072286, 1072300, 1072231, "Britain" )
		{
		}
	}

	public class EscortToOcllo : TownEscort
	{
		public EscortToOcllo()
			: base( 1072312, 1072313, 1072234, "Ocllo" )
		{
		}
	}
}
