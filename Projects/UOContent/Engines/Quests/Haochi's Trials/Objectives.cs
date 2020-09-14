using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
    public class FindHaochiObjective : QuestObjective
    {
        public override object Message => 1063026;

        public override void OnComplete()
        {
            System.AddConversation(new FirstTrialIntroConversation());
        }
    }

    public class FirstTrialIntroObjective : QuestObjective
    {
        public override object Message => 1063030;

        public override void OnComplete()
        {
            System.AddConversation(new FirstTrialKillConversation());
        }
    }

    public class FirstTrialKillObjective : QuestObjective
    {
        private int m_CursedSoulsKilled;
        private int m_YoungRoninKilled;

        public override object Message => 1063032;

        public override int MaxProgress => 3;

        public override void OnKill(BaseCreature creature, Container corpse)
        {
            if (creature is CursedSoul)
            {
                if (m_CursedSoulsKilled == 0)
                {
                    System.AddConversation(new GainKarmaConversation(true));
                }

                m_CursedSoulsKilled++;

                // Cursed Souls killed:  ~1_COUNT~
                System.From.SendLocalizedMessage(1063038, m_CursedSoulsKilled.ToString());
            }
            else if (creature is YoungRonin)
            {
                if (m_YoungRoninKilled == 0)
                {
                    System.AddConversation(new GainKarmaConversation(false));
                }

                m_YoungRoninKilled++;

                // Young Ronin killed:  ~1_COUNT~
                System.From.SendLocalizedMessage(1063039, m_YoungRoninKilled.ToString());
            }

            CurProgress = Math.Max(m_CursedSoulsKilled, m_YoungRoninKilled);
        }

        public override void OnComplete()
        {
            System.AddObjective(new FirstTrialReturnObjective(m_CursedSoulsKilled > m_YoungRoninKilled));
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_CursedSoulsKilled = reader.ReadEncodedInt();
            m_YoungRoninKilled = reader.ReadEncodedInt();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(m_CursedSoulsKilled);
            writer.WriteEncodedInt(m_YoungRoninKilled);
        }
    }

    public class FirstTrialReturnObjective : QuestObjective
    {
        private bool m_CursedSoul;

        public FirstTrialReturnObjective(bool cursedSoul) => m_CursedSoul = cursedSoul;

        public FirstTrialReturnObjective()
        {
        }

        public override object Message => 1063044;

        public override void OnComplete()
        {
            System.AddConversation(new SecondTrialIntroConversation(m_CursedSoul));
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_CursedSoul = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_CursedSoul);
        }
    }

    public class SecondTrialIntroObjective : QuestObjective
    {
        public override object Message => 1063047;

        public override void OnComplete()
        {
            System.AddConversation(new SecondTrialAttackConversation());
        }
    }

    public class SecondTrialAttackObjective : QuestObjective
    {
        public override object Message => 1063058;
    }

    public class SecondTrialReturnObjective : QuestObjective
    {
        public SecondTrialReturnObjective(bool dragon) => Dragon = dragon;

        public SecondTrialReturnObjective()
        {
        }

        public override object Message => 1063229;

        public bool Dragon { get; private set; }

        public override void OnComplete()
        {
            System.AddConversation(new ThirdTrialIntroConversation(Dragon));
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            Dragon = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(Dragon);
        }
    }

    public class ThirdTrialIntroObjective : QuestObjective
    {
        public override object Message => 1063061;

        public override void OnComplete()
        {
            System.AddConversation(new ThirdTrialKillConversation());
        }
    }

    public class ThirdTrialKillObjective : QuestObjective
    {
        public override object Message => 1063063;

        public override void OnKill(BaseCreature creature, Container corpse)
        {
            if (creature is InjuredWolf)
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new ThirdTrialReturnObjective());
        }
    }

    public class ThirdTrialReturnObjective : QuestObjective
    {
        public override object Message => 1063064;

        public override void OnComplete()
        {
            System.AddConversation(new FourthTrialIntroConversation());
        }
    }

    public class FourthTrialIntroObjective : QuestObjective
    {
        public override object Message => 1063066;

        public override void OnComplete()
        {
            System.AddConversation(new FourthTrialCatsConversation());
        }
    }

    public class FourthTrialCatsObjective : QuestObjective
    {
        public override object Message => 1063068;

        public override void OnKill(BaseCreature creature, Container corpse)
        {
            if (creature is DiseasedCat)
            {
                Complete();
                System.AddObjective(new FourthTrialReturnObjective(true));
            }
        }
    }

    public class FourthTrialReturnObjective : QuestObjective
    {
        public FourthTrialReturnObjective(bool killedCat) => KilledCat = killedCat;

        public FourthTrialReturnObjective()
        {
        }

        public override object Message => 1063242;

        public bool KilledCat { get; private set; }

        public override void OnComplete()
        {
            System.AddConversation(new FifthTrialIntroConversation(KilledCat));
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            KilledCat = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(KilledCat);
        }
    }

    public class FifthTrialIntroObjective : QuestObjective
    {
        public override object Message => 1063072;

        public bool StolenTreasure { get; set; }

        public override void OnComplete()
        {
            System.AddConversation(new FifthTrialReturnConversation());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            StolenTreasure = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(StolenTreasure);
        }
    }

    public class FifthTrialReturnObjective : QuestObjective
    {
        public override object Message => 1063073;
    }

    public class SixthTrialIntroObjective : QuestObjective
    {
        public override object Message => 1063078;

        public override void OnComplete()
        {
            System.AddObjective(new SixthTrialReturnObjective());
        }
    }

    public class SixthTrialReturnObjective : QuestObjective
    {
        public override object Message => 1063252;

        public override void OnComplete()
        {
            System.AddConversation(new SeventhTrialIntroConversation());
        }
    }

    public class SeventhTrialIntroObjective : QuestObjective
    {
        public override object Message => 1063080;

        public override int MaxProgress => 3;

        public override void OnKill(BaseCreature creature, Container corpse)
        {
            if (creature is YoungNinja)
            {
                CurProgress++;
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new SeventhTrialReturnObjective());
        }
    }

    public class SeventhTrialReturnObjective : QuestObjective
    {
        public override object Message => 1063253;

        public override void OnComplete()
        {
            System.AddConversation(new EndConversation());
        }
    }
}
