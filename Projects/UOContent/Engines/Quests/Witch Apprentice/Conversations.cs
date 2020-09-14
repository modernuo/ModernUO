using System;

namespace Server.Engines.Quests.Hag
{
    public class DontOfferConversation : QuestConversation
    {
        public override object Message => 1055000;

        public override bool Logged => false;
    }

    public class AcceptConversation : QuestConversation
    {
        public override object Message => 1055002;

        public override void OnRead()
        {
            System.AddObjective(new FindApprenticeObjective(true));
        }
    }

    public class HagDuringCorpseSearchConversation : QuestConversation
    {
        public override object Message => 1055003;

        public override bool Logged => false;
    }

    public class ApprenticeCorpseConversation : QuestConversation
    {
        public override object Message => 1055004;

        public override void OnRead()
        {
            System.AddObjective(new FindGrizeldaAboutMurderObjective());
        }
    }

    public class MurderConversation : QuestConversation
    {
        public override object Message => 1055005;

        public override void OnRead()
        {
            System.AddObjective(new KillImpsObjective(true));
        }
    }

    public class HagDuringImpSearchConversation : QuestConversation
    {
        public override object Message => 1055006;

        public override bool Logged => false;
    }

    public class ImpDeathConversation : QuestConversation
    {
        private Point3D m_ImpLocation;

        public ImpDeathConversation(Point3D impLocation) => m_ImpLocation = impLocation;

        public ImpDeathConversation()
        {
        }

        public override object Message => 1055007;

        public override void OnRead()
        {
            System.AddObjective(new FindZeefzorpulObjective(m_ImpLocation));
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_ImpLocation = reader.ReadPoint3D();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_ImpLocation);
        }
    }

    public class ZeefzorpulConversation : QuestConversation
    {
        public override object Message => 1055008;

        public override void OnRead()
        {
            System.AddObjective(new ReturnRecipeObjective());
        }
    }

    public class RecipeConversation : QuestConversation
    {
        public override object Message => 1055009;

        public override void OnRead()
        {
            System.AddObjective(new FindIngredientObjective(Array.Empty<Ingredient>()));
        }
    }

    public class HagDuringIngredientsConversation : QuestConversation
    {
        public override object Message => 1055012;

        public override bool Logged => false;
    }

    public class BlackheartFirstConversation : QuestConversation
    {
        public override object Message => 1055010;

        public override void OnRead()
        {
            var obj = System.FindObjective<FindIngredientObjective>();
            if (obj != null)
            {
                System.AddObjective(new FindIngredientObjective(obj.Ingredients, true));
            }
        }
    }

    public class BlackheartNoPirateConversation : QuestConversation
    {
        private bool m_Drunken;
        private bool m_Tricorne;

        public BlackheartNoPirateConversation(bool tricorne, bool drunken)
        {
            m_Tricorne = tricorne;
            m_Drunken = drunken;
        }

        public BlackheartNoPirateConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_Tricorne)
                {
                    if (m_Drunken)
                    {
                        return 1055059;
                    }

                    /* <I>Captain Blackheart looks up from polishing his cutlass, glaring at
                       * you with red-rimmed eyes.</I><BR><BR>
                       *
                       * Well, well.  Lookit the wee little deck swabby.  Aren't ye a cute lil'
                       * lassy?  Don't ye look just fancy?  Ye think yer ready te join me pirate
                       * crew?  Ye think I should offer ye some've me special Blackheart brew?<BR><BR>
                       *
                       * I'll make ye walk the plank, I will!  We'll see how sweet n' darlin' ye
                       * look when the sea serpents get at ye and rip ye te threads!  Won't that be
                       * a pretty picture, eh?<BR><BR>
                       *
                       * Ye don't have the stomach fer the pirate life, that's plain enough te me.  Ye
                       * prance around here like a wee lil' princess, ye do.  If ye want to join my
                       * crew ye can't just look tha part - ye have to have the stomach fer it, filled
                       * up with rotgut until ye can't see straight.  I don't drink with just any ol'
                       * landlubber!  Ye'd best prove yer mettle before ye talk te me again!<BR><BR>
                       *
                       * <I>The drunken pirate captain leans back in his chair, taking another gulp of
                       * his drink before he starts in on another bawdy pirate song.</I>
                       */
                    return 1055057;
                }

                if (m_Drunken)
                {
                    return 1055056;
                }

                /* <I>Captain Blackheart looks up from his drink, almost tipping over
                     * his chair as he looks you up and down.</I><BR><BR>
                     *
                     * You again?  I thought I told ye te get lost?  Go on with ye!  Ye ain't
                     * no pirate - yer not even fit te clean the barnacles off me rear end!
                     * Don't ye come back babbling te me for any of me Blackheart Whiskey until
                     * ye look and act like a true pirate!<BR><BR>
                     *
                     * Now shove off, sewer rat - I've got drinkin' te do!<BR><BR>
                     *
                     * <I>The inebriated pirate bolts back another mug of ale and brushes you
                     * off with a wave of his hand.</I>
                     */
                return 1055058;
            }
        }

        public override bool Logged => false;

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_Tricorne = reader.ReadBool();
            m_Drunken = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_Tricorne);
            writer.Write(m_Drunken);
        }
    }

    public class BlackheartPirateConversation : QuestConversation
    {
        private bool m_FirstMet;

        public BlackheartPirateConversation(bool firstMet) => m_FirstMet = firstMet;

        public BlackheartPirateConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_FirstMet)
                {
                    return 1055054;
                }

                /* <I>The drunken pirate, Captain Blackheart, looks up from his bottle
                   * of whiskey with a pleased expression.</I><BR><BR>
                   *
                   * Well looky here!  I didn't think a landlubber like yourself had the pirate
                   * blood in ye!  But look at that!  You certainly look the part now!  Sure
                   * you can still keep on your feet?  Har!<BR><BR>
                   *
                   * Avast ye, ye loveable pirate!  Ye deserve a belt of better brew than the slop
                   * ye've been drinking, and I've just the thing.<BR><BR>
                   *
                   * I call it Captain Blackheart's Whiskey, and it'll give ye hairs on yer chest,
                   * that's for sure.  Why, a keg of this stuff once spilled on my ship, and it ate
                   * a hole right through the deck!<BR><BR>
                   *
                   * Go on, drink up, or use it to clean the rust off your cutlass - it's the best
                   * brew, either way!<BR><BR>
                   *
                   * <I>Captain Blackheart hands you a jug of his famous Whiskey. You think it best
                   * to return it to the Hag, rather than drink any of the noxious swill.</I>
                   */
                return 1055011;
            }
        }

        public override void OnRead()
        {
            System.FindObjective<FindIngredientObjective>()?.NextStep();
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_FirstMet = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_FirstMet);
        }
    }

    public class EndConversation : QuestConversation
    {
        public override object Message => 1055013;

        public override void OnRead()
        {
            System.Complete();
        }
    }

    public class RecentlyFinishedConversation : QuestConversation
    {
        public override object Message => 1055064;

        public override bool Logged => false;
    }
}
