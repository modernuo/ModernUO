using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Collector
{
	public class CollectorQuest : QuestSystem
	{
		private static Type[] m_TypeReferenceTable = new Type[]
			{
				typeof( Collector.DontOfferConversation ),
				typeof( Collector.DeclineConversation ),
				typeof( Collector.AcceptConversation ),
				typeof( Collector.ElwoodDuringFishConversation ),
				typeof( Collector.ReturnPearlsConversation ),
				typeof( Collector.AlbertaPaintingConversation ),
				typeof( Collector.AlbertaStoolConversation ),
				typeof( Collector.AlbertaEndPaintingConversation ),
				typeof( Collector.AlbertaAfterPaintingConversation ),
				typeof( Collector.ElwoodDuringPainting1Conversation ),
				typeof( Collector.ElwoodDuringPainting2Conversation ),
				typeof( Collector.ReturnPaintingConversation ),
				typeof( Collector.GabrielAutographConversation ),
				typeof( Collector.GabrielNoSheetMusicConversation ),
				typeof( Collector.NoSheetMusicConversation ),
				typeof( Collector.GetSheetMusicConversation ),
				typeof( Collector.GabrielSheetMusicConversation ),
				typeof( Collector.GabrielIgnoreConversation ),
				typeof( Collector.ElwoodDuringAutograph1Conversation ),
				typeof( Collector.ElwoodDuringAutograph2Conversation ),
				typeof( Collector.ElwoodDuringAutograph3Conversation ),
				typeof( Collector.ReturnAutographConversation ),
				typeof( Collector.TomasToysConversation ),
				typeof( Collector.TomasDuringCollectingConversation ),
				typeof( Collector.ReturnImagesConversation ),
				typeof( Collector.ElwoodDuringToys1Conversation ),
				typeof( Collector.ElwoodDuringToys2Conversation ),
				typeof( Collector.ElwoodDuringToys3Conversation ),
				typeof( Collector.FullEndConversation ),
				typeof( Collector.FishPearlsObjective ),
				typeof( Collector.ReturnPearlsObjective ),
				typeof( Collector.FindAlbertaObjective ),
				typeof( Collector.SitOnTheStoolObjective ),
				typeof( Collector.ReturnPaintingObjective ),
				typeof( Collector.FindGabrielObjective ),
				typeof( Collector.FindSheetMusicObjective ),
				typeof( Collector.ReturnSheetMusicObjective ),
				typeof( Collector.ReturnAutographObjective ),
				typeof( Collector.FindTomasObjective ),
				typeof( Collector.CaptureImagesObjective ),
				typeof( Collector.ReturnImagesObjective ),
				typeof( Collector.ReturnToysObjective ),
				typeof( Collector.MakeRoomObjective )
			};

		public override Type[] TypeReferenceTable => m_TypeReferenceTable;

		public override object Name => "Collector's Quest";

		public override object OfferMessage => 1055081;

		public override TimeSpan RestartDelay => TimeSpan.Zero;
		public override bool IsTutorial => false;

		public override int Picture => 0x15A9;

		public CollectorQuest( PlayerMobile from ) : base( from )
		{
		}

		// Serialization
		public CollectorQuest()
		{
		}

		public override void Accept()
		{
			base.Accept();

			AddConversation( new AcceptConversation() );
		}

		public override void Decline()
		{
			base.Decline();

			AddConversation( new DeclineConversation() );
		}
	}
}
