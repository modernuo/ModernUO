namespace Server.Engines.Quests.Naturalist
{
	public class DontOfferConversation : QuestConversation
	{
		public override object Message => 1054052;

		public override bool Logged => false;

		public DontOfferConversation()
		{
		}
	}

	public class AcceptConversation : QuestConversation
	{
		public override object Message => 1054043;

		public AcceptConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new StudyNestsObjective() );
		}
	}

	public class NaturalistDuringStudyConversation : QuestConversation
	{
		public override object Message => 1054049;

		public override bool Logged => false;

		public NaturalistDuringStudyConversation()
		{
		}
	}

	public class EndConversation : QuestConversation
	{
		public override object Message => 1054050;

		public EndConversation()
		{
		}

		public override void OnRead()
		{
			System.Complete();
		}
	}

	public class SpecialEndConversation : QuestConversation
	{
		public override object Message => 1054051;

		public SpecialEndConversation()
		{
		}

		public override void OnRead()
		{
			System.Complete();
		}
	}

	public class FullBackpackConversation : QuestConversation
	{
		public override object Message => 1054053;

		public override bool Logged => false;

		public FullBackpackConversation()
		{
		}
	}
}
