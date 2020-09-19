using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro
{
    public class DarkTidesQuest : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(AcceptConversation),
            typeof(AnimateMaabusCorpseObjective),
            typeof(BankerConversation),
            typeof(CashBankCheckObjective),
            typeof(FetchAbraxusScrollObjective),
            typeof(FindBankObjective),
            typeof(FindCallingScrollObjective),
            typeof(FindCityOfLightObjective),
            typeof(FindCrystalCaveObjective),
            typeof(FindMaabusCorpseObjective),
            typeof(FindMaabusTombObjective),
            typeof(FindMardothAboutKronusObjective),
            typeof(FindMardothAboutVaultObjective),
            typeof(FindMardothEndObjective),
            typeof(FindVaultOfSecretsObjective),
            typeof(FindWellOfTearsObjective),
            typeof(HorusConversation),
            typeof(LostCallingScrollConversation),
            typeof(MaabasConversation),
            typeof(MardothEndConversation),
            typeof(MardothKronusConversation),
            typeof(MardothVaultConversation),
            typeof(RadarConversation),
            typeof(ReadAbraxusScrollConversation),
            typeof(ReadAbraxusScrollObjective),
            typeof(ReanimateMaabusConversation),
            typeof(RetrieveAbraxusScrollObjective),
            typeof(ReturnToCrystalCaveObjective),
            typeof(SecondHorusConversation),
            typeof(SpeakCavePasswordObjective),
            typeof(UseCallingScrollObjective),
            typeof(VaultOfSecretsConversation),
            typeof(FindHorusAboutRewardObjective),
            typeof(HealConversation),
            typeof(HorusRewardConversation)
        };

        public DarkTidesQuest(PlayerMobile from) : base(from)
        {
        }

        // Serialization
        public DarkTidesQuest()
        {
        }

        public override Type[] TypeReferenceTable => m_TypeReferenceTable;

        public override object Name => 1060095;

        public override object OfferMessage => 1060094;

        public override TimeSpan RestartDelay => TimeSpan.MaxValue;
        public override bool IsTutorial => true;

        public override int Picture => 0x15B5;

        public override void Accept()
        {
            base.Accept();

            AddConversation(new AcceptConversation());
        }

        public override bool IgnoreYoungProtection(Mobile from)
        {
            if (from is SummonedPaladin)
            {
                return true;
            }

            return base.IgnoreYoungProtection(from);
        }

        public static bool HasLostCallingScroll(Mobile from)
        {
            if (!(from is PlayerMobile pm))
            {
                return false;
            }

            var qs = pm.Quest;

            if (qs is DarkTidesQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(FindMardothAboutKronusObjective)) ||
                    qs.IsObjectiveInProgress(typeof(FindWellOfTearsObjective)) ||
                    qs.IsObjectiveInProgress(typeof(UseCallingScrollObjective)))
                {
                    return from.Backpack?.FindItemByType<KronusScroll>() == null;
                }
            }

            return false;
        }
    }
}
