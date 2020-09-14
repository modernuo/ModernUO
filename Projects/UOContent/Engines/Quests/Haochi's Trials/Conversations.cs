namespace Server.Engines.Quests.Samurai
{
    public class AcceptConversation : QuestConversation
    {
        public override object Message => 1049092;

        public override void OnRead()
        {
            System.AddObjective(new FindHaochiObjective());
        }
    }

    public class RadarConversation : QuestConversation
    {
        public override object Message => 1063033;

        public override bool Logged => false;
    }

    public class FirstTrialIntroConversation : QuestConversation
    {
        public override object Message => 1063029;

        public override void OnRead()
        {
            System.AddObjective(new FirstTrialIntroObjective());
        }
    }

    public class FirstTrialKillConversation : QuestConversation
    {
        public override object Message => 1063031;

        public override void OnRead()
        {
            System.AddObjective(new FirstTrialKillObjective());
        }
    }

    public class GainKarmaConversation : QuestConversation
    {
        private bool m_CursedSoul;

        public GainKarmaConversation(bool cursedSoul) => m_CursedSoul = cursedSoul;

        public GainKarmaConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_CursedSoul)
                {
                    return 1063040;
                }

                // You have just gained some <a href="?ForceTopic45">Karma</a> for killing a Young Ronin.
                return 1063041;
            }
        }

        public override bool Logged => false;

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

    public class SecondTrialIntroConversation : QuestConversation
    {
        private bool m_CursedSoul;

        public SecondTrialIntroConversation(bool cursedSoul) => m_CursedSoul = cursedSoul;

        public SecondTrialIntroConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_CursedSoul)
                {
                    return 1063045;
                }

                /* It is good that you rid the land of those dishonorable Samurai.
                   * Perhaps they will learn a greater lesson in death.<BR><BR>
                   *
                   * I have placed a reward in your pack.<BR><BR>
                   *
                   * The second trial will test your courage. You only have to follow
                   * the yellow path to see what awaits you.
                   */
                return 1063046;
            }
        }

        public override void OnRead()
        {
            System.AddObjective(new SecondTrialIntroObjective());
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

    public class SecondTrialAttackConversation : QuestConversation
    {
        public override object Message => 1063057;

        public override void OnRead()
        {
            System.AddObjective(new SecondTrialAttackObjective());
        }
    }

    public class ThirdTrialIntroConversation : QuestConversation
    {
        private bool m_Dragon;

        public ThirdTrialIntroConversation(bool dragon) => m_Dragon = dragon;

        public ThirdTrialIntroConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_Dragon)
                {
                    return 1063060;
                }

                /* Fear remains in your eyes but you have learned that not all is
                   * what it appears to be. <BR><BR>
                   *
                   * You must have known the dragon would slay you instantly.
                   * You elected the weaker opponent though the imp did not come
                   * here to destroy. You have much to learn. <BR><BR>
                   *
                   * In these lands, death is not forever. The shrines can make you whole
                   * again as can a helpful mage or healer. <BR><BR>
                   *
                   * Seek them out when you have been mortally wounded. <BR><BR>
                   *
                   * The next trial will test your benevolence. You only have to walk the blue path.
                   */
                return 1063059;
            }
        }

        public override void OnRead()
        {
            System.AddObjective(new ThirdTrialIntroObjective());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_Dragon = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_Dragon);
        }
    }

    public class ThirdTrialKillConversation : QuestConversation
    {
        public override object Message => 1063062;

        public override void OnRead()
        {
            System.AddObjective(new ThirdTrialKillObjective());
        }
    }

    public class FourthTrialIntroConversation : QuestConversation
    {
        public override object Message => 1063065;

        public override void OnRead()
        {
            System.AddObjective(new FourthTrialIntroObjective());
        }
    }

    public class FourthTrialCatsConversation : QuestConversation
    {
        public override object Message => 1063067;

        public override void OnRead()
        {
            System.AddObjective(new FourthTrialCatsObjective());
        }
    }

    public class FifthTrialIntroConversation : QuestConversation
    {
        private bool m_KilledCat;

        public FifthTrialIntroConversation(bool killedCat) => m_KilledCat = killedCat;

        public FifthTrialIntroConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_KilledCat)
                {
                    return 1063071;
                }

                /* You showed respect by helping another out while allowing the gypsy
                   * what little dignity she has left. <BR><BR>
                   *
                   * Now she will be able to feed herself and gain enough energy to walk
                   * to her camp. <BR><BR>
                   *
                   * The cats are her family members� cursed by an evil mage. <BR><BR>
                   *
                   * Once she has enough strength to walk back to the camp, she will be
                   * able to undo the spell. <BR><BR>
                   *
                   * You have been rewarded for completing your trial. And now you must
                   * prove yourself again. <BR><BR>Please retrieve my katana from the
                   * treasure room and return it to me.
                   */
                return 1063070;
            }
        }

        public override void OnRead()
        {
            System.AddObjective(new FifthTrialIntroObjective());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_KilledCat = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_KilledCat);
        }
    }

    public class FifthTrialReturnConversation : QuestConversation
    {
        public override object Message => 1063248;

        public override void OnRead()
        {
            System.AddObjective(new FifthTrialReturnObjective());
        }
    }

    public class LostSwordConversation : QuestConversation
    {
        public override object Message => 1063074;

        public override bool Logged => false;
    }

    public class SixthTrialIntroConversation : QuestConversation
    {
        private bool m_StolenTreasure;

        public SixthTrialIntroConversation(bool stolenTreasure) => m_StolenTreasure = stolenTreasure;

        public SixthTrialIntroConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_StolenTreasure)
                {
                    return 1063077;
                }

                /* Thank you for returning this sword to me and leaving the remaining
                   * treasure alone. <BR><BR>
                   *
                   * Your training is nearly complete. Before you have your final trial,
                   * you should pay homage to Samurai who came before you.  <BR><BR>
                   *
                   * Go into the Altar Room and light a candle for them. Afterwards, return to me.
                   */
                return 1063076;
            }
        }

        public override void OnRead()
        {
            System.AddObjective(new SixthTrialIntroObjective());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_StolenTreasure = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_StolenTreasure);
        }
    }

    public class SeventhTrialIntroConversation : QuestConversation
    {
        public override object Message => 1063079;

        public override void OnRead()
        {
            System.AddObjective(new SeventhTrialIntroObjective());
        }
    }

    public class EndConversation : QuestConversation
    {
        public override object Message => 1063125;

        public override void OnRead()
        {
            System.Complete();
        }
    }
}
