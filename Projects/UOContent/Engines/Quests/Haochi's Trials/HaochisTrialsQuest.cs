using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
    public class HaochisTrialsQuest : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(AcceptConversation),
            typeof(RadarConversation),
            typeof(FirstTrialIntroConversation),
            typeof(FirstTrialKillConversation),
            typeof(GainKarmaConversation),
            typeof(SecondTrialIntroConversation),
            typeof(SecondTrialAttackConversation),
            typeof(ThirdTrialIntroConversation),
            typeof(ThirdTrialKillConversation),
            typeof(FourthTrialIntroConversation),
            typeof(FourthTrialCatsConversation),
            typeof(FifthTrialIntroConversation),
            typeof(FifthTrialReturnConversation),
            typeof(LostSwordConversation),
            typeof(SixthTrialIntroConversation),
            typeof(SeventhTrialIntroConversation),
            typeof(EndConversation),
            typeof(FindHaochiObjective),
            typeof(FirstTrialIntroObjective),
            typeof(FirstTrialKillObjective),
            typeof(FirstTrialReturnObjective),
            typeof(SecondTrialIntroObjective),
            typeof(SecondTrialAttackObjective),
            typeof(SecondTrialReturnObjective),
            typeof(ThirdTrialIntroObjective),
            typeof(ThirdTrialKillObjective),
            typeof(ThirdTrialReturnObjective),
            typeof(FourthTrialIntroObjective),
            typeof(FourthTrialCatsObjective),
            typeof(FourthTrialReturnObjective),
            typeof(FifthTrialIntroObjective),
            typeof(FifthTrialReturnObjective),
            typeof(SixthTrialIntroObjective),
            typeof(SixthTrialReturnObjective),
            typeof(SeventhTrialIntroObjective),
            typeof(SeventhTrialReturnObjective)
        };

        private bool m_SentRadarConversion;

        public HaochisTrialsQuest(PlayerMobile from) : base(from)
        {
        }

        // Serialization
        public HaochisTrialsQuest()
        {
        }

        public override Type[] TypeReferenceTable => m_TypeReferenceTable;

        public override object Name => 1063022;

        public override object OfferMessage => 1063023;

        public override TimeSpan RestartDelay => TimeSpan.MaxValue;
        public override bool IsTutorial => true;

        public override int Picture => 0x15D7;

        public override void Accept()
        {
            base.Accept();

            AddConversation(new AcceptConversation());
        }

        public override void Slice()
        {
            if (!m_SentRadarConversion &&
                (From.Map != Map.Malas || From.X < 360 || From.X > 400 || From.Y < 760 || From.Y > 780))
            {
                m_SentRadarConversion = true;
                AddConversation(new RadarConversation());
            }

            base.Slice();
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_SentRadarConversion = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_SentRadarConversion);
        }

        public static bool HasLostHaochisKatana(Mobile from)
        {
            if (!(from is PlayerMobile pm))
            {
                return false;
            }

            var qs = pm.Quest;

            if (qs is HaochisTrialsQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(FifthTrialReturnObjective)))
                {
                    return from.Backpack?.FindItemByType<HaochisKatana>() == null;
                }
            }

            return false;
        }
    }
}
