namespace Server.Engines.Quests.Hag
{
	public class DontOfferConversation : QuestConversation
	{
		public override object Message => 1055000;

		public override bool Logged => false;

		public DontOfferConversation()
		{
		}
	}

	public class AcceptConversation : QuestConversation
	{
		public override object Message => 1055002;

		public AcceptConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindApprenticeObjective( true ) );
		}
	}

	public class HagDuringCorpseSearchConversation : QuestConversation
	{
		public override object Message => 1055003;

		public override bool Logged => false;

		public HagDuringCorpseSearchConversation()
		{
		}
	}

	public class ApprenticeCorpseConversation : QuestConversation
	{
		public override object Message => 1055004;

		public ApprenticeCorpseConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindGrizeldaAboutMurderObjective() );
		}
	}

	public class MurderConversation : QuestConversation
	{
		public override object Message => 1055005;

		public MurderConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new KillImpsObjective( true ) );
		}
	}

	public class HagDuringImpSearchConversation : QuestConversation
	{
		public override object Message => 1055006;

		public override bool Logged => false;

		public HagDuringImpSearchConversation()
		{
		}
	}

	public class ImpDeathConversation : QuestConversation
	{
		private Point3D m_ImpLocation;

		public override object Message => 1055007;

		public ImpDeathConversation( Point3D impLocation )
		{
			m_ImpLocation = impLocation;
		}

		public ImpDeathConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindZeefzorpulObjective( m_ImpLocation ) );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_ImpLocation = reader.ReadPoint3D();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (Point3D) m_ImpLocation );
		}
	}

	public class ZeefzorpulConversation : QuestConversation
	{
		public override object Message => 1055008;

		public ZeefzorpulConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new ReturnRecipeObjective() );
		}
	}

	public class RecipeConversation : QuestConversation
	{
		public override object Message => 1055009;

		public RecipeConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindIngredientObjective( new Ingredient[0] ) );
		}
	}

	public class HagDuringIngredientsConversation : QuestConversation
	{
		public override object Message => 1055012;

		public override bool Logged => false;

		public HagDuringIngredientsConversation()
		{
		}
	}

	public class BlackheartFirstConversation : QuestConversation
	{
		public override object Message => 1055010;

		public BlackheartFirstConversation()
		{
		}

		public override void OnRead()
		{
			if ( System.FindObjective( typeof( FindIngredientObjective ) ) is FindIngredientObjective obj )
				System.AddObjective( new FindIngredientObjective( obj.Ingredients, true ) );
		}
	}

	public class BlackheartNoPirateConversation : QuestConversation
	{
		private bool m_Tricorne;
		private bool m_Drunken;

		public override object Message
		{
			get
			{
				if ( m_Tricorne)
				{
					if ( m_Drunken )
					{
						/* <I>The filthy Captain flashes a pleased grin at you as he looks you up
						 * and down.</I><BR><BR>Well that's more like it, me little deck swabber!
						 * Ye almost look like ye fit in around here, ready te sail the great seas
						 * of Britannia, sinking boats and slaying sea serpents!<BR><BR>
						 *
						 * But can ye truly handle yerself?  Ye might think ye can test me meddle
						 * with a sip or two of yer dandy wine, but a real pirate walks the decks
						 * with a belly full of it.  Lookit that, yer not even wobblin'!<BR><BR>
						 *
						 * Ye've impressed me a bit, ye wee tyke, but it'll take more'n that te
						 * join me crew!<BR><BR><I>Captain Blackheart tips his mug in your direction,
						 * offering up a jolly laugh, but it seems you still haven't impressed him
						 * enough.</I>
						 */
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

				if ( m_Drunken )
				{
					/* <I>The inebriated pirate looks up at you with a wry grin.</I><BR><BR>
						 *
						 * Well hello again, me little matey.  I see ye have a belly full of rotgut
						 * in ye.  I bet ye think you're a right hero, ready te face the world.  But
						 * as I told ye before, bein' a member of my pirate crew means more'n just
						 * being able to hold yer drink.  Ye have te look the part - and frankly, me
						 * little barnacle, ye don't have the cut of cloth te fit in with the crowd I
						 * like te hang around.<BR><BR>
						 *
						 * So scurry off, ye wee sewer rat, and don't come back round these parts all
						 * liquored up an' three sheets te tha wind, unless yer truly ready te join
						 * me pirate crew!<BR><BR>
						 *
						 * <I>Captain Blackheart shoves you aside, banging his cutlass against the
						 * table as he calls to the waitress for another round.</I>
						 */
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

		public BlackheartNoPirateConversation( bool tricorne, bool drunken )
		{
			m_Tricorne = tricorne;
			m_Drunken = drunken;
		}

		public BlackheartNoPirateConversation()
		{
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_Tricorne = reader.ReadBool();
			m_Drunken = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_Tricorne );
			writer.Write( (bool) m_Drunken );
		}
	}

	public class BlackheartPirateConversation : QuestConversation
	{
		private bool m_FirstMet;

		public override object Message
		{
			get
			{
				if ( m_FirstMet )
				{
					/* <I>The bawdy old pirate captain looks up from his bottle of Wild Harpy
					 * whiskey, as drunk as any man you've ever seen.</I><BR><BR>
					 *
					 * Avast ye, ye loveable pirate!  Just in from sailin' the glorious sea?  Ye
					 * look right ready te fall down on the spot, ye do!<BR><BR>
					 *
					 * I tell ye what, from the look've ye, ye deserve a belt of better brew than
					 * the slop ye've been drinking, and I've just the thing.<BR><BR>
					 *
					 * I call it Captain Blackheart's Whiskey, and it'll give ye hairs on yer chest,
					 * that's for sure.  Why, a keg of this stuff once spilled on my ship, and it
					 * ate a hole right through the deck!<BR><BR>Go on, drink up, or use it to clean
					 * the rust off your cutlass - it's the best brew, either way!<BR><BR>
					 *
					 * <I>Captain Blackheart hands you a jug of his famous Whiskey. You think it best
					 * to return it to the Hag, rather than drink any of the noxious swill.</I>
					 */
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

		public BlackheartPirateConversation( bool firstMet )
		{
			m_FirstMet = firstMet;
		}

		public BlackheartPirateConversation()
		{
		}

		public override void OnRead()
		{
			if ( System.FindObjective( typeof( FindIngredientObjective ) ) is FindIngredientObjective obj )
				obj.NextStep();
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_FirstMet = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_FirstMet );
		}
	}

	public class EndConversation : QuestConversation
	{
		public override object Message => 1055013;

		public EndConversation()
		{
		}

		public override void OnRead()
		{
			System.Complete();
		}
	}

	public class RecentlyFinishedConversation : QuestConversation
	{
		public override object Message => 1055064;

		public RecentlyFinishedConversation()
		{
		}

		public override bool Logged => false;
	}
}
