using System;
using Server;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;

namespace Server.Engines.MLQuests.Definitions
{
	public class NewHavenEscort : BaseEscort
	{
		// New Haven escorts do not count for 'helping a human in need'
		public override bool AwardHumanInNeed { get { return false; } }

		// Escort reward
		private static readonly BaseReward m_Reward = new ItemReward( "Gold", typeof( Gold ), 500 );

		public NewHavenEscort( int title, int description, int progress, int destination, string region )
		{
			Activated = true;
			Title = title;
			Description = description;
			RefusalMessage = 1072288; // I wish you would reconsider my offer.  I'll be waiting right here for someone brave enough to assist me.
			InProgressMessage = progress;

			Objectives.Add( new EscortObjective( new QuestArea( destination, region ) ) );

			Rewards.Add( m_Reward );
		}
	}

	public class EscortToNHAlchemist : NewHavenEscort
	{
		public EscortToNHAlchemist()
			: base( 1072314, 1042769, 1072326, 1073864, "the New Haven Alchemist" )
		{
		}
	}

	public class EscortToNHBard : NewHavenEscort
	{
		public EscortToNHBard()
			: base( 1072315, 1042772, 1072327, 1073865, "the New Haven Bard" )
		{
		}
	}

	public class EscortToNHWarrior : NewHavenEscort
	{
		public EscortToNHWarrior()
			: base( 1072316, 1042787, 1072328, 1073866, "the New Haven Warrior" )
		{
		}
	}

	public class EscortToNHTailor : NewHavenEscort
	{
		public EscortToNHTailor()
			: base( 1072317, 1042781, 1072329, 1073867, "the New Haven Tailor" )
		{
		}
	}

	public class EscortToNHCarpenter : NewHavenEscort
	{
		public EscortToNHCarpenter()
			: base( 1072318, 1042775, 1072330, 1073868, "the New Haven Carpenter" )
		{
		}
	}

	public class EscortToNHMapmaker : NewHavenEscort
	{
		public EscortToNHMapmaker()
			: base( 1072319, 1042793, 1072331, 1073869, "the New Haven Mapmaker" )
		{
		}
	}

	public class EscortToNHMage : NewHavenEscort
	{
		public EscortToNHMage()
			: base( 1072320, 1042790, 1072332, 1073870, "the New Haven Mage" )
		{
		}
	}

	public class EscortToNHInn : NewHavenEscort
	{
		public EscortToNHInn()
			: base( 1072321, 1042796, 1072333, 1073871, "the New Haven Inn" )
		{
		}
	}

	public class EscortToNHFarm : NewHavenEscort
	{
		public EscortToNHFarm()
			: base( 1072322, 1042799, 1072334, 1073872, "the New Haven Farm" )
		{
		}
	}

	public class EscortToNHDocks : NewHavenEscort
	{
		public EscortToNHDocks()
			: base( 1072323, 1042802, 1072335, 1073873, "the New Haven Docks" )
		{
		}
	}

	public class EscortToNHBowyer : NewHavenEscort
	{
		public EscortToNHBowyer()
			: base( 1072324, 1042805, 1072336, 1073874, "the New Haven Bowyer" )
		{
		}
	}

	public class EscortToNHBank : NewHavenEscort
	{
		public EscortToNHBank()
			: base( 1072325, 1042784, 1072337, 1073875, "the New Haven Bank" )
		{
		}
	}
}
