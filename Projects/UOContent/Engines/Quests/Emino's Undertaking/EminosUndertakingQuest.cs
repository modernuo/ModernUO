using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
    public class EminosUndertakingQuest : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(AcceptConversation),
            typeof(FindZoelConversation),
            typeof(RadarConversation),
            typeof(EnterCaveConversation),
            typeof(SneakPastGuardiansConversation),
            typeof(NeedToHideConversation),
            typeof(UseTeleporterConversation),
            typeof(GiveZoelNoteConversation),
            typeof(LostNoteConversation),
            typeof(GainInnInformationConversation),
            typeof(ReturnFromInnConversation),
            typeof(SearchForSwordConversation),
            typeof(HallwayWalkConversation),
            typeof(ReturnSwordConversation),
            typeof(SlayHenchmenConversation),
            typeof(ContinueSlayHenchmenConversation),
            typeof(GiveEminoSwordConversation),
            typeof(LostSwordConversation),
            typeof(EarnGiftsConversation),
            typeof(EarnLessGiftsConversation),
            typeof(FindEminoBeginObjective),
            typeof(FindZoelObjective),
            typeof(EnterCaveObjective),
            typeof(SneakPastGuardiansObjective),
            typeof(UseTeleporterObjective),
            typeof(GiveZoelNoteObjective),
            typeof(GainInnInformationObjective),
            typeof(ReturnFromInnObjective),
            typeof(SearchForSwordObjective),
            typeof(HallwayWalkObjective),
            typeof(ReturnSwordObjective),
            typeof(SlayHenchmenObjective),
            typeof(GiveEminoSwordObjective)
        };

        private bool m_SentRadarConversion;

        public EminosUndertakingQuest(PlayerMobile from) : base(from)
        {
        }

        // Serialization
        public EminosUndertakingQuest()
        {
        }

        public override Type[] TypeReferenceTable => m_TypeReferenceTable;

        public override object Name => 1063173;

        public override object OfferMessage => 1063174;

        public override TimeSpan RestartDelay => TimeSpan.MaxValue;
        public override bool IsTutorial => true;

        public override int Picture => 0x15D5;

        public override void Accept()
        {
            base.Accept();

            AddConversation(new AcceptConversation());
        }

        public override void Slice()
        {
            if (!m_SentRadarConversion &&
                (From.Map != Map.Malas || From.X < 407 || From.X > 431 || From.Y < 801 || From.Y > 830))
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

        public static bool HasLostNoteForZoel(Mobile from)
        {
            if (!(from is PlayerMobile pm))
            {
                return false;
            }

            var qs = pm.Quest;

            if (qs is EminosUndertakingQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(GiveZoelNoteObjective)))
                {
                    return from.Backpack?.FindItemByType<NoteForZoel>() == null;
                }
            }

            return false;
        }

        public static bool HasLostEminosKatana(Mobile from)
        {
            if (!(from is PlayerMobile pm))
            {
                return false;
            }

            var qs = pm.Quest;

            if (qs is EminosUndertakingQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(GiveEminoSwordObjective)))
                {
                    return from.Backpack?.FindItemByType<EminosKatana>() == null;
                }
            }

            return false;
        }
    }
}
