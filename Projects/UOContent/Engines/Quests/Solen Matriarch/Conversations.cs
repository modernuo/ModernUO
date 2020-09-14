namespace Server.Engines.Quests.Matriarch
{
    public class DontOfferConversation : QuestConversation
    {
        private bool m_Friend;

        public DontOfferConversation(bool friend) => m_Friend = friend;

        public DontOfferConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_Friend)
                {
                    return 1054081;
                }

                /* <I>The Solen Matriarch smiles as she eats the seed you offered.</I><BR><BR>
                   *
                   * Thank you for that seed. It was quite delicious.  <BR><BR>
                   *
                   * I would offer to make you a friend of my colony, but you seem to be busy with
                   * another task at the moment. Perhaps you should finish whatever is occupying
                   * your attention at the moment and return to me once you're done.
                   */
                return 1054079;
            }
        }

        public override bool Logged => false;

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_Friend = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_Friend);
        }
    }

    public class AcceptConversation : QuestConversation
    {
        public override object Message => 1054084;

        public override void OnRead()
        {
            System.AddObjective(new KillInfiltratorsObjective());
        }
    }

    public class DuringKillInfiltratorsConversation : QuestConversation
    {
        public override object Message => 1054089;

        public override bool Logged => false;
    }

    public class GatherWaterConversation : QuestConversation
    {
        public override object Message => 1054091;

        public override void OnRead()
        {
            System.AddObjective(new GatherWaterObjective());
        }
    }

    public class DuringWaterGatheringConversation : QuestConversation
    {
        public override object Message => 1054094;

        public override bool Logged => false;
    }

    public class ProcessFungiConversation : QuestConversation
    {
        private bool m_Friend;

        public ProcessFungiConversation(bool friend) => m_Friend = friend;

        public ProcessFungiConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_Friend)
                {
                    return 1054097;
                }

                /* <I>The Solen Matriarch listens as you report the completion of your
                   * tasks to her.</I><BR><BR>
                   *
                   * I give you my thanks for your help, and I will gladly make you a friend of my
                   * solen colony. My warriors, workers, and queens will not longer look at you
                   * as an intruder and attack you when you enter our lair.<BR><BR>
                   *
                   * I will also process some zoogi fungus into powder of translocation for you.
                   * Two of the zoogi fungi are required for each measure of the powder. I will
                   * process up to 200 zoogi fungi into 100 measures of powder of translocation.<BR><BR>
                   *
                   * I will also give you some gold for assisting me and my colony, but first let's
                   * take care of your zoogi fungus.
                   */
                return 1054096;
            }
        }

        public override void OnRead()
        {
            System.AddObjective(new ProcessFungiObjective());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_Friend = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_Friend);
        }
    }

    public class DuringFungiProcessConversation : QuestConversation
    {
        public override object Message => 1054099;

        public override bool Logged => false;
    }

    public class FullBackpackConversation : QuestConversation
    {
        private readonly bool m_Logged;

        public FullBackpackConversation(bool logged) => m_Logged = logged;

        public FullBackpackConversation() => m_Logged = true;

        public override object Message => 1054102;

        public override bool Logged => m_Logged;

        public override void OnRead()
        {
            if (m_Logged)
            {
                System.AddObjective(new GetRewardObjective());
            }
        }
    }

    public class EndConversation : QuestConversation
    {
        public override object Message => 1054101;

        public override void OnRead()
        {
            System.Complete();
        }
    }
}
