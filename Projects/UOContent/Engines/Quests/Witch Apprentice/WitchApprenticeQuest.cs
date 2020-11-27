using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Hag
{
    public class WitchApprenticeQuest : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(FindApprenticeObjective),
            typeof(FindGrizeldaAboutMurderObjective),
            typeof(KillImpsObjective),
            typeof(FindZeefzorpulObjective),
            typeof(ReturnRecipeObjective),
            typeof(FindIngredientObjective),
            typeof(ReturnIngredientsObjective),
            typeof(DontOfferConversation),
            typeof(AcceptConversation),
            typeof(HagDuringCorpseSearchConversation),
            typeof(ApprenticeCorpseConversation),
            typeof(MurderConversation),
            typeof(HagDuringImpSearchConversation),
            typeof(ImpDeathConversation),
            typeof(ZeefzorpulConversation),
            typeof(RecipeConversation),
            typeof(HagDuringIngredientsConversation),
            typeof(BlackheartFirstConversation),
            typeof(BlackheartNoPirateConversation),
            typeof(BlackheartPirateConversation),
            typeof(EndConversation),
            typeof(RecentlyFinishedConversation)
        };

        private static readonly Point3D[] m_ZeefzorpulLocations =
        {
            new(1226, 1573, 0),
            new(1929, 1148, 0),
            new(1366, 2723, 0),
            new(1675, 2984, 0),
            new(2177, 3367, 10),
            new(1171, 3594, 0),
            new(1010, 2667, 5),
            new(1591, 2156, 5),
            new(2592, 464, 60),
            new(474, 1654, 0),
            new(897, 2411, 0),
            new(1471, 2505, 5),
            new(1257, 872, 16),
            new(2581, 1118, 0),
            new(2513, 1102, 0),
            new(1608, 3371, 0),
            new(4687, 1179, 0),
            new(3704, 2196, 20),
            new(3346, 572, 0),
            new(569, 1309, 0)
        };

        public WitchApprenticeQuest(PlayerMobile from) : base(from)
        {
        }

        // Serialization
        public WitchApprenticeQuest()
        {
        }

        public override Type[] TypeReferenceTable => m_TypeReferenceTable;

        public override object Name => 1055042;

        public override object OfferMessage => 1055001;

        public override TimeSpan RestartDelay => TimeSpan.FromMinutes(5.0);
        public override bool IsTutorial => false;

        public override int Picture => 0x15D3;

        public override void Accept()
        {
            base.Accept();

            AddConversation(new AcceptConversation());
        }

        public static Point3D RandomZeefzorpulLocation() => m_ZeefzorpulLocations.RandomElement();
    }
}
