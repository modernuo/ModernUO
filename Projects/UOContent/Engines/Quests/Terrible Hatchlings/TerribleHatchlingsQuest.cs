using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Zento
{
    public class TerribleHatchlingsQuest : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(AcceptConversation),
            typeof(DirectionConversation),
            typeof(TakeCareConversation),
            typeof(EndConversation),
            typeof(FirstKillObjective),
            typeof(SecondKillObjective),
            typeof(ThirdKillObjective),
            typeof(ReturnObjective)
        };

        public TerribleHatchlingsQuest(PlayerMobile from) : base(from)
        {
        }

        // Serialization
        public TerribleHatchlingsQuest()
        {
        }

        public override Type[] TypeReferenceTable => m_TypeReferenceTable;

        public override object Name => 1063314;

        public override object OfferMessage => 1063315;

        public override TimeSpan RestartDelay => TimeSpan.MaxValue;
        public override bool IsTutorial => true;

        public override int Picture => 0x15CF;

        public override void Accept()
        {
            base.Accept();

            AddConversation(new AcceptConversation());
        }
    }
}
