using Server.Items;
using Server.Mobiles;
using Server.Spells.Necromancy;

namespace Server.Engines.Quests.Necro
{
    public class AnimateMaabusCorpseObjective : QuestObjective
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1023643, 8787) // spellbook
        };

        public override object Message => 1060102;

        public override QuestItemInfo[] Info => m_Info;

        public override void OnComplete()
        {
            System.AddConversation(new MaabasConversation());
        }
    }

    public class FindCrystalCaveObjective : QuestObjective
    {
        public override object Message => 1060104;

        public override void OnComplete()
        {
            System.AddConversation(new HorusConversation());
        }
    }

    public class FindMardothAboutVaultObjective : QuestObjective
    {
        public override object Message => 1060106;

        public override void OnComplete()
        {
            System.AddConversation(new MardothVaultConversation());
        }
    }

    public class FindMaabusTombObjective : QuestObjective
    {
        public override object Message => 1060124;

        public override void CheckProgress()
        {
            if (System.From.Map == Map.Malas && System.From.InRange(new Point3D(2024, 1240, -90), 3))
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new FindMaabusCorpseObjective());
        }
    }

    public class FindMaabusCorpseObjective : QuestObjective
    {
        public override object Message => 1061142;

        public override void CheckProgress()
        {
            if (System.From.Map == Map.Malas && System.From.InRange(new Point3D(2024, 1223, -90), 3))
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new AnimateMaabusCorpseObjective());
        }
    }

    public class FindCityOfLightObjective : QuestObjective
    {
        public override object Message => 1060108;

        public override void CheckProgress()
        {
            if (System.From.Map == Map.Malas && System.From.InRange(new Point3D(1076, 519, -90), 5))
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new FindVaultOfSecretsObjective());
        }
    }

    public class FindVaultOfSecretsObjective : QuestObjective
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1023676, 3679) // glowing rune
        };

        public override object Message => 1060109;

        public override QuestItemInfo[] Info => m_Info;

        public override void CheckProgress()
        {
            if (System.From.Map == Map.Malas && System.From.InRange(new Point3D(1072, 455, -90), 1))
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            System.AddConversation(new VaultOfSecretsConversation());
        }
    }

    public class FetchAbraxusScrollObjective : QuestObjective
    {
        public override object Message => 1060196;

        public override void CheckProgress()
        {
            if (System.From.Map != Map.Malas || !System.From.InRange(new Point3D(1076, 450, -84), 5) ||
                !SummonFamiliarSpell.Table.TryGetValue(System.From, out var bc) || !(bc is HordeMinionFamiliar hmf) ||
                !hmf.InRange(System.From, 5) || hmf.TargetLocation != null)
            {
                return;
            }

            System.From.SendLocalizedMessage(
                1060113
            ); // You instinctively will your familiar to fetch the scroll for you.
            hmf.TargetLocation = new Point2D(1076, 450);
        }

        public override void OnComplete()
        {
            System.AddObjective(new RetrieveAbraxusScrollObjective());
        }
    }

    public class RetrieveAbraxusScrollObjective : QuestObjective
    {
        public override object Message => 1060199;

        public override void OnComplete()
        {
            System.AddConversation(new ReadAbraxusScrollConversation());
        }
    }

    public class ReadAbraxusScrollObjective : QuestObjective
    {
        public override object Message => 1060125;

        public override void OnComplete()
        {
            System.AddObjective(new ReturnToCrystalCaveObjective());
        }
    }

    public class ReturnToCrystalCaveObjective : QuestObjective
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1026153, 6178) // teleporter
        };

        public override object Message => 1060115;

        public override QuestItemInfo[] Info => m_Info;

        public override void OnComplete()
        {
            System.AddObjective(new SpeakCavePasswordObjective());
        }
    }

    public class SpeakCavePasswordObjective : QuestObjective
    {
        public override object Message => 1060117;

        public override void OnComplete()
        {
            System.AddConversation(new SecondHorusConversation());
        }
    }

    public class FindCallingScrollObjective : QuestObjective
    {
        private bool m_HealConversationShown;
        private bool m_SkitteringHoppersDisposed;

        private int m_SkitteringHoppersKilled;

        public override object Message => 1060119;

        public override bool IgnoreYoungProtection(Mobile from) => !m_SkitteringHoppersDisposed && from is SkitteringHopper;

        public override bool GetKillEvent(BaseCreature creature, Container corpse) => !m_SkitteringHoppersDisposed;

        public override void OnKill(BaseCreature creature, Container corpse)
        {
            if (creature is SkitteringHopper)
            {
                if (!m_HealConversationShown)
                {
                    m_HealConversationShown = true;
                    System.AddConversation(new HealConversation());
                }

                if (++m_SkitteringHoppersKilled >= 5)
                {
                    m_SkitteringHoppersDisposed = true;
                    System.AddObjective(new FindHorusAboutRewardObjective());
                }
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new FindMardothAboutKronusObjective());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_SkitteringHoppersKilled = reader.ReadEncodedInt();
            m_HealConversationShown = reader.ReadBool();
            m_SkitteringHoppersDisposed = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(m_SkitteringHoppersKilled);
            writer.Write(m_HealConversationShown);
            writer.Write(m_SkitteringHoppersDisposed);
        }
    }

    public class FindHorusAboutRewardObjective : QuestObjective
    {
        public override object Message => 1060126;

        public override void OnComplete()
        {
            System.AddConversation(new HorusRewardConversation());
        }
    }

    public class FindMardothAboutKronusObjective : QuestObjective
    {
        public override object Message => 1060127;

        public override void OnComplete()
        {
            System.AddConversation(new MardothKronusConversation());
        }
    }

    public class FindWellOfTearsObjective : QuestObjective
    {
        private static readonly Rectangle2D m_WellOfTearsArea = new(2080, 1346, 10, 10);

        private bool m_Inside;

        public override object Message => 1060128;

        public override void CheckProgress()
        {
            if (System.From.Map == Map.Malas && m_WellOfTearsArea.Contains(System.From.Location))
            {
                if (DarkTidesQuest.HasLostCallingScroll(System.From))
                {
                    if (!m_Inside)
                    {
                        System.AddConversation(new LostCallingScrollConversation(false));
                    }
                }
                else
                {
                    Complete();
                }

                m_Inside = true;
            }
            else
            {
                m_Inside = false;
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new UseCallingScrollObjective());
        }
    }

    public class UseCallingScrollObjective : QuestObjective
    {
        public override object Message => 1060130;
    }

    public class FindMardothEndObjective : QuestObjective
    {
        private bool m_Victory;

        public FindMardothEndObjective(bool victory) => m_Victory = victory;

        // Serialization
        public FindMardothEndObjective()
        {
        }

        public override object Message
        {
            get
            {
                if (m_Victory)
                {
                    return 1060131;
                }

                /* Although you were slain by the cowardly paladin,
                   * you managed to complete the rite of calling as
                   * instructed. Return to Mardoth.
                   */
                return 1060132;
            }
        }

        public override void OnComplete()
        {
            System.AddConversation(new MardothEndConversation());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_Victory = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_Victory);
        }
    }

    public class FindBankObjective : QuestObjective
    {
        public override object Message => 1060134;

        public override void CheckProgress()
        {
            if (System.From.Map == Map.Malas && System.From.InRange(new Point3D(2048, 1345, -84), 5))
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new CashBankCheckObjective());
        }
    }

    public class CashBankCheckObjective : QuestObjective
    {
        public override object Message => 1060644;

        public override void OnComplete()
        {
            System.AddConversation(new BankerConversation());
        }
    }
}
