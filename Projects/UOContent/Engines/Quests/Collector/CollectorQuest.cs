using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Collector
{
    public class CollectorQuest : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(DontOfferConversation),
            typeof(DeclineConversation),
            typeof(AcceptConversation),
            typeof(ElwoodDuringFishConversation),
            typeof(ReturnPearlsConversation),
            typeof(AlbertaPaintingConversation),
            typeof(AlbertaStoolConversation),
            typeof(AlbertaEndPaintingConversation),
            typeof(AlbertaAfterPaintingConversation),
            typeof(ElwoodDuringPainting1Conversation),
            typeof(ElwoodDuringPainting2Conversation),
            typeof(ReturnPaintingConversation),
            typeof(GabrielAutographConversation),
            typeof(GabrielNoSheetMusicConversation),
            typeof(NoSheetMusicConversation),
            typeof(GetSheetMusicConversation),
            typeof(GabrielSheetMusicConversation),
            typeof(GabrielIgnoreConversation),
            typeof(ElwoodDuringAutograph1Conversation),
            typeof(ElwoodDuringAutograph2Conversation),
            typeof(ElwoodDuringAutograph3Conversation),
            typeof(ReturnAutographConversation),
            typeof(TomasToysConversation),
            typeof(TomasDuringCollectingConversation),
            typeof(ReturnImagesConversation),
            typeof(ElwoodDuringToys1Conversation),
            typeof(ElwoodDuringToys2Conversation),
            typeof(ElwoodDuringToys3Conversation),
            typeof(FullEndConversation),
            typeof(FishPearlsObjective),
            typeof(ReturnPearlsObjective),
            typeof(FindAlbertaObjective),
            typeof(SitOnTheStoolObjective),
            typeof(ReturnPaintingObjective),
            typeof(FindGabrielObjective),
            typeof(FindSheetMusicObjective),
            typeof(ReturnSheetMusicObjective),
            typeof(ReturnAutographObjective),
            typeof(FindTomasObjective),
            typeof(CaptureImagesObjective),
            typeof(ReturnImagesObjective),
            typeof(ReturnToysObjective),
            typeof(MakeRoomObjective)
        };

        public CollectorQuest(PlayerMobile from) : base(from)
        {
        }

        // Serialization
        public CollectorQuest()
        {
        }

        public override Type[] TypeReferenceTable => m_TypeReferenceTable;

        public override object Name => "Collector's Quest";

        public override object OfferMessage => 1055081;

        public override TimeSpan RestartDelay => TimeSpan.Zero;
        public override bool IsTutorial => false;

        public override int Picture => 0x15A9;

        public override void Accept()
        {
            base.Accept();

            AddConversation(new AcceptConversation());
        }

        public override void Decline()
        {
            base.Decline();

            AddConversation(new DeclineConversation());
        }
    }
}
