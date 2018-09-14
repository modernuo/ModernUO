namespace Server.Engines.Quests.Ninja
{
	public class AcceptConversation : QuestConversation
	{
		public override object Message => 1049092;

		public AcceptConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindEminoBeginObjective() );
		}
	}

	public class FindZoelConversation : QuestConversation
	{
		public override object Message => 1063175;

		public FindZoelConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindZoelObjective() );
		}
	}

	public class RadarConversation : QuestConversation
	{
		public override object Message => 1063033;

		public override bool Logged => false;

		public RadarConversation()
		{
		}
	}

	public class EnterCaveConversation : QuestConversation
	{
		public override object Message => 1063177;

		public EnterCaveConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new EnterCaveObjective() );
		}
	}

	public class SneakPastGuardiansConversation : QuestConversation
	{
		public override object Message => 1063180;

		public SneakPastGuardiansConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new SneakPastGuardiansObjective() );
		}
	}

	public class NeedToHideConversation : QuestConversation
	{
		public override object Message => 1063181;

		public NeedToHideConversation()
		{
		}
	}

	public class UseTeleporterConversation : QuestConversation
	{
		public override object Message => 1063182;

		public UseTeleporterConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new UseTeleporterObjective() );
		}
	}

	public class GiveZoelNoteConversation : QuestConversation
	{
		public override object Message => 1063184;

		public GiveZoelNoteConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new GiveZoelNoteObjective() );
		}
	}

	public class LostNoteConversation : QuestConversation
	{
		public override object Message => 1063187;

		public override bool Logged => false;

		public LostNoteConversation()
		{
		}
	}

	public class GainInnInformationConversation : QuestConversation
	{
		public override object Message => 1063189;

		public GainInnInformationConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new GainInnInformationObjective() );
		}
	}

	public class ReturnFromInnConversation : QuestConversation
	{
		public override object Message => 1063196;

		public ReturnFromInnConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new ReturnFromInnObjective() );
		}
	}

	public class SearchForSwordConversation : QuestConversation
	{
		public override object Message => 1063199;

		public SearchForSwordConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new SearchForSwordObjective() );
		}
	}

	public class HallwayWalkConversation : QuestConversation
	{
		public override object Message => 1063201;

		public HallwayWalkConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new HallwayWalkObjective() );
		}
	}

	public class ReturnSwordConversation : QuestConversation
	{
		public override object Message => 1063203;

		public ReturnSwordConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new ReturnSwordObjective() );
		}
	}

	public class SlayHenchmenConversation : QuestConversation
	{
		public override object Message => 1063205;

		public SlayHenchmenConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new SlayHenchmenObjective() );
		}
	}

	public class ContinueSlayHenchmenConversation : QuestConversation
	{
		public override object Message => 1063208;

		public override bool Logged => false;

		public ContinueSlayHenchmenConversation()
		{
		}
	}

	public class GiveEminoSwordConversation : QuestConversation
	{
		public override object Message => 1063211;

		public GiveEminoSwordConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new GiveEminoSwordObjective() );
		}
	}

	public class LostSwordConversation : QuestConversation
	{
		public override object Message => 1063212;

		public override bool Logged => false;

		public LostSwordConversation()
		{
		}
	}

	public class EarnGiftsConversation : QuestConversation
	{
		public override object Message => 1063216;

		public EarnGiftsConversation()
		{
		}

		public override void OnRead()
		{
			System.Complete();
		}
	}

	public class EarnLessGiftsConversation : QuestConversation
	{
		public override object Message => 1063217;

		public EarnLessGiftsConversation()
		{
		}

		public override void OnRead()
		{
			System.Complete();
		}
	}
}
