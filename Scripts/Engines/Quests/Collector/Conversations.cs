namespace Server.Engines.Quests.Collector
{
	public class DontOfferConversation : QuestConversation
	{
		public override object Message => 1055080;

		public override bool Logged => false;

		public DontOfferConversation()
		{
		}
	}

	public class DeclineConversation : QuestConversation
	{
		public override object Message => 1055082;

		public override bool Logged => false;

		public DeclineConversation()
		{
		}
	}

	public class AcceptConversation : QuestConversation
	{
		public override object Message => 1055083;

		public AcceptConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FishPearlsObjective() );
		}
	}

	public class ElwoodDuringFishConversation : QuestConversation
	{
		public override object Message => 1055089;

		public override bool Logged => false;

		public ElwoodDuringFishConversation()
		{
		}
	}

	public class ReturnPearlsConversation : QuestConversation
	{
		public override object Message => 1055090;

		public ReturnPearlsConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindAlbertaObjective() );
		}
	}

	public class AlbertaPaintingConversation : QuestConversation
	{
		public override object Message => 1055092;

		public AlbertaPaintingConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new SitOnTheStoolObjective() );
		}
	}

	public class AlbertaStoolConversation : QuestConversation
	{
		public override object Message => 1055096;

		public override bool Logged => false;

		public AlbertaStoolConversation()
		{
		}
	}

	public class AlbertaEndPaintingConversation : QuestConversation
	{
		public override object Message => 1055098;

		public AlbertaEndPaintingConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new ReturnPaintingObjective() );
		}
	}

	public class AlbertaAfterPaintingConversation : QuestConversation
	{
		public override object Message => 1055102;

		public override bool Logged => false;

		public AlbertaAfterPaintingConversation()
		{
		}
	}

	public class ElwoodDuringPainting1Conversation : QuestConversation
	{
		public override object Message => 1055094;

		public override bool Logged => false;

		public ElwoodDuringPainting1Conversation()
		{
		}
	}

	public class ElwoodDuringPainting2Conversation : QuestConversation
	{
		public override object Message => 1055097;

		public override bool Logged => false;

		public ElwoodDuringPainting2Conversation()
		{
		}
	}

	public class ReturnPaintingConversation : QuestConversation
	{
		public override object Message => 1055100;

		public ReturnPaintingConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindGabrielObjective() );
		}
	}

	public class GabrielAutographConversation : QuestConversation
	{
		public override object Message => 1055103;

		public GabrielAutographConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindSheetMusicObjective( true ) );
		}
	}

	public class GabrielNoSheetMusicConversation : QuestConversation
	{
		public override object Message => 1055111;

		public override bool Logged => false;

		public GabrielNoSheetMusicConversation()
		{
		}
	}

	public class NoSheetMusicConversation : QuestConversation
	{
		public override object Message => 1055106;

		public override bool Logged => false;

		public NoSheetMusicConversation()
		{
		}
	}

	public class GetSheetMusicConversation : QuestConversation
	{
		public override object Message => 1055109;

		public GetSheetMusicConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new ReturnSheetMusicObjective() );
		}
	}

	public class GabrielSheetMusicConversation : QuestConversation
	{
		public override object Message => 1055113;

		public GabrielSheetMusicConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new ReturnAutographObjective() );
		}
	}

	public class GabrielIgnoreConversation : QuestConversation
	{
		public override object Message => 1055118;

		public override bool Logged => false;

		public GabrielIgnoreConversation()
		{
		}
	}

	public class ElwoodDuringAutograph1Conversation : QuestConversation
	{
		public override object Message => 1055105;

		public override bool Logged => false;

		public ElwoodDuringAutograph1Conversation()
		{
		}
	}

	public class ElwoodDuringAutograph2Conversation : QuestConversation
	{
		public override object Message => 1055112;

		public override bool Logged => false;

		public ElwoodDuringAutograph2Conversation()
		{
		}
	}

	public class ElwoodDuringAutograph3Conversation : QuestConversation
	{
		public override object Message => 1055115;

		public override bool Logged => false;

		public ElwoodDuringAutograph3Conversation()
		{
		}
	}

	public class ReturnAutographConversation : QuestConversation
	{
		public override object Message => 1055116;

		public ReturnAutographConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindTomasObjective() );
		}
	}

	public class TomasToysConversation : QuestConversation
	{
		public override object Message => 1055119;

		public TomasToysConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new CaptureImagesObjective( true ) );
		}
	}

	public class TomasDuringCollectingConversation : QuestConversation
	{
		public override object Message => 1055129;

		public override bool Logged => false;

		public TomasDuringCollectingConversation()
		{
		}
	}

	public class ReturnImagesConversation : QuestConversation
	{
		public override object Message => 1055131;

		public ReturnImagesConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new ReturnToysObjective() );
		}
	}

	public class ElwoodDuringToys1Conversation : QuestConversation
	{
		public override object Message => 1055123;

		public override bool Logged => false;

		public ElwoodDuringToys1Conversation()
		{
		}
	}

	public class ElwoodDuringToys2Conversation : QuestConversation
	{
		public override object Message => 1055130;

		public override bool Logged => false;

		public ElwoodDuringToys2Conversation()
		{
		}
	}

	public class ElwoodDuringToys3Conversation : QuestConversation
	{
		public override object Message => 1055133;

		public override bool Logged => false;

		public ElwoodDuringToys3Conversation()
		{
		}
	}

	public class EndConversation : QuestConversation
	{
		public override object Message => 1055134;

		public EndConversation()
		{
		}

		public override void OnRead()
		{
			System.Complete();
		}
	}

	public class FullEndConversation : QuestConversation
	{
		private bool m_Logged;

		public override object Message => 1055135;

		public override bool Logged => m_Logged;

		public FullEndConversation( bool logged )
		{
			m_Logged = logged;
		}

		public FullEndConversation()
		{
			m_Logged = true;
		}

		public override void OnRead()
		{
			if ( m_Logged )
				System.AddObjective( new MakeRoomObjective() );
		}
	}
}
