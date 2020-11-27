namespace Server.Engines.Quests.Haven
{
    public class AcceptConversation : QuestConversation
    {
        public override object Message => 1049092;

        public override void OnRead()
        {
            System.AddObjective(new FindUzeraanBeginObjective());
        }
    }

    public class UzeraanTitheConversation : QuestConversation
    {
        public override object Message => 1060209;

        public override void OnRead()
        {
            System.AddObjective(new TitheGoldObjective());
        }
    }

    public class UzeraanFirstTaskConversation : QuestConversation
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1023676, 0xE68) // glowing rune
        };

        public override object Message
        {
            get
            {
                if (System.From.Profession == 1) // warrior
                {
                    return 1049088;
                }

                if (System.From.Profession == 2) // magician
                {
                    return 1049386;
                }

                /* <I>Uzeraan nods at you with approval and begins to speak...</I><BR><BR>
                           *
                           * Now that you are ready, let me give you your first task.<BR><BR>
                           *
                           * As I mentioned earlier, we have been trying to fight back the wicked
                           * <I>Horde Minions</I> which have recently begun attacking our cities
                           * - but to no avail. Our need is great!<BR><BR>
                           *
                           * Your first task will be to assess the situation in the mountain pass,
                           * and help our troops defeat the Horde Minions there.<BR><BR>
                           *
                           * Take the road marked with glowing runes, that starts just outside of this mansion.
                           * Before you go into battle, it would be prudent to
                           * <a href="?ForceTopic27">review combat techniques</a> as well as
                           * <a href = "?ForceTopic29">information on healing yourself,
                           * using your Paladin ability 'Close Wounds'</a>.<BR><BR>
                           *
                           * To aid you in your fight, you may also wish to
                           * <a href = "?ForceTopic33">purchase equipment</a> from Frank the Blacksmith,
                           * who is standing just <a href = "?ForceTopic13">South</a> of here.<BR><BR>
                           *
                           * Good luck young Paladin!
                           */
                return 1060388;
            }
        }

        public override QuestItemInfo[] Info => m_Info;

        public override void OnRead()
        {
            System.AddObjective(new KillHordeMinionsObjective());
        }
    }

    public class UzeraanReportConversation : QuestConversation
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1026153, 0x1822), // teleporter
            new(1048032, 0xE76)   // a bag
        };

        public override object Message
        {
            get
            {
                if (System.From.Profession == 2) // magician
                {
                    return 1049387;
                }

                /* <I>You give your report to Uzeraan and after a while,
                   * he begins to speak...</I><BR><BR>
                   *
                   * Your report is grim, but all hope is not lost!  It has become apparent
                   * that our swords and spells will not drive the evil from Haven.<BR><BR>
                   *
                   * The head of my order, the High Mage Schmendrick, arrived here shortly after
                   * you went into battle with the <I>Horde Minions</I>.  He has brought with him a
                   * scroll of great power, that should aid us greatly in our battle.<BR><BR>
                   *
                   * Unfortunately, the entrance to one of our mining caves collapsed recently,
                   * trapping our miners inside.<BR><BR>
                   *
                   * Schmendrick went to install magical teleporters inside the mines so that
                   * the miners would have a way out.  The miners have since returned, but Schmendrick has not.
                   * Those who have returned, all seem to have lost their minds to madness;
                   * mumbling strange things of "the souls of the dead seeking revenge".<BR><BR>
                   *
                   * No matter. We must find Schmendrick.<BR><BR>
                   *
                   * Step onto the teleporter, located against the wall, and seek Schmendrick in the mines.<BR><BR>
                   *
                   * I've given you a bag with some <a href="?ForceTopic75">Night Sight</a>
                   * and <a href="?ForceTopic76">Healing</a> <a href="?ForceTopic74">potions</a>
                   * to help you out along the way.  Good luck.
                   */
                return 1049119;
            }
        }

        public override QuestItemInfo[] Info => m_Info;

        public override void OnRead()
        {
            System.AddObjective(new FindSchmendrickObjective());
        }
    }

    public class SchmendrickConversation : QuestConversation
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1023637, 0xE34) // scroll
        };

        public override object Message
        {
            get
            {
                if (System.From.Profession == 5) // paladin
                {
                    return 1060749;
                }

                /* <I>Schmendrick barely pays you any attention as you approach him.  His
                   * mind seems to be occupied with something else.  You explain to him that
                   * you came for the scroll of power and after a long while he begins to speak,
                   * but apparently still not giving you his full attention...</I><BR><BR>
                   *
                   * Hmmm.. peculiar indeed.  Very strange activity here indeed... I wonder...<BR><BR>
                   *
                   * Hmmm.  Oh yes! Scroll, you say?  I don't have it, sorry. My apprentice was
                   * carrying it, and he ran off to somewhere in this cave.  Find him and you will
                   * find the scroll.<BR><BR>Be sure to bring the scroll to Uzeraan once you
                   * have it. He's the only person aside from myself who can read the ancient
                   * markings on the scroll.  I need to figure out what's going on down here before
                   * I can leave.  Strange activity indeed...<BR><BR>
                   *
                   * <I>Schmendrick goes back to his work and you seem to completely fade from his
                   * awareness...
                   */
                return 1049322;
            }
        }

        public override QuestItemInfo[] Info => m_Info;

        public override void OnRead()
        {
            System.AddObjective(new FindApprenticeObjective());
        }
    }

    public class UzeraanScrollOfPowerConversation : QuestConversation
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1048030, 0x14EB), // a Treasure Map
            new(1023969, 0xF81),  // Fertile Dirt
            new(1049117, 0xFC4)   // Horn of Retreat
        };

        public override object Message => 1049325;

        public override QuestItemInfo[] Info => m_Info;

        public override void OnRead()
        {
            System.AddObjective(new FindDryadObjective());
        }
    }

    public class DryadConversation : QuestConversation
    {
        public override object Message => 1049326;

        public override void OnRead()
        {
            System.AddObjective(new ReturnFertileDirtObjective());
        }
    }

    public class UzeraanFertileDirtConversation : QuestConversation
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1023965, 0xF7D), // Daemon Blood
            new(1022581, 0xA22)  // lantern
        };

        public override object Message
        {
            get
            {
                if (System.From.Profession == 2) // magician
                {
                    return 1049388;
                }

                /* <I>Uzeraan takes the dirt from you and smiles...<BR><BR></I>
                   *
                   * Wonderful!  I knew I could count on you.  As a token of my appreciation
                   * I've given you a bag with some bandages as well as some healing potions.
                   * They should help out a bit.<BR><BR>
                   *
                   * The next item I need is a <I>Vial of Blood</I>.  I know it seems strange,
                   * but that's what the formula asks for.  I have some locked away in a chest
                   * not far from here.  It's only a short distance from the mansion.  Let me give
                   * you directions...<BR><BR>
                   *
                   * Exit the front door to the East.  Then follow the path to the North.
                   * You will pass by several pedestals with lanterns on them.  Continue on this
                   * path until you run into a small hut.  Walk up the stairs and through the door.
                   * Inside you will find a chest.  Open it and bring me a <I>Vial of Blood</I>
                   * from inside the chest.  It's very easy to find.  Just follow the road and you
                   * can't miss it.<BR><BR>
                   *
                   * Good luck!
                   */
                return 1049329;
            }
        }

        public override QuestItemInfo[] Info => m_Info;

        public override void OnRead()
        {
            System.AddObjective(new GetDaemonBloodObjective());
        }
    }

    public class UzeraanDaemonBloodConversation : QuestConversation
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1017412, 0xF80) // Daemon Bone
        };

        private static readonly QuestItemInfo[] m_InfoPaladin =
        {
            new(1017412, 0xF80), // Daemon Bone
            new(1060577, 0x1F14) // Recall Rune
        };

        public override object Message
        {
            get
            {
                if (System.From.Profession == 2) // magician
                {
                    return "<I>You hand Uzeraan the Vial of Blood, which he hastily accepts...</I><BR>"
                           + "<BR>"
                           + "Excellent work!  Only one reagent remains and the spell is complete!  The final "
                           + "requirement is a <I>Daemon Bone</I>, which will not be as easily acquired as the "
                           + "previous two components.<BR>"
                           + "<BR>"
                           + "There is a haunted graveyard on this island, which is the home to many undead "
                           + "creatures.   Dispose of the undead as you see fit.  Be sure to search their remains "
                           + "after you have smitten them, to check for a <I>Daemon Bone</I>.  I'm quite sure "
                           + "that you will find what we seek, if you are thorough enough with your "
                           + "extermination.<BR>"
                           + "<BR>"
                           + "Take these explosion spell scrolls and  magical wizard's hat to aid you in your "
                           + "battle.  The scrolls should help you make short work of the undead.<BR>"
                           + "<BR>"
                           + "Return here when you have found a <I>Daemon Bone</I>.";
                }

                /* <I>You hand Uzeraan the Vial of Blood, which he hastily accepts...</I><BR><BR>
                   *
                   * Excellent work!  Only one reagent remains and the spell is complete!
                   * The final requirement is a <I>Daemon Bone</I>, which will not be as easily
                   * acquired as the previous two components.<BR><BR>
                   *
                   * There is a haunted graveyard on this island, which is the home to many
                   * undead creatures.   Dispose of the undead as you see fit.  Be sure to search
                   * their remains after you have smitten them, to check for a <I>Daemon Bone</I>.
                   * I'm quite sure that you will find what we seek, if you are thorough enough
                   * with your extermination.<BR><BR>
                   *
                   * Take this magical silver sword to aid you in your battle.  Silver weapons
                   * will damage the undead twice as much as your regular weapon.<BR><BR>
                   *
                   * Return here when you have found a <I>Daemon Bone</I>.
                   */
                return 1049333;
            }
        }

        public override QuestItemInfo[] Info
        {
            get
            {
                if (System.From.Profession == 5) // paladin
                {
                    return m_InfoPaladin;
                }

                return m_Info;
            }
        }

        public override void OnRead()
        {
            System.AddObjective(new GetDaemonBoneObjective());
        }
    }

    public class UzeraanDaemonBoneConversation : QuestConversation
    {
        public override object Message => 1049335;

        public override void OnRead()
        {
            System.AddObjective(new CashBankCheckObjective());
        }
    }

    public class BankerConversation : QuestConversation
    {
        public override object Message => 1060137;

        public override void OnRead()
        {
            System.Complete();
        }
    }

    public class RadarConversation : QuestConversation
    {
        public override object Message => 1049660;

        public override bool Logged => false;
    }

    public class LostScrollOfPowerConversation : QuestConversation
    {
        private bool m_FromUzeraan;

        public LostScrollOfPowerConversation(bool fromUzeraan) => m_FromUzeraan = fromUzeraan;

        public LostScrollOfPowerConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_FromUzeraan)
                {
                    return 1049377;
                }

                /* You've lost the scroll?  Argh!  I will have to try and re-construct
                   * the scroll from memory.  Bring me a blank scroll, which you can
                   * <a href = "?ForceTopic33">purchase from the mage shop</a> just
                   * <a href = "?ForceTopic13">East</a> of Uzeraan's mansion in Haven.<BR><BR>
                   *
                   * Return the scroll to me and I will try to make another scroll for you.<BR><BR>
                   *
                   * When you return, be sure to hand me the scroll (drag and drop).
                   */
                return 1049345;
            }
        }

        public override bool Logged => false;

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_FromUzeraan = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_FromUzeraan);
        }
    }

    public class LostFertileDirtConversation : QuestConversation
    {
        private bool m_FromUzeraan;

        public LostFertileDirtConversation(bool fromUzeraan) => m_FromUzeraan = fromUzeraan;

        public LostFertileDirtConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_FromUzeraan)
                {
                    return 1049374;
                }

                /* You've lost the dirt I gave you?<BR><BR>
                   *
                   * My, my, my... What ever shall we do now?<BR><BR>
                   *
                   * I can try to make you some more, but I will need something
                   * that I can transform.  Bring me an <I>apple</I>, and I shall
                   * see what I can do.<BR><BR>
                   *
                   * You can <a href = "?ForceTopic33">buy</a> apples from the
                   * Provisioner's Shop, which is located a ways <a href = "?ForceTopic13">East</a>
                   * of Uzeraan's mansion.<BR><BR>
                   *
                   * Hand me the apple when you have it, and I shall see about transforming
                   * it for you.<BR><BR>
                   *
                   * Good luck.<BR><BR>
                   */
                return 1049359;
            }
        }

        public override bool Logged => false;

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_FromUzeraan = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_FromUzeraan);
        }
    }

    public class DryadAppleConversation : QuestConversation
    {
        public override object Message => 1049360;

        public override bool Logged => false;
    }

    public class LostDaemonBloodConversation : QuestConversation
    {
        public override object Message => 1049375;

        public override bool Logged => false;
    }

    public class LostDaemonBoneConversation : QuestConversation
    {
        public override object Message => 1049376;

        public override bool Logged => false;
    }

    public class FewReagentsConversation : QuestConversation
    {
        public override object Message => 1049390;

        public override bool Logged => false;
    }
}
