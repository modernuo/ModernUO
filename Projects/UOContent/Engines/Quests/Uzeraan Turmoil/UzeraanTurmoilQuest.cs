using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
    public class UzeraanTurmoilQuest : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(AcceptConversation),
            typeof(UzeraanTitheConversation),
            typeof(UzeraanFirstTaskConversation),
            typeof(UzeraanReportConversation),
            typeof(SchmendrickConversation),
            typeof(UzeraanScrollOfPowerConversation),
            typeof(DryadConversation),
            typeof(UzeraanFertileDirtConversation),
            typeof(UzeraanDaemonBloodConversation),
            typeof(UzeraanDaemonBoneConversation),
            typeof(BankerConversation),
            typeof(RadarConversation),
            typeof(LostScrollOfPowerConversation),
            typeof(LostFertileDirtConversation),
            typeof(DryadAppleConversation),
            typeof(LostDaemonBloodConversation),
            typeof(LostDaemonBoneConversation),
            typeof(FindUzeraanBeginObjective),
            typeof(TitheGoldObjective),
            typeof(FindUzeraanFirstTaskObjective),
            typeof(KillHordeMinionsObjective),
            typeof(FindUzeraanAboutReportObjective),
            typeof(FindSchmendrickObjective),
            typeof(FindApprenticeObjective),
            typeof(ReturnScrollOfPowerObjective),
            typeof(FindDryadObjective),
            typeof(ReturnFertileDirtObjective),
            typeof(GetDaemonBloodObjective),
            typeof(ReturnDaemonBloodObjective),
            typeof(GetDaemonBoneObjective),
            typeof(ReturnDaemonBoneObjective),
            typeof(CashBankCheckObjective),
            typeof(FewReagentsConversation)
        };

        private bool m_HasLeftTheMansion;

        public UzeraanTurmoilQuest(PlayerMobile from) : base(from)
        {
        }

        // Serialization
        public UzeraanTurmoilQuest()
        {
        }

        public override Type[] TypeReferenceTable => m_TypeReferenceTable;

        public override object Name => 1049007;

        public override object OfferMessage => 1049008;

        public override TimeSpan RestartDelay => TimeSpan.MaxValue;
        public override bool IsTutorial => true;

        public override int Picture
        {
            get
            {
                return From.Profession switch
                {
                    1 => 0x15C9, // warrior
                    2 => 0x15C1, // magician
                    _ => 0x15D3
                };
            }
        }

        public override void Slice()
        {
            if (!m_HasLeftTheMansion &&
                (From.Map != Map.Trammel || From.X < 3573 || From.X > 3611 || From.Y < 2568 || From.Y > 2606))
            {
                m_HasLeftTheMansion = true;
                AddConversation(new RadarConversation());
            }

            base.Slice();
        }

        public override void Accept()
        {
            base.Accept();

            AddConversation(new AcceptConversation());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_HasLeftTheMansion = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_HasLeftTheMansion);
        }

        public static bool HasLostScrollOfPower(Mobile from)
        {
            if (!(from is PlayerMobile pm))
            {
                return false;
            }

            var qs = pm.Quest;

            if (qs is UzeraanTurmoilQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(ReturnScrollOfPowerObjective)))
                {
                    return from.Backpack?.FindItemByType<SchmendrickScrollOfPower>() == null;
                }
            }

            return false;
        }

        public static bool HasLostFertileDirt(Mobile from)
        {
            if (!(from is PlayerMobile pm))
            {
                return false;
            }

            var qs = pm.Quest;

            if (qs is UzeraanTurmoilQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(ReturnFertileDirtObjective)))
                {
                    return from.Backpack?.FindItemByType<QuestFertileDirt>() == null;
                }
            }

            return false;
        }

        public static bool HasLostDaemonBlood(Mobile from)
        {
            if (!(from is PlayerMobile pm))
            {
                return false;
            }

            var qs = pm.Quest;

            if (qs is UzeraanTurmoilQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(ReturnDaemonBloodObjective)))
                {
                    return from.Backpack?.FindItemByType<QuestDaemonBlood>() == null;
                }
            }

            return false;
        }

        public static bool HasLostDaemonBone(Mobile from)
        {
            if (!(from is PlayerMobile pm))
            {
                return false;
            }

            var qs = pm.Quest;

            if (qs is UzeraanTurmoilQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(ReturnDaemonBoneObjective)))
                {
                    return from.Backpack?.FindItemByType<QuestDaemonBone>() == null;
                }
            }

            return false;
        }
    }
}
