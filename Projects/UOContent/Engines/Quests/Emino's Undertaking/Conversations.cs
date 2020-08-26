namespace Server.Engines.Quests.Ninja
{
    public class AcceptConversation : QuestConversation
    {
        public override object Message => 1049092;

        public override void OnRead()
        {
            System.AddObjective(new FindEminoBeginObjective());
        }
    }

    public class FindZoelConversation : QuestConversation
    {
        public override object Message => 1063175;

        public override void OnRead()
        {
            System.AddObjective(new FindZoelObjective());
        }
    }

    public class RadarConversation : QuestConversation
    {
        public override object Message => 1063033;

        public override bool Logged => false;
    }

    public class EnterCaveConversation : QuestConversation
    {
        public override object Message => 1063177;

        public override void OnRead()
        {
            System.AddObjective(new EnterCaveObjective());
        }
    }

    public class SneakPastGuardiansConversation : QuestConversation
    {
        public override object Message => 1063180;

        public override void OnRead()
        {
            System.AddObjective(new SneakPastGuardiansObjective());
        }
    }

    public class NeedToHideConversation : QuestConversation
    {
        public override object Message => 1063181;
    }

    public class UseTeleporterConversation : QuestConversation
    {
        public override object Message => 1063182;

        public override void OnRead()
        {
            System.AddObjective(new UseTeleporterObjective());
        }
    }

    public class GiveZoelNoteConversation : QuestConversation
    {
        public override object Message => 1063184;

        public override void OnRead()
        {
            System.AddObjective(new GiveZoelNoteObjective());
        }
    }

    public class LostNoteConversation : QuestConversation
    {
        public override object Message => 1063187;

        public override bool Logged => false;
    }

    public class GainInnInformationConversation : QuestConversation
    {
        public override object Message => 1063189;

        public override void OnRead()
        {
            System.AddObjective(new GainInnInformationObjective());
        }
    }

    public class ReturnFromInnConversation : QuestConversation
    {
        public override object Message => 1063196;

        public override void OnRead()
        {
            System.AddObjective(new ReturnFromInnObjective());
        }
    }

    public class SearchForSwordConversation : QuestConversation
    {
        public override object Message => 1063199;

        public override void OnRead()
        {
            System.AddObjective(new SearchForSwordObjective());
        }
    }

    public class HallwayWalkConversation : QuestConversation
    {
        public override object Message => 1063201;

        public override void OnRead()
        {
            System.AddObjective(new HallwayWalkObjective());
        }
    }

    public class ReturnSwordConversation : QuestConversation
    {
        public override object Message => 1063203;

        public override void OnRead()
        {
            System.AddObjective(new ReturnSwordObjective());
        }
    }

    public class SlayHenchmenConversation : QuestConversation
    {
        public override object Message => 1063205;

        public override void OnRead()
        {
            System.AddObjective(new SlayHenchmenObjective());
        }
    }

    public class ContinueSlayHenchmenConversation : QuestConversation
    {
        public override object Message => 1063208;

        public override bool Logged => false;
    }

    public class GiveEminoSwordConversation : QuestConversation
    {
        public override object Message => 1063211;

        public override void OnRead()
        {
            System.AddObjective(new GiveEminoSwordObjective());
        }
    }

    public class LostSwordConversation : QuestConversation
    {
        public override object Message => 1063212;

        public override bool Logged => false;
    }

    public class EarnGiftsConversation : QuestConversation
    {
        public override object Message => 1063216;

        public override void OnRead()
        {
            System.Complete();
        }
    }

    public class EarnLessGiftsConversation : QuestConversation
    {
        public override object Message => 1063217;

        public override void OnRead()
        {
            System.Complete();
        }
    }
}
