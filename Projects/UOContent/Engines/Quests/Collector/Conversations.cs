namespace Server.Engines.Quests.Collector
{
    public class DontOfferConversation : QuestConversation
    {
        public override object Message => 1055080;

        public override bool Logged => false;
    }

    public class DeclineConversation : QuestConversation
    {
        public override object Message => 1055082;

        public override bool Logged => false;
    }

    public class AcceptConversation : QuestConversation
    {
        public override object Message => 1055083;

        public override void OnRead()
        {
            System.AddObjective(new FishPearlsObjective());
        }
    }

    public class ElwoodDuringFishConversation : QuestConversation
    {
        public override object Message => 1055089;

        public override bool Logged => false;
    }

    public class ReturnPearlsConversation : QuestConversation
    {
        public override object Message => 1055090;

        public override void OnRead()
        {
            System.AddObjective(new FindAlbertaObjective());
        }
    }

    public class AlbertaPaintingConversation : QuestConversation
    {
        public override object Message => 1055092;

        public override void OnRead()
        {
            System.AddObjective(new SitOnTheStoolObjective());
        }
    }

    public class AlbertaStoolConversation : QuestConversation
    {
        public override object Message => 1055096;

        public override bool Logged => false;
    }

    public class AlbertaEndPaintingConversation : QuestConversation
    {
        public override object Message => 1055098;

        public override void OnRead()
        {
            System.AddObjective(new ReturnPaintingObjective());
        }
    }

    public class AlbertaAfterPaintingConversation : QuestConversation
    {
        public override object Message => 1055102;

        public override bool Logged => false;
    }

    public class ElwoodDuringPainting1Conversation : QuestConversation
    {
        public override object Message => 1055094;

        public override bool Logged => false;
    }

    public class ElwoodDuringPainting2Conversation : QuestConversation
    {
        public override object Message => 1055097;

        public override bool Logged => false;
    }

    public class ReturnPaintingConversation : QuestConversation
    {
        public override object Message => 1055100;

        public override void OnRead()
        {
            System.AddObjective(new FindGabrielObjective());
        }
    }

    public class GabrielAutographConversation : QuestConversation
    {
        public override object Message => 1055103;

        public override void OnRead()
        {
            System.AddObjective(new FindSheetMusicObjective(true));
        }
    }

    public class GabrielNoSheetMusicConversation : QuestConversation
    {
        public override object Message => 1055111;

        public override bool Logged => false;
    }

    public class NoSheetMusicConversation : QuestConversation
    {
        public override object Message => 1055106;

        public override bool Logged => false;
    }

    public class GetSheetMusicConversation : QuestConversation
    {
        public override object Message => 1055109;

        public override void OnRead()
        {
            System.AddObjective(new ReturnSheetMusicObjective());
        }
    }

    public class GabrielSheetMusicConversation : QuestConversation
    {
        public override object Message => 1055113;

        public override void OnRead()
        {
            System.AddObjective(new ReturnAutographObjective());
        }
    }

    public class GabrielIgnoreConversation : QuestConversation
    {
        public override object Message => 1055118;

        public override bool Logged => false;
    }

    public class ElwoodDuringAutograph1Conversation : QuestConversation
    {
        public override object Message => 1055105;

        public override bool Logged => false;
    }

    public class ElwoodDuringAutograph2Conversation : QuestConversation
    {
        public override object Message => 1055112;

        public override bool Logged => false;
    }

    public class ElwoodDuringAutograph3Conversation : QuestConversation
    {
        public override object Message => 1055115;

        public override bool Logged => false;
    }

    public class ReturnAutographConversation : QuestConversation
    {
        public override object Message => 1055116;

        public override void OnRead()
        {
            System.AddObjective(new FindTomasObjective());
        }
    }

    public class TomasToysConversation : QuestConversation
    {
        public override object Message => 1055119;

        public override void OnRead()
        {
            System.AddObjective(new CaptureImagesObjective(true));
        }
    }

    public class TomasDuringCollectingConversation : QuestConversation
    {
        public override object Message => 1055129;

        public override bool Logged => false;
    }

    public class ReturnImagesConversation : QuestConversation
    {
        public override object Message => 1055131;

        public override void OnRead()
        {
            System.AddObjective(new ReturnToysObjective());
        }
    }

    public class ElwoodDuringToys1Conversation : QuestConversation
    {
        public override object Message => 1055123;

        public override bool Logged => false;
    }

    public class ElwoodDuringToys2Conversation : QuestConversation
    {
        public override object Message => 1055130;

        public override bool Logged => false;
    }

    public class ElwoodDuringToys3Conversation : QuestConversation
    {
        public override object Message => 1055133;

        public override bool Logged => false;
    }

    public class EndConversation : QuestConversation
    {
        public override object Message => 1055134;

        public override void OnRead()
        {
            System.Complete();
        }
    }

    public class FullEndConversation : QuestConversation
    {
        private readonly bool m_Logged;

        public FullEndConversation(bool logged) => m_Logged = logged;

        public FullEndConversation() => m_Logged = true;

        public override object Message => 1055135;

        public override bool Logged => m_Logged;

        public override void OnRead()
        {
            if (m_Logged)
            {
                System.AddObjective(new MakeRoomObjective());
            }
        }
    }
}
