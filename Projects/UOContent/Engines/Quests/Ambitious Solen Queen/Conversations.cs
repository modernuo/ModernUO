namespace Server.Engines.Quests.Ambitious
{
    public class DontOfferConversation : QuestConversation
    {
        public override object Message => 1054059;

        public override bool Logged => false;
    }

    public class AcceptConversation : QuestConversation
    {
        public override object Message => 1054061;

        public override void OnRead()
        {
            System.AddObjective(new KillQueensObjective());
        }
    }

    public class DuringKillQueensConversation : QuestConversation
    {
        public override object Message => 1054066;

        public override bool Logged => false;
    }

    public class GatherFungiConversation : QuestConversation
    {
        public override object Message => 1054068;

        public override void OnRead()
        {
            System.AddObjective(new GatherFungiObjective());
        }
    }

    public class DuringFungiGatheringConversation : QuestConversation
    {
        public override object Message => 1054070;

        public override bool Logged => false;
    }

    public class EndConversation : QuestConversation
    {
        public override object Message => 1054073;

        public override void OnRead()
        {
            var bagOfSending = true;
            var powderOfTranslocation = true;
            var gold = true;

            AmbitiousQueenQuest.GiveRewardTo(System.From, ref bagOfSending, ref powderOfTranslocation, ref gold);

            if (!bagOfSending && !powderOfTranslocation && !gold)
            {
                System.Complete();
            }
            else
            {
                System.AddConversation(new FullBackpackConversation(true, bagOfSending, powderOfTranslocation, gold));
            }
        }
    }

    public class FullBackpackConversation : QuestConversation
    {
        private readonly bool m_Logged;
        private bool m_BagOfSending;
        private bool m_Gold;
        private bool m_PowderOfTranslocation;

        public FullBackpackConversation(bool logged, bool bagOfSending, bool powderOfTranslocation, bool gold)
        {
            m_Logged = logged;

            m_BagOfSending = bagOfSending;
            m_PowderOfTranslocation = powderOfTranslocation;
            m_Gold = gold;
        }

        public FullBackpackConversation() => m_Logged = true;

        public override object Message => 1054077;

        public override bool Logged => m_Logged;

        public override void OnRead()
        {
            if (m_Logged)
            {
                System.AddObjective(new GetRewardObjective(m_BagOfSending, m_PowderOfTranslocation, m_Gold));
            }
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_BagOfSending = reader.ReadBool();
            m_PowderOfTranslocation = reader.ReadBool();
            m_Gold = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_BagOfSending);
            writer.Write(m_PowderOfTranslocation);
            writer.Write(m_Gold);
        }
    }

    public class End2Conversation : QuestConversation
    {
        public override object Message => 1054078;

        public override void OnRead()
        {
            System.Complete();
        }
    }
}
