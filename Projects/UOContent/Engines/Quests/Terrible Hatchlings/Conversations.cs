namespace Server.Engines.Quests.Zento
{
    public class AcceptConversation : QuestConversation
    {
        public override object Message => 1049092;

        public override void OnRead()
        {
            System.AddObjective(new FirstKillObjective());
        }
    }

    public class DirectionConversation : QuestConversation
    {
        public override object Message => 1063323;

        public override bool Logged => false;
    }

    public class TakeCareConversation : QuestConversation
    {
        public override object Message => 1063324;

        public override bool Logged => false;
    }

    public class EndConversation : QuestConversation
    {
        public override object Message => 1063321;

        public override void OnRead()
        {
            System.Complete();
        }
    }
}
