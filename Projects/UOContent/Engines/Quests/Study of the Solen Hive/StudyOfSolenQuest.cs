using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Naturalist
{
    public class StudyOfSolenQuest : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(StudyNestsObjective),
            typeof(ReturnToNaturalistObjective),
            typeof(DontOfferConversation),
            typeof(AcceptConversation),
            typeof(NaturalistDuringStudyConversation),
            typeof(EndConversation),
            typeof(SpecialEndConversation),
            typeof(FullBackpackConversation)
        };

        public StudyOfSolenQuest(PlayerMobile from, Naturalist naturalist) : base(from) => Naturalist = naturalist;

        // Serialization
        public StudyOfSolenQuest()
        {
        }

        public override Type[] TypeReferenceTable => m_TypeReferenceTable;

        public override object Name => 1054041;

        public override object OfferMessage => 1054042;

        public override TimeSpan RestartDelay => TimeSpan.Zero;
        public override bool IsTutorial => false;

        public override int Picture => 0x15C7;

        public Naturalist Naturalist { get; private set; }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            Naturalist = (Naturalist)reader.ReadEntity<Mobile>();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(Naturalist);
        }

        public override void Accept()
        {
            base.Accept();

            Naturalist?.PlaySound(0x431);

            AddConversation(new AcceptConversation());
        }
    }
}
