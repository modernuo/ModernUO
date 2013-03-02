using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
	#region Quests

	public class TheyreBreedingLikeRabbits : MLQuest
	{
		public TheyreBreedingLikeRabbits()
		{
			Activated = true;
			Title = 1072244; // They're Breeding Like Rabbits
			Description = 1072259; // Aaaahhhh!  They're everywhere!  Aaaaahhh!  Ahem.  Actually, friend, how do you feel about rabbits? Well, we're being overrun by them.  We're finding fuzzy bunnies everywhere. Aaaaahhh!
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Rabbit ) }, "rabbits" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}

		public override void Generate()
		{
			base.Generate();

			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Saril" ), new Point3D( 7075, 376, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Saril" ), new Point3D( 7075, 376, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Cailla" ), new Point3D( 7075, 377, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Cailla" ), new Point3D( 7075, 377, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Tamm" ), new Point3D( 7075, 378, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Tamm" ), new Point3D( 7075, 378, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 3, "Landy" ), new Point3D( 7089, 390, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 3, "Landy" ), new Point3D( 7089, 390, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Alejaha" ), new Point3D( 7043, 387, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Alejaha" ), new Point3D( 7043, 387, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 3, "Mielan" ), new Point3D( 7063, 350, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 3, "Mielan" ), new Point3D( 7063, 350, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Ciala" ), new Point3D( 7031, 411, 7 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Ciala" ), new Point3D( 7031, 411, 7 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Aniel" ), new Point3D( 7034, 412, 6 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Aniel" ), new Point3D( 7034, 412, 6 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Aulan" ), new Point3D( 6986, 340, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Aulan" ), new Point3D( 6986, 340, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Brinnae" ), new Point3D( 6996, 351, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Brinnae" ), new Point3D( 6996, 351, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Caelas" ), new Point3D( 7039, 390, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Caelas" ), new Point3D( 7039, 390, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Clehin" ), new Point3D( 7092, 390, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Clehin" ), new Point3D( 7092, 390, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Cloorne" ), new Point3D( 7010, 364, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Cloorne" ), new Point3D( 7010, 364, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Salaenih" ), new Point3D( 7009, 362, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Salaenih" ), new Point3D( 7009, 362, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Vilo" ), new Point3D( 7029, 377, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Vilo" ), new Point3D( 7029, 377, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Tholef" ), new Point3D( 6986, 386, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Tholef" ), new Point3D( 6986, 386, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Tillanil" ), new Point3D( 6987, 388, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Tillanil" ), new Point3D( 6987, 388, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Waelian" ), new Point3D( 6996, 381, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Waelian" ), new Point3D( 6996, 381, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Sleen" ), new Point3D( 6997, 381, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Sleen" ), new Point3D( 6997, 381, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Unoelil" ), new Point3D( 7010, 388, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Unoelil" ), new Point3D( 7010, 388, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Anolly" ), new Point3D( 7009, 388, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Anolly" ), new Point3D( 7009, 388, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Jusae" ), new Point3D( 7042, 377, 2 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Jusae" ), new Point3D( 7042, 377, 2 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Cillitha" ), new Point3D( 7043, 377, 2 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Cillitha" ), new Point3D( 7043, 377, 2 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Lohn" ), new Point3D( 7062, 410, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Lohn" ), new Point3D( 7062, 410, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Olla" ), new Point3D( 7063, 410, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Olla" ), new Point3D( 7063, 410, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Thallary" ), new Point3D( 7032, 439, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Thallary" ), new Point3D( 7032, 439, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Ahie" ), new Point3D( 7033, 440, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Ahie" ), new Point3D( 7033, 440, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Tyeelor" ), new Point3D( 7010, 364, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Tyeelor" ), new Point3D( 7010, 364, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Athailon" ), new Point3D( 7011, 365, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Athailon" ), new Point3D( 7011, 365, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderTaellia" ), new Point3D( 7038, 387, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderTaellia" ), new Point3D( 7038, 387, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderMallew" ), new Point3D( 7047, 390, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderMallew" ), new Point3D( 7047, 390, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderAbbein" ), new Point3D( 7043, 390, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderAbbein" ), new Point3D( 7043, 390, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderVicaie" ), new Point3D( 7054, 390, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderVicaie" ), new Point3D( 7054, 390, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderJothan" ), new Point3D( 7056, 383, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderJothan" ), new Point3D( 7056, 383, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "ElderAlethanian" ), new Point3D( 7056, 380, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "ElderAlethanian" ), new Point3D( 7056, 380, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Rebinil" ), new Point3D( 7089, 380, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Rebinil" ), new Point3D( 7089, 380, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Aluniol" ), new Point3D( 7089, 383, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Aluniol" ), new Point3D( 7089, 383, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Olaeni" ), new Point3D( 7080, 363, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Olaeni" ), new Point3D( 7080, 363, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Bolaevin" ), new Point3D( 7066, 351, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Bolaevin" ), new Point3D( 7066, 351, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "LorekeeperAneen" ), new Point3D( 7053, 337, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "LorekeeperAneen" ), new Point3D( 7053, 337, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Daelas" ), new Point3D( 7036, 412, 7 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Daelas" ), new Point3D( 7036, 412, 7 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Alelle" ), new Point3D( 7028, 406, 7 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "Alelle" ), new Point3D( 7028, 406, 7 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "LorekeeperNillaen" ), new Point3D( 7061, 370, 14 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "LorekeeperNillaen" ), new Point3D( 7061, 370, 14 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "LorekeeperRyal" ), new Point3D( 7009, 375, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 5, "LorekeeperRyal" ), new Point3D( 7009, 375, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Braen" ), new Point3D( 7081, 366, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Braen" ), new Point3D( 7081, 366, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderAcob" ), new Point3D( 7037, 387, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "ElderAcob" ), new Point3D( 7037, 387, 0 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "LorekeeperCalendor" ), new Point3D( 7062, 370, 14 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "LorekeeperCalendor" ), new Point3D( 7062, 370, 14 ), Map.Felucca );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "LorekeeperSiarra" ), new Point3D( 7051, 339, 0 ), Map.Trammel );
			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "LorekeeperSiarra" ), new Point3D( 7051, 339, 0 ), Map.Felucca );
		}
	}

	public class TheyllEatAnything : MLQuest
	{
		public TheyllEatAnything()
		{
			Activated = true;
			Title = 1072248; // They'll Eat Anything
			Description = 1072262; // Pork is the fruit of the land!  You can barbeque it, boil it, bake it, sautee it.  There's pork kebabs, pork creole, pork gumbo, pan fried, deep fried, stir fried.  There's apple pork, peppered pork, pork soup, pork salad, pork and potatoes, pork burger, pork sandwich, pork stew, pork chops, pork loins, shredded pork. So, lets get some piggies butchered!
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Pig ) }, "pigs" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}
	}

	public class NoGoodFishStealing : MLQuest
	{
		public NoGoodFishStealing()
		{
			Activated = true;
			Title = 1072251; // No Good, Fish Stealing ...
			Description = 1072265; // Mighty creatures they are, aye.  Fierce and strong, can't blame 'em for wanting to feed themselves an' all. Blame or no, they're eating all the fish up, so they got to go.  Lend a hand?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Walrus ) }, "walruses" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}
	}

	public class AHeroInTheMaking : MLQuest
	{
		public AHeroInTheMaking()
		{
			Activated = true;
			Title = 1072246; // A Hero in the Making
			Description = 1072257; // Are you new around here?  Well, nevermind that.  You look ready for adventure, I can see the gleam of glory in your eyes!  Nothing is more valiant, more noble, more praiseworthy than mongbat slaying.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Mongbat ) }, "mongbats" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}
	}

	public class BullfightingSortOf : MLQuest
	{
		public BullfightingSortOf()
		{
			Activated = true;
			Title = 1072247; // Bullfighting ... Sort Of
			Description = 1072254; // You there! Yes, you.  Listen, I've got a little problem on my hands, but a brave, bold hero like yourself should find it a snap to solve.  Bottom line -- we need some of the bulls in the area culled.  You're welcome to any meat or hides, and of course, I'll give you a nice reward.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Bull ) }, "bulls" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}
	}

	public class AFineFeast : MLQuest
	{
		public AFineFeast()
		{
			Activated = true;
			Title = 1072243; // A Fine Feast.
			Description = 1072261; // Mmm, I do love mutton!  It's slaughtering time again and my usual hirelings haven't turned up.  I've arranged for a butcher to come by and cut everything up but the basic sheep killing part I haven't gotten worked out yet.  Are you up for the task?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Sheep ) }, "sheep" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}
	}

	public class ForcedMigration : MLQuest
	{
		public ForcedMigration()
		{
			Activated = true;
			Title = 1072250; // Forced Migration
			Description = 1072264; // Chirp chirp ... tweet chirp.  Tra la la.  Bloody birds and their blasted noise.  I've tried everything but they just won't stop that infernal clamor.  Return me to blessed silence and I'll make it worth your while.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Bird ) }, "birds" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}
	}

	public class FilthyPests : MLQuest
	{
		public FilthyPests()
		{
			Activated = true;
			Title = 1072242; // Filthy Pests!
			Description = 1072253; // They're everywhere I tell you!  They crawl in the walls, they scurry in the bushes.  Disgusting critters. Say ... I don't suppose you're up for some sewer rat killing?  Sewer rats now, not any other kind of squeaker will do.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Sewerrat ) }, "sewer rats" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}
	}

	public class DeadManWalking : MLQuest
	{
		public DeadManWalking()
		{
			Activated = true;
			Title = 1072983; // Dead Man Walking
			Description = 1073009; // Why?  I ask you why?  They walk around after they're put in the ground.  It's just wrong in so many ways. Put them to proper rest, I beg you.  I'll find some way to pay you for the kindness. Just kill five zombies and five skeletons.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 5, new Type[] { typeof( Zombie ) }, "zombies" ) );
			Objectives.Add( new KillObjective( 5, new Type[] { typeof( Skeleton ) }, "skeletons" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class KingOfBears : MLQuest
	{
		public KingOfBears()
		{
			Activated = true;
			Title = 1072996; // King of Bears
			Description = 1073030; // A pity really.  With the balance of nature awry, we have no choice but to accept the responsibility of making it all right.  It's all a part of the circle of life, after all. So, yes, the grizzly bears are running rampant. There are far too many in the region.  Will you shoulder your obligations as a higher life form?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( GrizzlyBear ) }, "grizzly bears" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class Specimens : MLQuest
	{
		public Specimens()
		{
			Activated = true;
			Title = 1072999; // Specimens
			Description = 1073032; // I admire them, you know.  The solen have their place -- regimented, organized.  They're fascinating to watch with their constant strife between red and black.  I can't help but want to stir things up from time to time.  And that's where you come in.  Kill either twelve red or twelve black solen workers and let's see what happens next!
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			ObjectiveType = ObjectiveType.Any;

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( RedSolenWorker ) }, "red solen workers" ) );
			Objectives.Add( new KillObjective( 12, new Type[] { typeof( BlackSolenWorker ) }, "black solen workers" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class Spirits : MLQuest
	{
		public Spirits()
		{
			Activated = true;
			Title = 1073076; // Spirits
			Description = 1073566; // It is a piteous thing when the dead continue to walk the earth. Restless spirits are known to inhabit these parts, taking the lives of unwary travelers. It is about time a hero put the dead back in their graves. I'm sure such a hero would be justly rewarded.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073586; // The restless spirts still walk -- you must kill 15 of them.

			Objectives.Add( new KillObjective( 15, new Type[] { typeof( Spectre ), typeof( Shade ), typeof( Wraith ) }, "spectres or shades or wraiths" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class RollTheBones : MLQuest
	{
		public RollTheBones()
		{
			Activated = true;
			Title = 1073002; // Roll the Bones
			Description = 1073011; // Why?  I ask you why?  They walk around after they're put in the ground.  It's just wrong in so many ways. Put them to proper rest, I beg you.  I'll find some way to pay you for the kindness. Just kill eight patchwork skeletons.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 8, new Type[] { typeof( PatchworkSkeleton ) }, "patchwork skeletons" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class ItsAGhastlyJob : MLQuest
	{
		public ItsAGhastlyJob()
		{
			Activated = true;
			Title = 1073008; // It's a Ghastly Job
			Description = 1073012; // Why?  I ask you why?  They walk around after they're put in the ground.  It's just wrong in so many ways. Put them to proper rest, I beg you.  I'll find some way to pay you for the kindness. Just kill twelve ghouls.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( Ghoul ) }, "ghouls" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class Troglodytes : MLQuest
	{
		public Troglodytes()
		{
			Activated = true;
			Title = 1074688; // Troglodytes!
			Description = 1074689; // Oh nevermind, you don't look capable of my task afterall.  Haha! What was I thinking - you could never handle killing troglodytes.  It'd be suicide.  What?  I don't know, I don't want to be responsible ... well okay if you're really sure?
			RefusalMessage = 1074690; // Probably the wiser course of action.
			InProgressMessage = 1074691; // You still need to kill those troglodytes, remember?

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( Troglodyte ) }, "troglodytes" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class UnholyKnights : MLQuest
	{
		public UnholyKnights()
		{
			Activated = true;
			Title = 1073075; // Unholy Knights
			Description = 1073565; // Please, hear me kind traveler. You know when a knight falls, sometimes they are cursed to roam the earth as undead mockeries of their former glory? That is too grim a fate for even any knight to suffer! Please, put them out of their misery. I will offer you what payment I can if you will end the torment of these undead wretches.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073585; // Your task is not done. Continue putting the Skeleton and Bone Knights to rest.

			Objectives.Add( new KillObjective( 16, new Type[] { typeof( BoneKnight ), typeof( SkeletalKnight ) }, "bone knights or skeletal knights" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class AFeatherInYerCap : MLQuest
	{
		public AFeatherInYerCap()
		{
			Activated = true;
			Title = 1074738; // A Feather in Yer Cap
			Description = 1074737; // I've seen how you strut about, as if you were something special. I have some news for you, you don't impress me at all. It's not enough to have a fancy hat you know.  That may impress people in the big city, but not here. If you want a reputation you have to climb a mountain, slay some great beast, and then write about it. Trust me, it's a long process.  The first step is doing a great feat. If I were you, I'd go pluck a feather from the harpy Saliva, that would give you a good start.
			RefusalMessage = 1074736; // The path to greatness isn't for everyone obviously.
			InProgressMessage = 1074735; // If you're going to get anywhere in the adventuring game, you have to take some risks.  A harpy, well, it's bad, but it's not a dragon.
			CompletionMessage = 1074734; // The hero returns from the glorious battle and - oh, such a small feather?

			Objectives.Add( new CollectObjective( 1, typeof( SalivasFeather ), "Saliva's Feather" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class ATaleOfTail : MLQuest
	{
		public ATaleOfTail()
		{
			Activated = true;
			Title = 1074726; // A Tale of Tail
			Description = 1074727; // I've heard of you, adventurer.  Your reputation is impressive, and now I'll put it to the test. This is not something I ask lightly, for this task is fraught with danger, but it is vital.  Seek out the vile hydra Abscess, slay it, and return to me with it's tail.
			RefusalMessage = 1074728; // Well, the beast will still be there when you are ready I suppose.
			InProgressMessage = 1074729; // Em, I thought I had explained already.  Abscess, the hydra, you know? Lots of heads but just the one tail. I need the tail. I have my reasons. Go go go.
			CompletionMessage = 1074730; // Ah, the tail.  You did it!  You know the rumours about dried ground hydra tail powder are all true? Thank you so much!

			Objectives.Add( new CollectObjective( 1, typeof( AbscessTail ), "Abscess' Tail" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class ATrogAndHisDog : MLQuest
	{
		public ATrogAndHisDog()
		{
			Activated = true;
			Title = 1074681; // A Trog and His Dog
			Description = 1074680; // I don't know if you can handle it, but I'll give you a go at it. Troglodyte chief - name of Lurg and his mangy wolf pet need killing. Do the deed and I'll reward you.
			RefusalMessage = 1074655; // Perhaps I thought too highly of you.
			InProgressMessage = 1074682; // The trog chief and his mutt should be easy enough to find. Just kill them and report back.  Easy enough.
			CompletionMessage = 1074683; // Not half bad.  Here's your prize.

			Objectives.Add( new KillObjective( 1, new Type[] { typeof( Lurg ) }, "Lurg" ) );
			Objectives.Add( new KillObjective( 1, new Type[] { typeof( Grobu ) }, "Grobu" ) );

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class Overpopulation : MLQuest
	{
		public Overpopulation()
		{
			Activated = true;
			Title = 1072252; // Overpopulation
			Description = 1072267; // I just can't bear it any longer.  Sure, it's my job to thin the deer out so they don't overeat the area and starve themselves come winter time.  Sure, I know we killed off the predators that would do this naturally so now we have to make up for it.  But they're so graceful and innocent.  I just can't do it. Will you?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Hind ) }, "hinds" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}
	}

	public class WildBoarCull : MLQuest
	{
		public WildBoarCull()
		{
			Activated = true;
			Title = 1072245; // Wild Boar Cull
			Description = 1072260; // A pity really.  With the balance of nature awry, we have no choice but to accept the responsibility of making it all right.  It's all a part of the circle of life, after all. So, yes, the boars are running rampant. There are far too many in the region.  Will you shoulder your obligations as a higher life form?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Boar ) }, "boars" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}
	}

	public class ItsElemental : MLQuest
	{
		public ItsElemental()
		{
			Activated = true;
			Title = 1073089; // It's Elemental
			Description = 1073579; // The universe is all about balance my friend. Tip one end, you must balance the other. That's why I must ask you to kill not just one kind of elemental, but three kinds. Snuff out some Fire, douse a few Water, and crush some Earth elementals and I'll pay you for your trouble.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073599; // Four of each, that's all I ask. Water, earth and fire.

			Objectives.Add( new KillObjective( 4, new Type[] { typeof( FireElemental ) }, "fire elementals" ) );
			Objectives.Add( new KillObjective( 4, new Type[] { typeof( WaterElemental ) }, "water elementals" ) );
			Objectives.Add( new KillObjective( 4, new Type[] { typeof( EarthElemental ) }, "earth elementals" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class CircleOfLife : MLQuest
	{
		public CircleOfLife()
		{
			Activated = true;
			Title = 1073656; // Circle of Life
			Description = 1073695; // There's been a bumper crop of evil with the Bog Things in these parts, my friend. Though they are foul creatures, they are also most fecund. Slay one and you make the land more fertile. Even better, slay several and I will give you whatever coin I can spare.
			RefusalMessage = 1073733; // Perhaps you'll change your mind and return at some point.
			InProgressMessage = 1073736; // Continue to seek and kill the Bog Things.

			Objectives.Add( new KillObjective( 8, new Type[] { typeof( BogThing ) }, "bog things" ) );

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class DustToDust : MLQuest
	{
		public DustToDust()
		{
			Activated = true;
			Title = 1073074; // Dust to Dust
			Description = 1073564; // You want to hear about trouble? I got trouble. How's angry piles of granite walking around for trouble? Maybe they don't like the mining, maybe it's the farming. I don't know. All I know is someone's got to turn them back to potting soil. And it'd be worth a pretty penny to the soul that does it.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073584; // You got rocks in your head? I said to kill 12 earth elementals, okay?

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( EarthElemental ) }, "earth elementals" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class CreepyCrawlies : MLQuest
	{
		public CreepyCrawlies()
		{
			Activated = true;
			Title = 1072987; // Creepy Crawlies
			Description = 1073016; // Disgusting!  The way they scuttle on those hairy legs just makes me want to gag. I hate spiders!  Rid the world of twelve and I'll find something nice to give you in thanks.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( GiantSpider ) }, "giant spiders" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class VoraciousPlants : MLQuest
	{
		public VoraciousPlants()
		{
			Activated = true;
			Title = 1073001; // Voracious Plants
			Description = 1073024; // I bet you can't tangle with those nasty plants ... say eight corpsers and two swamp tentacles!  I bet they're too much for you. You may as well confess you can't ...
			RefusalMessage = 1073019; // Hahahaha!  I knew it!
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 8, new Type[] { typeof( Corpser ) }, "corpsers" ) );
			Objectives.Add( new KillObjective( 2, new Type[] { typeof( SwampTentacle ) }, "swamp tentacles" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class GibberJabber : MLQuest
	{
		public GibberJabber()
		{
			Activated = true;
			Title = 1073004; // Gibber Jabber
			Description = 1073024; // I bet you can't kill ... ten gibberlings!  I bet they're too much for you.  You may as well confess you can't ...
			RefusalMessage = 1073019; // Hahahaha!  I knew it!
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Gibberling ) }, "gibberlings" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class AnimatedMonstrosity : MLQuest
	{
		public AnimatedMonstrosity()
		{
			Activated = true;
			Title = 1072990; // Animated Monstrosity
			Description = 1073020; // I bet you can't kill ... say twelve ... flesh golems!  I bet they're too much for you.  You may as well confess you can't ...
			RefusalMessage = 1073019; // Hahahaha!  I knew it!
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( FleshGolem ) }, "flesh golems" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class BirdsOfAFeather : MLQuest
	{
		public BirdsOfAFeather()
		{
			Activated = true;
			Title = 1073007; // Birds of a Feather
			Description = 1073022; // I bet you can't kill ... ten harpies!  I bet they're too much for you.  You may as well confess you can't ...
			RefusalMessage = 1073019; // Hahahaha!  I knew it!
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Harpy ) }, "harpies" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class Frightmares : MLQuest
	{
		public Frightmares()
		{
			Activated = true;
			Title = 1073000; // Frightmares
			Description = 1073036; // I bet you can't handle ten plague spawns!  I bet they're too much for you.  You may as well confess you can't ...
			RefusalMessage = 1073019; // Hahahaha!  I knew it!
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( PlagueSpawn ) }, "plague spawns" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class MoltenReptiles : MLQuest
	{
		public MoltenReptiles()
		{
			Activated = true;
			Title = 1072989; // Molten Reptiles
			Description = 1073018; // I bet you can't kill ... say ten ... lava lizards!  I bet they're too much for you.  You may as well confess you can't ...
			RefusalMessage = 1073019; // Hahahaha!  I knew it!
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( LavaLizard ) }, "lava lizards" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class BloodyNuisance : MLQuest
	{
		public BloodyNuisance()
		{
			Activated = true;
			Title = 1072992; // Bloody Nuisance
			Description = 1073021; // I bet you can't kill ... ten gore fiends!  I bet they're too much for you.  You may as well confess you can't ...
			RefusalMessage = 1073019; // Hahahaha!  I knew it!
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( GoreFiend ) }, "gore fiends" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class BloodSuckers : MLQuest
	{
		public BloodSuckers()
		{
			Activated = true;
			Title = 1072997; // Blood Suckers
			Description = 1073025; // I bet you can't tangle with those bloodsuckers ... say around ten vampire bats!  I bet they're too much for you.  You may as well confess you can't ...
			RefusalMessage = 1073019; // Hahahaha!  I knew it!
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( VampireBat ) }, "vampire bats" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class TheAfterlife : MLQuest
	{
		public TheAfterlife()
		{
			Activated = true;
			Title = 1073073; // The Afterlife
			Description = 1073563; // Nobody told me about the Mummy's Curse. How was I supposed to know you shouldn't disturb the tombs? Oh, sure, now all I hear about is the curse of the vengeful dead. I'll tell you what - make a few of these mummies go away and we'll keep this between you and me.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073583; // Uh, I don't think you're quite done killing Mummies yet.

			Objectives.Add( new KillObjective( 15, new Type[] { typeof( Mummy ) }, "mummies" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class ForkedTongue : MLQuest
	{
		public ForkedTongue()
		{
			Activated = true;
			Title = 1073655; // Forked Tongue
			Description = 1073694; // I must implore you, brave traveler, to do battle with the vile reptiles which haunt these parts. Those hideous abominations, the Ophidians, are a blight across the land. If you were able to put down a host of the scaly warriors, the Knights or the Avengers, I would forever be in your debt.
			RefusalMessage = 1073733; // Perhaps you'll change your mind and return at some point.
			InProgressMessage = 1073735; // Have you killed the Ophidian Knights or Avengers?

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( OphidianKnight ) }, "ophidian avengers or ophidian knight-errants" ) );

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class ImpishDelights : MLQuest
	{
		public ImpishDelights()
		{
			Activated = true;
			Title = 1073077; // Impish Delights
			Description = 1073567; // Imps! Do you hear me? Imps! They're everywhere! They're in everything! Oh, don't be fooled by their size - they vicious little devils! Half-sized evil incarnate, they are! Somebody needs to send them back to where they came from, if you know what I mean.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073587; // Don't let the little devils scare you! You  kill 12 imps - then we'll talk reward.

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( Imp ) }, "imps" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class ThreeWishes : MLQuest
	{
		public ThreeWishes()
		{
			Activated = true;
			Title = 1073660; // Three Wishes
			Description = 1073699; // If I had but one wish, it would be to rid myself of these dread Efreet! Fire and ash, they are cunning and deadly! You look a brave soul - would you be interested in earning a rich reward for slaughtering a few of the smoky devils?
			RefusalMessage = 1073733; // Perhaps you'll change your mind and return at some point.
			InProgressMessage = 1073740; // Those smoky devils, the Efreets, are still about.

			Objectives.Add( new KillObjective( 8, new Type[] { typeof( Efreet ) }, "efreets" ) );

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class EvilEye : MLQuest
	{
		public EvilEye()
		{
			Activated = true;
			Title = 1073084; // Evil Eye
			Description = 1073574; // Kind traveler, hear my plea. You know of the evil orbs? The wrathful eyes? Some call them gazers? They must be a nest nearby, for they are tormenting us poor folk. We need to drive back their numbers. But we are not strong enough to face such horrors ourselves, we need a true hero.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073594; // Have you annihilated a dozen Gazers yet, kind traveler?

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( Gazer ) }, "gazers" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class GargoylesWrath : MLQuest
	{
		public GargoylesWrath()
		{
			Activated = true;
			Title = 1073658; // Gargoyle's Wrath
			Description = 1073697; // It is regretable that the Gargoyles insist upon warring with us. Their Enforcers attack men on sight, despite all efforts at reason. To help maintain order in this region, I have been authorized to encourage bounty hunters to reduce their numbers. Eradicate their number and I will reward you handsomely.
			RefusalMessage = 1073733; // Perhaps you'll change your mind and return at some point.
			InProgressMessage = 1073738; // I won't be able to pay you until you've gotten enough Gargoyle Enforcers.

			Objectives.Add( new KillObjective( 6, new Type[] { typeof( GargoyleEnforcer ) }, "gargoyle enforcers" ) );

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class UndeadMages : MLQuest
	{
		public UndeadMages()
		{
			Activated = true;
			Title = 1073080; // Undead Mages
			Description = 1073570; // Why must the dead plague the living? With their foul necromancy and dark sorceries, the undead menace the countryside. I fear what will happen if no one is strong enough to face these nightmare sorcerers and thin their numbers.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073590; // Surely, a brave soul like yourself can kill 10 Bone Magi and Skeletal Mages?

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( BoneMagi ), typeof( SkeletalMage ) }, "bone mages or skeletal mages" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class FriendlyNeighborhoodSpiderkiller : MLQuest
	{
		public FriendlyNeighborhoodSpiderkiller()
		{
			Activated = true;
			Title = 1073662; // Friendly Neighborhood Spider-killer
			Description = 1073701; // They aren't called Dread Spiders because they're fluffy and cuddly now, are they? No, there's nothing appealing about those wretches so I sure wouldn't lose any sleep if you were to exterminate a few. I'd even part with a generous amount of gold, I would.
			RefusalMessage = 1073733; // Perhaps you'll change your mind and return at some point.
			InProgressMessage = 1073742; // Dread Spiders? I say keep exterminating the arachnid vermin.

			Objectives.Add( new KillObjective( 8, new Type[] { typeof( DreadSpider ) }, "dread spiders" ) );

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class MongbatMenace : MLQuest
	{
		public MongbatMenace()
		{
			Activated = true;
			Title = 1073003; // Mongbat Menace!
			Description = 1073033; // I imagine you don't know about the mongbats.  Well, you may think you do, but I know more than just about anyone. You see they come in two varieties ... the stronger and the weaker.  Either way, they're a menace.  Exterminate ten of the weaker ones and four of the stronger and I'll pay you an honest wage.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Mongbat ) }, "mongbats" ) );
			Objectives.Add( new KillObjective( 4, new Type[] { typeof( GreaterMongbat ) }, "greater mongbats" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class StirringTheNest : MLQuest
	{
		public StirringTheNest()
		{
			Activated = true;
			Title = 1073087; // Stirring the Nest
			Description = 1073577; // Were you the sort of child that enjoyed knocking over anthills? Well, perhaps you'd like to try something a little bigger? There's a Solen nest nearby and I bet if you killed a queen or two, it would be quite the sight to behold.  I'd even pay to see that - what do you say?
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073597; // Dead Solen Queens isn't too much to ask, is it?

			ObjectiveType = ObjectiveType.Any;

			Objectives.Add( new KillObjective( 3, new Type[] { typeof( RedSolenQueen ) }, "red solen queens" ) );
			Objectives.Add( new KillObjective( 3, new Type[] { typeof( BlackSolenQueen ) }, "black solen queens" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class WarriorCaste : MLQuest
	{
		public WarriorCaste()
		{
			Activated = true;
			Title = 1073078; // Warrior Caste
			Description = 1073568; // The Terathan are an aggressive species. Left unchecked, they will swarm across our lands. And where will that leave us? Compost in the hive, that's what! Stop them, stop them cold my friend. Kill their warriors and you'll check their movement, that is certain.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073588; // Unless you kill at least 10 Terathan Warriors, you won't have any impact on their hive.

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( TerathanWarrior ) }, "terathan warriors" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class BigWorms : MLQuest
	{
		public BigWorms()
		{
			Activated = true;
			Title = 1073088; // Big Worms
			Description = 1073578; // It makes no sense! Cold blooded serpents cannot live in the ice! It's a biological impossibility! They are an abomination against reason! Please, I beg you - kill them! Make them disappear for me! Do this and I will reward you.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073598; // You wouldn't try and just pretend you murdered 10 Giant Ice Worms, would you?

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( IceSerpent ) }, "giant ice serpents" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class OrcishElite : MLQuest
	{
		public OrcishElite()
		{
			Activated = true;
			Title = 1073081; // Orcish Elite
			Description = 1073571; // Foul brutes! No one loves an orc, but some of them are worse than the rest. Their Captains and their Bombers, for instance, they're the worst of the lot. Kill a few of those, and the rest are just a rabble. Exterminate a few of them and you'll make the world a sunnier place, don't you know.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073591; // The only good orc is a dead orc - and 4 dead Captains and 6 dead Bombers is even better!

			Objectives.Add( new KillObjective( 6, new Type[] { typeof( OrcBomber ) }, "orc bombers" ) );
			Objectives.Add( new KillObjective( 4, new Type[] { typeof( OrcCaptain ) }, "orc captain" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class ThinningTheHerd : MLQuest
	{
		public ThinningTheHerd()
		{
			Activated = true;
			Title = 1072249; // Thinning the Herd
			Description = 1072263; // Psst!  Hey ... psst!  Listen, I need some help here but it's gotta be hush hush.  I don't want THEM to know I'm onto them.  They watch me.  I've seen them, but they don't know that I know what I know.  You know?  Anyway, I need you to scare them off by killing a few of them.  That'll send a clear message that I won't suffer goats watching me!
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Goat ) }, "goats" ) );

			Rewards.Add( ItemReward.SmallBagOfTrinkets );
		}
	}

	public class Squishy : MLQuest
	{
		public Squishy()
		{
			Activated = true;
			Title = 1072998; // Squishy
			Description = 1073031; // Have you ever seen what a slime can do to good gear?  Well, it's not pretty, let me tell you!  If you take on my task to destroy twelve of them, bear that in mind.  They'll corrode your equipment faster than anything.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( Slime ) }, "slimes" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class OrcSlaying : MLQuest
	{
		public OrcSlaying()
		{
			Activated = true;
			Title = 1072986; // Orc Slaying
			Description = 1073015; // Those green-skinned freaks have run off with more of my livestock.  I want an orc scout killed for each sheep I lost and an orc for each chicken.  So that's four orc scouts and eight orcs I'll pay you to slay.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 8, new Type[] { typeof( Orc ) }, "orcs" ) );
			// TODO: This needs to be orc scouts but they aren't in the SVN
			Objectives.Add( new KillObjective( 4, new Type[] { typeof( OrcishLord ) }, "orcish lords" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class ABigJob : MLQuest
	{
		public ABigJob()
		{
			Activated = true;
			Title = 1072988; // A Big Job
			Description = 1073017; // It's a big job but you look to be just the adventurer to do it! I'm so glad you came by ... I'm paying well for the death of five ogres and five ettins.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 5, new Type[] { typeof( Ogre ) }, "ogres" ) );
			Objectives.Add( new KillObjective( 5, new Type[] { typeof( Ettin ) }, "ettins" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class TrollingForTrolls : MLQuest
	{
		public TrollingForTrolls()
		{
			Activated = true;
			Title = 1072985; // Trolling for Trolls
			Description = 1073014; // They may not be bright, but they're incredibly destructive. Kill off ten trolls and I'll consider it a favor done for me.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Troll ) }, "trolls" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class ColdHearted : MLQuest
	{
		public ColdHearted()
		{
			Activated = true;
			Title = 1072991; // Cold Hearted
			Description = 1073027; // It's a big job but you look to be just the adventurer to do it! I'm so glad you came by ... I'm paying well for the death of six giant ice serpents and six frost spiders.  Hop to it, if you're so inclined.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 6, new Type[] { typeof( IceSerpent ) }, "giant ice serpents" ) );
			Objectives.Add( new KillObjective( 6, new Type[] { typeof( FrostSpider ) }, "frost spiders" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class ForkedTongues : MLQuest
	{
		public ForkedTongues()
		{
			Activated = true;
			Title = 1072984; // Forked Tongues
			Description = 1073013; // You can't trust them, you know.  Lizardmen I mean.  They have forked tongues ... and you know what that means.  Exterminate ten of them and I'll reward you.
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Lizardman ) }, "lizardmen" ) );

			Rewards.Add( ItemReward.BagOfTrinkets );
		}
	}

	public class ShakingThingsUp : MLQuest
	{
		public ShakingThingsUp()
		{
			Activated = true;
			Title = 1073083; // Shaking Things Up
			Description = 1073573; // A Solen hive is a fascinating piece of ecology. It's put together like a finely crafted clock. Who knows what happens if you remove something? So let's find out. Exterminate a few of the warriors and I'll make it worth your while.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073593; // I don't think you've gotten their attention yet -- you need to kill at least 10 Solen Warriors.

			ObjectiveType = ObjectiveType.Any;

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( RedSolenWarrior ) }, "red solen warriors" ) );
			Objectives.Add( new KillObjective( 10, new Type[] { typeof( BlackSolenWarrior ) }, "black solen warriors" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class Arachnophobia : MLQuest
	{
		public Arachnophobia()
		{
			Activated = true;
			Title = 1073079; // Arachnophobia
			Description = 1073569; // I've seen them hiding in their webs among the woods. Glassy eyes, spindly legs, poisonous fangs. Monsters, I say! Deadly horrors, these black widows. Someone must exterminate the abominations! If only I could find a worthy hero for such a task, then I could give them this considerable reward.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073589; // You've got a good start, but to stop the black-eyed fiends, you need to kill a dozen.

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( GiantBlackWidow ) }, "giant black widows" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class MiniSwampThing : MLQuest
	{
		public MiniSwampThing()
		{
			Activated = true;
			Title = 1073072; // Mini Swamp Thing
			Description = 1073562; // Some say killing a boggling brings good luck. I don't place much stock in old wives' tales, but I can say a few dead bogglings would certainly be lucky for me! Help me out and I can reward you for your efforts.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1073582; // Go back and kill all 20 bogglings!

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( Bogling ) }, "boglings" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class ThePerilsOfFarming : MLQuest
	{
		public ThePerilsOfFarming()
		{
			Activated = true;
			Title = 1073664; // The Perils of Farming
			Description = 1073703; // I should be trimming back the vegetation here, but something nasty has taken root. Viscious vines I can't go near. If there's any hope of getting things under control, some one's going to need to destroy a few of those Whipping Vines. Someone strong and fast and tough.
			RefusalMessage = 1073733; // Perhaps you'll change your mind and return at some point.
			InProgressMessage = 1073744; // How are farmers supposed to work with these Whipping Vines around?

			Objectives.Add( new KillObjective( 15, new Type[] { typeof( WhippingVine ) }, "whipping vines" ) );

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class IndustriousAsAnAntLion : MLQuest
	{
		public IndustriousAsAnAntLion()
		{
			Activated = true;
			Title = 1073665; // Industrious as an Ant Lion
			Description = 1073704; // Ants are industrious and Lions are noble so who'd think an Ant Lion would be such a problem? The Ant Lion's have been causing mindless destruction in these parts. I suppose it's just how ants are. But I need you to help eliminate the infestation. Would you be willing to help for a bit of reward?
			RefusalMessage = 1073733; // Perhaps you'll change your mind and return at some point.
			InProgressMessage = 1073745; // Please, rid us of the Ant Lion infestation.

			Objectives.Add( new KillObjective( 12, new Type[] { typeof( AntLion ) }, "ant lions" ) );

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class UnholyConstruct : MLQuest
	{
		public UnholyConstruct()
		{
			Activated = true;
			Title = 1073666; // Unholy Construct
			Description = 1073705; // They're unholy, I say. Golems, a walking mockery of all life, born of blackest magic. They're not truly alive, so destroying them isn't a crime, it's a service. A service I will gladly pay for.
			RefusalMessage = 1073733; // Perhaps you'll change your mind and return at some point.
			InProgressMessage = 1073746; // The unholy brutes, the Golems, must be smited!
			CompletionMessage = 1073787; // Reduced those Golems to component parts? Good, then -- you deserve this reward!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Golem ) }, "golems" ) );

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class AChillInTheAir : MLQuest
	{
		public AChillInTheAir()
		{
			Activated = true;
			Title = 1073663; // A Chill in the Air
			Description = 1073702; // Feel that chill in the air? It means an icy death for the unwary, for deadly Ice Elementals are about. Who knows what magic summoned them, what's important now is getting rid of them. I don't have much, but I'll give all I can if you'd only stop the cold-hearted monsters.
			RefusalMessage = 1073733; // Perhaps you'll change your mind and return at some point.
			InProgressMessage = 1073746; // The chill won't lift until you eradicate a few Ice Elemenals.

			Objectives.Add( new KillObjective( 15, new Type[] { typeof( IceElemental ) }, "ice elementals" ) );

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class TheKingOfClothing : MLQuest
	{
		public TheKingOfClothing()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073902; // The King of Clothing
			Description = 1074092; // I have heard noble tales of a fine and proud human garment. An article of clothing fit for both man and god alike. It is called a "kilt" I believe? Could you fetch for me some of these kilts so I that I might revel in their majesty and glory?
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073948; // I will be in your debt if you bring me kilts.
			CompletionMessage = 1073974; // I say truly - that is a magnificent garment! You have more than earned a reward.

			Objectives.Add( new CollectObjective( 10, typeof( Kilt ), 1025431 ) ); // kilt

			Rewards.Add( ItemReward.TailorSatchel );
		}
	}

	public class ThePuffyShirt : MLQuest
	{
		public ThePuffyShirt()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073903; // The Puffy Shirt
			Description = 1074093; // We elves believe that beauty is expressed in all things, including the garments we wear. I wish to understand more about human aesthetics, so please kind traveler - could you bring to me magnificent examples of human fancy shirts? For my thanks, I could teach you more about the beauty of elven vestements.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073949; // I will be in your debt if you bring me fancy shirts.
			CompletionMessage = 1073973; // I appreciate your service. Now, see what elven hands can create.

			Objectives.Add( new CollectObjective( 10, typeof( FancyShirt ), 1027933 ) ); // fancy shirt

			Rewards.Add( ItemReward.TailorSatchel );
		}
	}

	public class FromTheGaultierCollection : MLQuest
	{
		public FromTheGaultierCollection()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073905; // From the Gaultier Collection
			Description = 1074095; // It is my understanding, the females of humankind actually wear on certain occasions a studded bustier? This is not simply a fanciful tale? Remarkable! It sounds hideously uncomfortable as well as ludicrously impracticle. But perhaps, I simply do not understand the nuances of human clothing. Perhaps, I need to see such a studded bustier for myself?
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073951; // I will be in your debt if you bring me studded bustiers.
			CompletionMessage = 1073976; // Truly, it is worse than I feared. Still, I appreciate your efforts on my behalf.

			Objectives.Add( new CollectObjective( 10, typeof( StuddedBustierArms ), 1027180 ) ); // studded bustier

			Rewards.Add( ItemReward.TailorSatchel );
		}
	}

	public class HauteCouture : MLQuest
	{
		public HauteCouture()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073901; // Hâute Couture
			Description = 1074091; // Most human apparel is interesting to elven eyes. But there is one garment - the flower garland - which sounds very elven indeed. Could I see how a human crafts such an object of beauty? In exchange, I could share with you the wonders of elven garments.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073947; // I will be in your debt if you bring me flower garlands.
			CompletionMessage = 1073973; // I appreciate your service. Now, see what elven hands can create.

			Objectives.Add( new CollectObjective( 10, typeof( FlowerGarland ), 1028965 ) ); // flower garland

			Rewards.Add( ItemReward.TailorSatchel );
		}
	}

	public class TheSongOfTheWind : MLQuest
	{
		public TheSongOfTheWind()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073910; // The Song of the Wind
			Description = 1074100; // To give voice to the passing wind, this is an idea worthy of an elf! Friend, bring me some of the amazing fancy wind chimes so that I may listen to the song of the passing breeze. Do this, and I will share with you treasured elven secrets.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073956; // I will be in your debt if you bring me fancy wind chimes.
			CompletionMessage = 1073980; // Such a delightful sound, I think I shall never tire of it.

			Objectives.Add( new CollectObjective( 10, typeof( FancyWindChimes ), "fancy wind chimes" ) );

			Rewards.Add( ItemReward.TinkerSatchel );
		}
	}

	public class BeerGoggles : MLQuest
	{
		public BeerGoggles()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073895; // Beer Goggles
			Description = 1074085; // Oh, the deviltry! Why would humans lock their precious liquors inside a wooden coffin? I understand I need a "keg tap" to access the golden brew within such a wooden abomination. Perhaps, if you could bring me such a tap, we could share a drink and I could teach you.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073941; // I will be in your debt if you bring me barrel taps.
			CompletionMessage = 1073971; // My thanks for your service.  Here is something for you to enjoy.

			Objectives.Add( new CollectObjective( 25, typeof( BarrelTap ), 1024100 ) ); // barrel tap

			Rewards.Add( ItemReward.TinkerSatchel );
		}
	}

	public class MessageInABottleQuest : MLQuest
	{
		public MessageInABottleQuest()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073894; // Message in a Bottle
			Description = 1074084; // We elves are interested in trading our wines with humans but we understand human usually trade such brew in strange transparent bottles. If you could provide some of these empty glass bottles, I might engage in a bit of elven winemaking.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073940; // I will be in your debt if you bring me empty bottles.
			CompletionMessage = 1073971; // My thanks for your service.  Here is something for you to enjoy.

			Objectives.Add( new CollectObjective( 50, typeof( Bottle ), 1023854 ) ); // empty bottle

			Rewards.Add( ItemReward.TinkerSatchel );
		}
	}

	public class NecessitysMother : MLQuest
	{
		public NecessitysMother()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073906; // Necessity's Mother
			Description = 1074096; // What a thing, this human need to tinker. It seems there is no end to what might be produced with a set of Tinker's Tools. Who knows what an elf might build with some? Could you obtain some tinker's tools and bring them to me? In exchange, I offer you elven lore and knowledge.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073952; // I will be in your debt if you bring me tinker's tools.
			CompletionMessage = 1073977; // Now, I shall see what an elf can invent!

			Objectives.Add( new CollectObjective( 10, typeof( TinkerTools ), 1027868 ) ); // tinker's tools

			Rewards.Add( ItemReward.TinkerSatchel );
		}
	}

	public class TickTock : MLQuest
	{
		public TickTock()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073907; // Tick Tock
			Description = 1074097; // Elves find it remarkable the human preoccupation with the passage of time. To have built instruments to try and capture time -- it is a fascinating notion. I would like to see how a clock is put together. Maybe you could provide some clocks for my experimentation?
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073953; // I will be in your debt if you bring me clocks.
			CompletionMessage = 1073978; // Enjoy my thanks for your service.

			Objectives.Add( new CollectObjective( 10, typeof( Clock ), 1024171 ) ); // clock

			Rewards.Add( ItemReward.TinkerSatchel );
		}
	}

	public class ReptilianDentist : MLQuest
	{
		public ReptilianDentist()
		{
			Activated = true;
			Title = 1074280; // Reptilian Dentist
			Description = 1074710; // I'm working on a striking necklace -- something really unique -- and I know just what I need to finish it up.  A huge fang!  Won't that catch the eye?  I would like to employ you to find me such an item, perhaps a snake would make the ideal donor.  I'll make it worth your while, of course.
			RefusalMessage = 1074723; // I understand.  I don't like snakes much either.  They're so creepy.
			InProgressMessage = 1074722; // Those really big snakes like swamps, I've heard.  You might try the blighted grove.
			CompletionMessage = 1074721; // Do you have it?  *gasp* What a tooth!  Here … I must get right to work.

			Objectives.Add( new CollectObjective( 1, typeof( CoilsFang ), "coil's fang" ) );

			Rewards.Add( ItemReward.TinkerSatchel );
		}
	}

	public class StopHarpingOnMe : MLQuest
	{
		public StopHarpingOnMe()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073881; // Stop Harping on Me
			Description = 1074071; // Humans artistry can be a remarkable thing. For instance, I have heard of a wonderful instrument which creates the most melodious of music. A lap harp. I would be ever so grateful if I could examine one in person.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073927; // I will be in your debt if you bring me lap harp.
			CompletionMessage = 1073969; // My thanks for your service. Now, I will show you something of elven carpentry.

			Objectives.Add( new CollectObjective( 20, typeof( LapHarp ), 1023762 ) ); // lap harp

			Rewards.Add( ItemReward.CarpentrySatchel );
		}
	}

	public class TheFarEye : MLQuest
	{
		public TheFarEye()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073908; // The Far Eye
			Description = 1074098; // The wonders of human invention! Turning sand and metal into a far-seeing eye! This is something I must experience for myself. Bring me some of these spyglasses friend human.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073954; // I will be in your debt if you bring me spyglasses.
			CompletionMessage = 1073978; // Enjoy my thanks for your service.

			Objectives.Add( new CollectObjective( 20, typeof( Spyglass ), 1025365 ) ); // spyglass

			Rewards.Add( ItemReward.TinkerSatchel );
		}
	}

	public class LethalDarts : MLQuest
	{
		public LethalDarts()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073876; // Lethal Darts
			Description = 1074066; // We elves are no strangers to archery but I would be interested in learning whether there is anything to learn from the human approach. I would gladly trade you something I have if you could teach me of the deadly crossbow bolt.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073922; // I will be in your debt if you bring me crossbow bolts.
			CompletionMessage = 1073968; // My thanks for your service. Now, I shall teach you of elven archery.
			CompletionNotice = CompletionNoticeCraft;

			Objectives.Add( new CollectObjective( 10, typeof( Bolt ), 1027163 ) ); // crossbow bolt

			Rewards.Add( ItemReward.FletchingSatchel );
		}
	}

	public class ASimpleBow : MLQuest
	{
		public ASimpleBow()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073877; // A Simple Bow
			Description = 1074067; // I wish to try a bow crafted in the human style. Is it possible for you to bring me such a weapon? I would be happy to return this favor.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073923; // I will be in your debt if you bring me bows.
			CompletionMessage = 1073968; // My thanks for your service. Now, I shall teach you of elven archery.
			CompletionNotice = CompletionNoticeCraft;

			Objectives.Add( new CollectObjective( 10, typeof( Bow ), 1025041 ) ); // bow

			Rewards.Add( ItemReward.FletchingSatchel );
		}
	}

	public class IngeniousArcheryPartOne : MLQuest
	{
		public IngeniousArcheryPartOne()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073878; // Ingenious Archery, Part I
			Description = 1074068; // I have heard of a curious type of bow, you call it a "crossbow". It sounds fascinating and I would very much like to examine one closely. Would you be able to obtain such an instrument for me?
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073924; // I will be in your debt if you bring me crossbows.
			CompletionMessage = 1073968; // My thanks for your service. Now, I shall teach you of elven archery.
			CompletionNotice = CompletionNoticeCraft;

			Objectives.Add( new CollectObjective( 10, typeof( Crossbow ), 1023919 ) ); // crossbow

			Rewards.Add( ItemReward.FletchingSatchel );
		}
	}

	public class IngeniousArcheryPartTwo : MLQuest
	{
		public IngeniousArcheryPartTwo()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073879; // Ingenious Archery, Part II
			Description = 1074069; // These human "crossbows" are complex and clever. The "heavy crossbow" is a remarkable instrument of war. I am interested in seeing one up close, if you could arrange for one to make its way to my hands.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073925; // I will be in your debt if you bring me heavy crossbows.
			CompletionMessage = 1073968; // My thanks for your service. Now, I shall teach you of elven archery.
			CompletionNotice = CompletionNoticeCraft;

			Objectives.Add( new CollectObjective( 8, typeof( HeavyCrossbow ), 1025116 ) ); // heavy crossbow

			Rewards.Add( ItemReward.FletchingSatchel );
		}
	}

	public class IngeniousArcheryPartThree : MLQuest
	{
		public IngeniousArcheryPartThree()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073880; // Ingenious Archery, Part III
			Description = 1074070; // My friend, I am in search of a device, a instrument of remarkable human ingenuity. It is a repeating crossbow. If you were to obtain such a device, I would gladly reveal to you some of the secrets of elven craftsmanship.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073926; // I will be in your debt if you bring me repeating crossbows.
			CompletionMessage = 1073968; // My thanks for your service. Now, I shall teach you of elven archery.
			CompletionNotice = CompletionNoticeCraft;

			Objectives.Add( new CollectObjective( 10, typeof( RepeatingCrossbow ), 1029923 ) ); // repeating crossbow

			Rewards.Add( ItemReward.FletchingSatchel );
		}
	}

	public class ScaleArmor : MLQuest
	{
		public ScaleArmor()
		{
			Activated = true;
			Title = 1074711; // Scale Armor
			Description = 1074712; // Here's what I need ... there are some creatures called hydra, fearsome beasts, whose scales are especially suitable for a new sort of armor that I'm developing.  I need a few such pieces and then some supple alligator skin for the backing.  I'm going to need a really large piece that's shaped just right ... the tail I think would do nicely.  I appreciate your help.
			RefusalMessage = 1073580; // I hope you'll reconsider. Until then, farwell.
			InProgressMessage = 1074724; // Hydras have been spotted in the Blighted Grove.  You won't get those scales without getting your feet wet, I'm afraid.
			CompletionMessage = 1074725; // I can't wait to get to work now that you've returned with my scales.

			Objectives.Add( new CollectObjective( 1, typeof( ThrashersTail ), "Thrasher's Tail" ) );
			Objectives.Add( new CollectObjective( 10, typeof( HydraScale ), "Hydra Scales" ) );

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	public class CutsBothWays : MLQuest
	{
		public CutsBothWays()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073913; // Cuts Both Ways
			Description = 1074103; // What would you say is a typical human instrument of war? Is a broadsword a typical example? I wish to see more of such human weapons, so I would gladly trade elven knowledge for human steel.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073959; // I will be in your debt if you bring me broadswords.
			CompletionMessage = 1073978; // Enjoy my thanks for your service.

			Objectives.Add( new CollectObjective( 12, typeof( Broadsword ), 1023934 ) ); // broadsword

			Rewards.Add( ItemReward.BlacksmithSatchel );
		}
	}

	public class DragonProtection : MLQuest
	{
		public DragonProtection()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073915; // Dragon Protection
			Description = 1074105; // Mankind, I am told, knows how to take the scales of a terrible dragon and forge them into powerful armor. Such a feat of craftsmanship! I would give anything to view such a creation - I would even teach some of the prize secrets of the elven people.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073961; // I will be in your debt if you bring me dragon armor.
			CompletionMessage = 1073978; // Enjoy my thanks for your service.

			Objectives.Add( new CollectObjective( 10, typeof( DragonHelm ), 1029797 ) ); // dragon helm

			Rewards.Add( ItemReward.BlacksmithSatchel );
		}
	}

	public class NothingFancy : MLQuest
	{
		public NothingFancy()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073911; // Nothing Fancy
			Description = 1074101; // I am curious to see the results of human blacksmithing. To examine the care and quality of a simple item. Perhaps, a simple bascinet helmet? Yes, indeed -- if you could bring to me some bascinet helmets, I would demonstrate my gratitude.
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073957; // I will be in your debt if you bring me bascinets.
			CompletionMessage = 1073978; // Enjoy my thanks for your service.

			Objectives.Add( new CollectObjective( 15, typeof( Bascinet ), 1025132 ) ); // bascinet

			Rewards.Add( ItemReward.BlacksmithSatchel );
		}
	}

	public class TheBulwark : MLQuest
	{
		public TheBulwark()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073912; // The Bulwark
			Description = 1074102; // The clank of human iron and steel is strange to elven ears. For instance, the metallic heater shield which human warriors carry into battle. It is odd to an elf, but nevertheless intriguing. Tell me friend, could you bring me such an example of human smithing skill?
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073958; // I will be in your debt if you bring me heater shields.
			CompletionMessage = 1073978; // Enjoy my thanks for your service.

			Objectives.Add( new CollectObjective( 10, typeof( HeaterShield ), 1027030 ) ); // heater shield

			Rewards.Add( ItemReward.BlacksmithSatchel );
		}
	}

	public class ArchSupport : MLQuest
	{
		public ArchSupport()
		{
			Activated = true;
			HasRestartDelay = true;
			Title = 1073882; // Arch Support
			Description = 1074072; // How clever humans are - to understand the need of feet to rest from time to time!  Imagine creating a special stool just for weary toes.  I would like to examine and learn the secret of their making.  Would you bring me some foot stools to examine?
			RefusalMessage = 1073921; // I will patiently await your reconsideration.
			InProgressMessage = 1073928; // I will be in your debt if you bring me foot stools.
			CompletionMessage = 1073969; // My thanks for your service. Now, I will show you something of elven carpentry.

			Objectives.Add( new CollectObjective( 10, typeof( FootStool ), 1022910 ) ); // foot stool

			Rewards.Add( ItemReward.CarpentrySatchel );
		}
	}

	public class ParoxysmusSuccubi : MLQuest
	{
		public ParoxysmusSuccubi()
		{
			Activated = true;
			Title = 1073067; // Paroxysmus' Succubi
			Description = 1074696; // The succubi that have congregated within the sinkhole to worship Paroxysmus pose a tremendous danger. Will you enter the lair and see to their destruction?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 3, new Type[] { typeof( Succubus ) }, "succubi", new QuestArea( 1074806, "The Palace of Paroxysmus" ) ) ); // The Palace of Paroxysmus

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class ParoxysmusMoloch : MLQuest
	{
		public ParoxysmusMoloch()
		{
			Activated = true;
			Title = 1073068; // Paroxysmus' Moloch
			Description = 1074695; // The moloch daemons that have congregated to worship Paroxysmus pose a tremendous danger. Will you enter the lair and see to their destruction?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 3, new Type[] { typeof( Moloch ) }, "molochs", new QuestArea( 1074806, "The Palace of Paroxysmus" ) ) ); // The Palace of Paroxysmus

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class ParoxysmusDaemons : MLQuest
	{
		public ParoxysmusDaemons()
		{
			Activated = true;
			Title = 1073069; // Paroxysmus' Daemons
			Description = 1074694; // The daemons that have congregated to worship Paroxysmus pose a tremendous danger. Will you enter the lair and see to their destruction?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( Daemon ) }, "daemons", new QuestArea( 1074806, "The Palace of Paroxysmus" ) ) ); // The Palace of Paroxysmus

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class ParoxysmusArcaneDaemons : MLQuest
	{
		public ParoxysmusArcaneDaemons()
		{
			Activated = true;
			Title = 1073070; // Paroxysmus' Arcane Daemons
			Description = 1074697; // The arcane daemons that worship Paroxysmus pose a tremendous danger. Will you enter the lair and see to their destruction?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( ArcaneDaemon ) }, "arcane daemons", new QuestArea( 1074806, "The Palace of Paroxysmus" ) ) ); // The Palace of Paroxysmus

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class CausticCombo : MLQuest
	{
		public CausticCombo()
		{
			Activated = true;
			Title = 1073062; // Caustic Combo
			Description = 1074693; // Vile creatures have exited the sinkhole and begun terrorizing the surrounding area.  The demons are bad enough, but the elementals are an abomination, their poisons seeping into the fertile ground here.  Will you enter the sinkhole and put a stop to their depredations?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 3, new Type[] { typeof( PoisonElemental ) }, "poison elementals", new QuestArea( 1074806, "The Palace of Paroxysmus" ) ) ); // The Palace of Paroxysmus
			Objectives.Add( new KillObjective( 6, new Type[] { typeof( AcidElemental ) }, "acid elementals", new QuestArea( 1074806, "The Palace of Paroxysmus" ) ) ); // The Palace of Paroxysmus

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class PlagueLord : MLQuest
	{
		public PlagueLord()
		{
			Activated = true;
			Title = 1073061; // Plague Lord
			Description = 1074692; // Some of the most horrific creatures have slithered out of the sinkhole there and begun terrorizing the surrounding area. The plague creatures are one of the most destruction of the minions of Paroxysmus.  Are you willing to do something about them?
			RefusalMessage = 1072270; // Well, okay. But if you decide you are up for it after all, c'mon back and see me.
			InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!

			Objectives.Add( new KillObjective( 10, new Type[] { typeof( PlagueSpawn ) }, "plague spawns", new QuestArea( 1074806, "The Palace of Paroxysmus" ) ) ); // The Palace of Paroxysmus
			Objectives.Add( new KillObjective( 3, new Type[] { typeof( PlagueBeast ) }, "plague beasts", new QuestArea( 1074806, "The Palace of Paroxysmus" ) ) ); // The Palace of Paroxysmus
			Objectives.Add( new KillObjective( 1, new Type[] { typeof( PlagueBeastLord ) }, "plague beast lord", new QuestArea( 1074806, "The Palace of Paroxysmus" ) ) ); // The Palace of Paroxysmus

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class GlassyFoe : MLQuest
	{
		public GlassyFoe()
		{
			Activated = true;
			Title = 1073055; // Glassy Foe
			Description = 1074669; // Good, you're here.  The presence of a twisted creature deep under the earth near Nu'Jelm has corrupted the natural growth of crystals in that region.  They've become infused with the twisting energy - they've come to a sort of life.  This is an abomination that festers within Sosaria.  You must eradicate the crystal lattice seekers.
			RefusalMessage = 1074671; // These abominations must not be permitted to fester!
			InProgressMessage = 1074672; // You must not waste time. Do not suffer these crystalline abominations to live.
			CompletionMessage = 1074673; // You have done well.  Enjoy this reward.

			Objectives.Add( new KillObjective( 5, new Type[] { typeof( CrystalLatticeSeeker ) }, "crystal lattice seekers", new QuestArea( 1074805, "The Prism of Light" ) ) ); // The Prism of Light

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class DaemonicPrism : MLQuest
	{
		public DaemonicPrism()
		{
			Activated = true;
			Title = 1073053; // Daemonic Prism
			Description = 1074668; // Good, you're here.  The presence of a twisted creature deep under the earth near Nu'Jelm has corrupted the natural growth of crystals in that region.  They've become infused with the twisting energy - they've come to a sort of life.  This is an abomination that festers within Sosaria. You must eradicate the crystal daemons.
			RefusalMessage = 1074671; // These abominations must not be permitted to fester!
			InProgressMessage = 1074672; // You must not waste time. Do not suffer these crystalline abominations to live.
			CompletionMessage = 1074673; // You have done well.  Enjoy this reward.

			Objectives.Add( new KillObjective( 3, new Type[] { typeof( CrystalDaemon ) }, "crystal daemons", new QuestArea( 1074805, "The Prism of Light" ) ) ); // The Prism of Light

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	public class Hailstorm : MLQuest
	{
		public Hailstorm()
		{
			Activated = true;
			Title = 1073057; // Hailstorm
			Description = 1074670; // Good, you're here.  The presence of a twisted creature deep under the earth near Nu'Jelm has corrupted the natural growth of crystals in that region.  They've become infused with the twisting energy - they've come to a sort of life.  This is an abomination that festers within Sosaria.  You must eradicate the crystal vortices.
			RefusalMessage = 1074671; // These abominations must not be permitted to fester!
			InProgressMessage = 1074672; // You must not waste time. Do not suffer these crystalline abominations to live.
			CompletionMessage = 1074673; // You have done well.  Enjoy this reward.

			Objectives.Add( new KillObjective( 8, new Type[] { typeof( CrystalVortex ) }, "crystal vortices", new QuestArea( 1074805, "The Prism of Light" ) ) ); // The Prism of Light

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}

	/* TODO: Uncomment when Crystal Hydra is added
	public class HowManyHeads : MLQuest
	{
		public HowManyHeads()
		{
			Activated = true;
			Title = 1073050; // How Many Heads?
			Description = 1074674; // Good, you're here.  The presence of a twisted creature deep under the earth near Nu'Jelm has corrupted the natural growth of crystals in that region.  They've become infused with the twisting energy - they've come to a sort of life.  This is an abomination that festers within Sosaria. You must eradicate the crystal hydras.
			RefusalMessage = 1074671; // These abominations must not be permitted to fester!
			InProgressMessage = 1074672; // You must not waste time. Do not suffer these crystalline abominations to live.
			CompletionMessage = 1074673; // You have done well.  Enjoy this reward.

			Objectives.Add( new KillObjective( 3, new Type[] { typeof( CrystalHydra ) }, "crystal hydras", new QuestArea( 1074805, "The Prism of Light" ) ) ); // The Prism of Light

			Rewards.Add( ItemReward.LargeBagOfTreasure );
		}
	}
	*/

	/* TODO: Uncomment when Dreadhorn is added
	public class DreadhornQuest : MLQuest
	{
		public DreadhornQuest()
		{
			Activated = true;
			Title = 1074645; // Dreadhorn
			Description = 1074646; // Can you comprehend it? I cannot, I confess.  The most pristine and perfect Lord of Sosaria has fallen prey to the blight.  From the depths of my heart I mourn his corruption; my thoughts are filled with pity for this glorious creature now tainted.  And my blood boils with fury at those responsible for the innocent creature's undoing.  Will you find Dread Horn, as he is now called, and free him from this misery?
			RefusalMessage = 1074647; // How can you not feel as I do?
			InProgressMessage = 1074648; // The lush and fertile land where Dread Horn now lives is twisted and tainted, a result of his corruption.  The fey folk have sealed the land off through their magics, but you can enter through an enchanted mushroom fairy circle.
			CompletionMessage = 1074649; // Thank you.  I haven't the words to express my gratitude.

			Objectives.Add( new KillObjective( 1, new Type[] { typeof( DreadHorn ) }, "dread horn" ) );

			Rewards.Add( ItemReward.RewardStrongbox );
		}
	}
	*/

	/* TODO: Uncomment when SerpentsFangHighExecutioner, TigersClawThief and DragonsFlameGrandMage are added
	public class NewLeadership : MLQuest
	{
		public NewLeadership()
		{
			Activated = true;
			Title = 1072905; // New Leadership
			Description = 1072963; // I have a task for you ... adventurer.  Will you risk all to win great renown?  The Black Order is organized into three sects, each with their own speciality.  The Dragon's Flame serves the will of the Grand Mage, the Tiger's Claw answers to the Master Thief, and the Serpent's Fang kills at the direction of the High Executioner.  Slay all three and you will strike the order a devastating blow!
			RefusalMessage = 1072973; // I do not fault your decision.
			InProgressMessage = 1072974; // Once you gain entrance into The Citadel, you will need to move cautiously to find the sect leaders.

			Objectives.Add( new KillObjective( 1, new Type[] { typeof( SerpentsFangHighExecutioner ) }, "serpent's fang high executioner", new QuestArea( 1074804, "The Citadel" ) ) ); // The Citadel
			Objectives.Add( new KillObjective( 1, new Type[] { typeof( TigersClawThief ) }, "tiger's claw thief", new QuestArea( 1074804, "The Citadel" ) ) ); // The Citadel
			Objectives.Add( new KillObjective( 1, new Type[] { typeof( DragonsFlameGrandMage ) }, "dragon's flame mage", new QuestArea( 1074804, "The Citadel" ) ) ); // The Citadel

			Rewards.Add( ItemReward.RewardStrongbox );
		}
	}
	*/

	/* TODO: Uncomment when SerpentsFangAssassin is added
	public class ExAssassins : MLQuest
	{
		public ExAssassins()
		{
			Activated = true;
			Title = 1072917; // Ex-Assassins
			Description = 1072969; // The Serpent's Fang sect members have gone too far! Express to them my displeasure by slaying ten of them. But remember, I do not condone war on women, so I will only accept the deaths of men, human and elf.
			RefusalMessage = 1072979; // As you wish.
			InProgressMessage = 1072980; // The Black Order's fortress home is well hidden.  Legend has it that a humble fishing village disguises the magical portal.

			// TODO: This has to be MALES only!
			Objectives.Add( new KillObjective( 10, new Type[] { typeof( SerpentsFangAssassin ) }, "male serpent's fang assassins", new QuestArea( 1074804, "The Citadel" ) ) ); // The Citadel

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}
	*/

	/* TODO: Uncomment when DragonsFlameMage is added
	public class ExtinguishingTheFlame : MLQuest
	{
		public ExtinguishingTheFlame()
		{
			Activated = true;
			Title = 1072911; // Extinguishing the Flame
			Description = 1072966; // The Dragon's Flame sect members have gone too far! Express to them my displeasure by slaying ten of them. But remember, I do not condone war on women, so I will only accept the deaths of men, human or elf.  Either race will do, I care not for the shape of their ears. Yes, this action will properly make clear my disapproval and has a pleasing harmony.
			RefusalMessage = 1072979; // As you wish.
			InProgressMessage = 1072980; // The Black Order's fortress home is well hidden.  Legend has it that a humble fishing village disguises the magical portal.

			// TODO: This has to be MALES only!
			Objectives.Add( new KillObjective( 10, new Type[] { typeof( DragonsFlameMage ) }, "male dragon's flame mages", new QuestArea( 1074804, "The Citadel" ) ) ); // The Citadel

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}
	*/

	public class DeathToTheNinja : MLQuest
	{
		public DeathToTheNinja()
		{
			Activated = true;
			Title = 1072913; // Death to the Ninja!
			Description = 1072966; // I wish to make a statement of censure against the elite ninjas of the Black Order.  Deliver, in the strongest manner, my disdain.  But do not make war on women, even those that take arms against you.  It is not ... fitting.
			RefusalMessage = 1072979; // As you wish.
			InProgressMessage = 1072980; // The Black Order's fortress home is well hidden.  Legend has it that a humble fishing village disguises the magical portal.

			// TODO: Verify that this has to be males only (as per the description)
			Objectives.Add( new KillObjective( 10, new Type[] { typeof( EliteNinja ) }, "elite ninjas", new QuestArea( 1074804, "The Citadel" ) ) ); // The Citadel

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}

	/* TODO: Uncomment when TigersClawThief is added
	public class CrimeAndPunishment : MLQuest
	{
		public CrimeAndPunishment()
		{
			Activated = true;
			Title = 1072914; // Crime and Punishment
			Description = 1072968; // The Tiger's Claw sect members have gone too far! Express to them my displeasure by slaying ten of them. But remember, I do not condone war on women, so I will only accept the deaths of men, human and elf.
			RefusalMessage = 1072979; // As you wish.
			InProgressMessage = 1072980; // The Black Order's fortress home is well hidden.  Legend has it that a humble fishing village disguises the magical portal.

			// TODO: This has to be MALES only!
			Objectives.Add( new KillObjective( 10, new Type[] { typeof( TigersClawThief ) }, "male tiger's claw thieves", new QuestArea( 1074804, "The Citadel" ) ) ); // The Citadel

			Rewards.Add( ItemReward.BagOfTreasure );
		}
	}
	*/

	/* TODO: Uncomment when ShimmeringEffusion is added
	public class AllThatGlittersIsNotGood : MLQuest
	{
		public AllThatGlittersIsNotGood()
		{
			Activated = true;
			Title = 1073048; // All That Glitters is Not Good
			Description = 1074654; // The most incredible tale has reached my ears!  Deep within the bowels of Sosaria, somewhere under the city of Nu'Jelm, a twisted creature feeds.  What created this abomination, no one knows ... though there is some speculation that the fumbling initial efforts to open the portal to The Heartwood, brought it into existence.  Regardless of it's origin, it must be destroyed before it damages Sosaria.  Will you undertake this quest?
			RefusalMessage = 1074655; // Perhaps I thought too highly of you.
			InProgressMessage = 1074656; // An explorer discovered the cave system under Nu'Jelm.  He made multiple trips into the place bringing back fascinating crystals and artifacts that suggested the hollow place in Sosaria was inhabited by other creatures at some point.  You'll need to follow in his footsteps to find this abomination and destroy it.
			CompletionMessage = 1074657; // I am overjoyed with your efforts!  Your devotion to Sosaria is noted and appreciated.

			Objectives.Add( new KillObjective( 1, new Type[] { typeof( ShimmeringEffusion ) }, "shimmering effusion" ) );

			Rewards.Add( ItemReward.RewardStrongbox );
		}
	}
	*/

	#endregion

	#region Mobiles

	[QuesterName( "Saril (The Heartwood)" )]
	public class Saril : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074186, // Come here, I have a task.
				1074183 // You there! I have a job for you.
			) );
		}

		[Constructable]
		public Saril()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Saril";
			Title = "the guard";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots() );
			AddItem( new WoodlandLegs() );
			AddItem( new WoodlandArms() );
			AddItem( new WoodlandBelt() );
			AddItem( new WingedHelm() );
			AddItem( new FemaleElvenPlateChest() );
			AddItem( new RadiantScimitar() );
		}

		public Saril( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Cailla (The Heartwood)" )]
	public class Cailla : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074187, // Want a job?
				1074210 // Hi.  Looking for something to do?
			) );
		}

		[Constructable]
		public Cailla()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Cailla";
			Title = "the guard";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots() );
			AddItem( new HidePants() );
			AddItem( new HidePauldrons() );
			AddItem( new HideGloves() );
			AddItem( new WoodlandBelt() );
			AddItem( new RavenHelm() );
			AddItem( new HideFemaleChest() );
			AddItem( new MagicalShortbow() );
		}

		public Cailla( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Tamm (The Heartwood)" )]
	public class Tamm : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074213, // Hey buddy.  Looking for work?
				1074187 // Want a job?
			) );
		}

		[Constructable]
		public Tamm()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Tamm";
			Title = "the guard";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots() );
			AddItem( new HidePants() );
			AddItem( new HidePauldrons() );
			AddItem( new WingedHelm() );
			AddItem( new HideChest() );
			AddItem( new ElvenCompositeLongbow() );
		}

		public Tamm( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Landy (The Heartwood)" )]
	public class Landy : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074211, // I could use some help.
				1074218 // Hey!  I want to talk to you, now.
			) );
		}

		[Constructable]
		public Landy()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Landy";
			Title = "the soil nurturer";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Sandals( Utility.RandomYellowHue() ) );
			AddItem( new ShortPants( Utility.RandomYellowHue() ) );
			AddItem( new Tunic( Utility.RandomYellowHue() ) );

			Item gloves = new LeafGloves();
			gloves.Hue = Utility.RandomYellowHue();
			AddItem( gloves );
		}

		public Landy( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Elder Alejaha (The Heartwood)" )]
	public class Alejaha : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, 1074223 ); // Have you done it yet?  Oh, I haven’t told you, have I?
		}

		[Constructable]
		public Alejaha()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Elder Alejaha";
			Title = "the wise";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Sandals( Utility.RandomYellowHue() ) );
			AddItem( new ElvenShirt( Utility.RandomYellowHue() ) );
			AddItem( new GemmedCirclet() );
			AddItem( new Cloak( Utility.RandomBrightHue() ) );

			if ( Utility.RandomBool() )
				AddItem( new Kilt( 0x387 ) );
			else
				AddItem( new Skirt( 0x387 ) );
		}

		public Alejaha( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Mielan (The Heartwood)" )]
	public class Mielan : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074219, // Hello there, can I have a moment of your time?
				1074223 // Have you done it yet?  Oh, I haven’t told you, have I?
			) );
		}

		[Constructable]
		public Mielan()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Mielan";
			Title = "the arcanist";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots( 0x901 ) );
			AddItem( new ElvenShirt( 0x56 ) );
			AddItem( new GemmedCirclet() );
			AddItem( new ElvenPants( 0x901 ) );
		}

		public Mielan( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Ciala (The Heartwood)" )]
	public class Ciala : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074206, // Excuse me please traveler, might I have a little of your time?
				1074186 // Come here, I have a task.
			) );
		}

		[Constructable]
		public Ciala()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Ciala";
			Title = "the arborist";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Skirt( Utility.RandomBlueHue() ) );
			AddItem( new ElvenShirt( Utility.RandomYellowHue() ) );
			AddItem( new RoyalCirclet() );

			if ( Utility.RandomBool() )
				AddItem( new Boots( Utility.RandomYellowHue() ) );
			else
				AddItem( new ThighBoots( Utility.RandomYellowHue() ) );
		}

		public Ciala( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Aniel (The Heartwood)" )]
	public class Aniel : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074220, // May I call you friend?  I have a favor to beg of you.
				1074222 // Could I trouble you for some assistance?
			) );
		}

		[Constructable]
		public Aniel()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Aniel";
			Title = "the arborist";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenPants( 0x901 ) );
			AddItem( new LeafChest() );
			AddItem( new HalfApron( Utility.RandomYellowHue() ) );
			AddItem( new ElvenBoots( 0x901 ) );
		}

		public Aniel( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Aulan (The Heartwood)" )]
	public class Aulan : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074188, // Weakling! You are not up to the task I have.
				1074191, // Just keep walking away!  I thought so. Coward!  I’ll bite your legs off!
				1074195 // You there, in the stupid hat!   Come here.
			) );
		}

		[Constructable]
		public Aulan()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Aulan";
			Title = "the expeditionist";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			Item item;

			item = new ElvenBoots();
			item.Hue = Utility.RandomYellowHue();
			AddItem( item );

			AddItem( new ElvenPants( Utility.RandomGreenHue() ) );
			AddItem( new Cloak( Utility.RandomGreenHue() ) );
			AddItem( new Circlet() );

			item = new HideChest();
			item.Hue = Utility.RandomYellowHue();
			AddItem( item );

			item = new HideGloves();
			item.Hue = Utility.RandomYellowHue();
			AddItem( item );
		}

		public Aulan( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Brinnae (The Heartwood)" )]
	public class Brinnae : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074212, // *yawn* You busy?
				1074210 // Hi.  Looking for something to do?
			) );
		}

		[Constructable]
		public Brinnae()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Brinnae";
			Title = "the wise";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots() );
			AddItem( new FemaleLeafChest() );
			AddItem( new LeafArms() );
			AddItem( new HidePants() );
			AddItem( new ElvenCompositeLongbow() );
		}

		public Brinnae( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Elder Caelas (The Heartwood)" )]
	public class Caelas : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074204, // Greetings seeker.  I have an urgent matter for you, if you are willing.
				1074201 // Waste not a minute! There’s work to be done.
			) );
		}

		[Constructable]
		public Caelas()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Elder Caelas";
			Title = "the wise";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots( 0x1BB ) );
			AddItem( new MaleElvenRobe( 0x489 ) );
			AddItem( new Cloak( 0x718 ) );
			AddItem( new RoyalCirclet() );
		}

		public Caelas( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Clehin (The Heartwood)" )]
	public class Clehin : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074211, // I could use some help.
				1074186 // Come here, I have a task.
			) );
		}

		[Constructable]
		public Clehin()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Clehin";
			Title = "the soil nurturer";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots() );
			AddItem( new ElvenShirt() );
			AddItem( new LeafTonlet() );
		}

		public Clehin( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class Cloorne : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074185, // Hey you! Want to help me out?
				1074186 //Come here, I have a task.
			) );
		}

		[Constructable]
		public Cloorne()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Cloorne";
			Title = "the expeditionist";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots( 0x3B2 ) );
			AddItem( new RadiantScimitar() );
			AddItem( new WingedHelm() );

			Item item;

			item = new WoodlandLegs();
			item.Hue = 0x74A;
			AddItem( item );

			item = new HideChest();
			item.Hue = 0x726;
			AddItem( item );

			item = new LeafArms();
			item.Hue = 0x73E;
			AddItem( item );
		}

		public Cloorne( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Salaenih (The Heartwood)" )]
	public class Salaenih : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074207, // Good day to you friend! Allow me to offer you a fabulous opportunity!  Thrills and adventure await!
				1074209 // Hey, could you help me out with something?
			) );
		}

		[Constructable]
		public Salaenih()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Salaenih";
			Title = "the expeditionist";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots() );
			AddItem( new WarCleaver() );

			Item item;

			item = new WoodlandBelt();
			item.Hue = 0x597;
			AddItem( item );

			item = new VultureHelm();
			item.Hue = 0x1BB;
			AddItem( item );

			item = new WoodlandLegs();
			item.Hue = 0x1BB;
			AddItem( item );

			item = new WoodlandChest();
			item.Hue = 0x1BB;
			AddItem( item );

			item = new WoodlandArms();
			item.Hue = 0x1BB;
			AddItem( item );
		}

		public Salaenih( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Vilo (The Heartwood)" )]
	public class Vilo : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074210, // Hi.  Looking for something to do?
				1074220 // May I call you friend?  I have a favor to beg of you.
			) );
		}

		[Constructable]
		public Vilo()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Vilo";
			Title = "the guard";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots( 0x901 ) );
			AddItem( new OrnateAxe() );
			AddItem( new WoodlandBelt( 0x592 ) );
			AddItem( new VultureHelm() );
			AddItem( new WoodlandLegs() );
			AddItem( new WoodlandChest() );
			AddItem( new WoodlandArms() );
			AddItem( new WoodlandGorget() );
		}

		public Vilo( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Tholef (The Heartwood)" )]
	public class Tholef : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074209, // Hey, could you help me out with something?
				1074184 // Come here, I have work for you.
			) );
		}

		[Constructable]
		public Tholef()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Tholef";
			Title = "the grape tender";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Sandals( 0x901 ) );
			AddItem( new FullApron( 0x756 ) );
			AddItem( new ShortPants( 0x28C ) );
			AddItem( new Shirt( 0x28C ) );

			Item item;

			item = new LeafArms();
			item.Hue = 0x28C;
			AddItem( item );
		}

		public Tholef( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Tillanil (The Heartwood)" )]
	public class Tillanil : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074187, // Want a job?
				1074222 // Could I trouble you for some assistance?
			) );
		}

		[Constructable]
		public Tillanil()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Tillanil";
			Title = "the grape tender";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Sandals( 0x1BB ) );
			AddItem( new Tunic( 0x759 ) );
			AddItem( new ShortPants( 0x21 ) );
		}

		public Tillanil( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Waelian (The Heartwood)" )]
	public class Waelian : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074221, // Greetings!  I have a small task for you good traveler.
				1074201 // Waste not a minute! There’s work to be done.
			) );
		}

		[Constructable]
		public Waelian()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Waelian";
			Title = "the trinket weaver";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Shoes( 0x901 ) );
			AddItem( new SmithHammer() );
			AddItem( new LongPants( 0x340 ) );
			AddItem( new GemmedCirclet() );

			Item item;

			item = new LeafChest();
			item.Hue = 0x344;
			AddItem( item );
		}

		public Waelian( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Sleen (The Heartwood)" )]
	public class Sleen : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074200, // Thank goodness you are here, there’s no time to lose.
				1074206 // Excuse me please traveler, might I have a little of your time?
			) );
		}

		[Constructable]
		public Sleen()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Sleen";
			Title = "the trinket weaver";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots( 0x901 ) );
			AddItem( new SmithHammer() );
			AddItem( new Cloak( 0x75A ) );
			AddItem( new ElvenShirt() );
		}

		public Sleen( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Unoelil (The Heartwood)" )]
	public class Unoelil : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074186, // Come here, I have a task.
				1074209 // Hey, could you help me out with something?
			) );
		}

		[Constructable]
		public Unoelil()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Unoelil";
			Title = "the bark weaver";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots( 0x1BB ) );
			AddItem( new ShortPants( 0x1BB ) );
			AddItem( new Tunic( 0x64D ) );
		}

		public Unoelil( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Anolly (The Heartwood)" )]
	public class Anolly : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		[Constructable]
		public Anolly()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Anolly";
			Title = "the bark weaver";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Sandals( 0x901 ) );
			AddItem( new ShortPants( 0x3B3 ) );
			AddItem( new FullApron( 0x1BB ) );
			AddItem( new SmithHammer() );
		}

		public Anolly( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Jusae (The Heartwood)" )]
	public class Jusae : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074210, // Hi.  Looking for something to do?
				1074213 // Hey buddy.  Looking for work?
			) );
		}

		[Constructable]
		public Jusae()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Jusae";
			Title = "the bowcrafter";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Sandals( 0x901 ) );
			AddItem( new ShortPants( 0x661 ) );
			AddItem( new MagicalShortbow() );

			Item item;

			item = new HideChest();
			item.Hue = 0x27B;
			AddItem( item );

			item = new HidePauldrons();
			item.Hue = 0x27E;
			AddItem( item );
		}

		public Jusae( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Cillitha (The Heartwood)" )]
	public class Cillitha : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074223, // Have you done it yet?  Oh, I haven’t told you, have I?
				1074213 // Hey buddy.  Looking for work?
			) );
		}

		[Constructable]
		public Cillitha()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Cillitha";
			Title = "the bowcrafter";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots( 0x901 ) );
			AddItem( new ElvenShirt( 0x731 ) );
			AddItem( new LeafLegs() );
		}

		public Cillitha( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Lohn (The Heartwood)" )]
	public class Lohn : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074187, // Want a job?
				1074209 // Hey, could you help me out with something?
			) );
		}

		[Constructable]
		public Lohn()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Lohn";
			Title = "the metal weaver";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Shoes( 0x901 ) );
			AddItem( new LongPants( 0x359 ) );
			AddItem( new SmithHammer() );
			AddItem( new GemmedCirclet() );

			Item item;

			item = new LeafChest();
			item.Hue = 0x359;
			AddItem( item );
		}

		public Lohn( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Olla (The Heartwood)" )]
	public class Olla : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074187, // Want a job?
				1074185 // Hey you! Want to help me out?
			) );
		}

		[Constructable]
		public Olla()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Olla";
			Title = "the metal weaver";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots() );
			AddItem( new LongPants( 0x3B3 ) );
			AddItem( new SmithHammer() );
			AddItem( new FullApron( 0x1BB ) );
			AddItem( new ElvenShirt() );

		}

		public Olla( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Thallary (The Heartwood)" )]
	public class Thallary : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074221, // Greetings!  I have a small task for you good traveler.
				1074212 // *yawn* You busy?
			) );
		}

		[Constructable]
		public Thallary()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Thallary";
			Title = "the cloth weaver";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Sandals( 0x901 ) );
			AddItem( new LongPants( 0x72E ) );
			AddItem( new Cloak( 0x3B3 ) );
			AddItem( new FancyShirt( 0x13 ) );

		}

		public Thallary( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Ahie (The Heartwood)" )]
	public class Ahie : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074206, // Excuse me please traveler, might I have a little of your time?
				1074203 // Hello friend. I realize you are busy but if you would be willing to render me a service I can assure you that you will be judiciously renumerated.
			) );
		}

		[Constructable]
		public Ahie()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Ahie";
			Title = "the cloth weaver";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Boots( 0x901 ) );
			AddItem( new Skirt( 0x1C ) );
			AddItem( new Cloak( 0x62 ) );
			AddItem( new FancyShirt( 0x738 ) );

		}

		public Ahie( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class Tyeelor : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public Tyeelor()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Tyeelor";
			Title = "the expeditionist";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots( 0x1BB ) );

			Item item;

			item = new WoodlandLegs();
			item.Hue = 0x236;
			AddItem( item );

			item = new WoodlandChest();
			item.Hue = 0x236;
			AddItem( item );

			item = new WoodlandArms();
			item.Hue = 0x236;
			AddItem( item );

			item = new VultureHelm();
			item.Hue = 0x236;
			AddItem( item );

			item = new WoodlandBelt();
			item.Hue = 0x236;
			AddItem( item );

		}

		public Tyeelor( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class Athailon : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public Athailon()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Athailon";
			Title = "the expeditionist";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots( 0x901 ) );
			AddItem( new WoodlandBelt() );
			AddItem( new DiamondMace() );

			Item item;

			item = new WoodlandLegs();
			item.Hue = 0x3B2;
			AddItem( item );

			item = new FemaleElvenPlateChest();
			item.Hue = 0x3B2;
			AddItem( item );

			item = new WoodlandArms();
			item.Hue = 0x3B2;
			AddItem( item );

			item = new WingedHelm();
			item.Hue = 0x3B2;
			AddItem( item );

		}

		public Athailon( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class ElderTaellia : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public ElderTaellia()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Elder Taellia";
			Title = "the wise";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ThighBoots( 0x127 ) );
			AddItem( new FemaleElvenRobe( Utility.RandomBrightHue() ) );
			AddItem( new MagicWand() );
			AddItem( new Circlet() );

		}

		public ElderTaellia( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class ElderMallew : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public ElderMallew()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Elder Mallew";
			Title = "the wise";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots( 0x1BB ) );
			AddItem( new Cloak( 0x3B2 ) );
			AddItem( new Circlet() );

			Item item;

			item = new LeafTonlet();
			item.Hue = 0x544;
			AddItem( item );

			item = new LeafChest();
			item.Hue = 0x538;
			AddItem( item );

			item = new LeafArms();
			item.Hue = 0x528;
			AddItem( item );

		}

		public ElderMallew( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class ElderAbbein : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public ElderAbbein()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Elder Abbein";
			Title = "the wise";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots( 0x72C ) );
			AddItem( new FemaleElvenRobe( 0x8B0 ) );
			AddItem( new RoyalCirclet() );

		}

		public ElderAbbein( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class ElderVicaie : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public ElderVicaie()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Elder Vicaie";
			Title = "the wise";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots() );
			AddItem( new Tunic( 0x732 ) );

			Item item;

			item = new LeafLegs();
			item.Hue = 0x3B2;
			AddItem( item );

		}

		public ElderVicaie( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class ElderJothan : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public ElderJothan()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Elder Jothan";
			Title = "the wise";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ThighBoots() );
			AddItem( new ElvenPants( 0x58D ) );
			AddItem( new ElvenShirt( Utility.RandomYellowHue() ) );
			AddItem( new Cloak( Utility.RandomBrightHue() ) );
			AddItem( new Circlet() );

		}

		public ElderJothan( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class ElderAlethanian : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public ElderAlethanian()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Elder Alethanian";
			Title = "the wise";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots() );
			AddItem( new HidePants() );
			AddItem( new HideFemaleChest() );
			AddItem( new HidePauldrons() );
			AddItem( new GemmedCirclet() );

		}

		public ElderAlethanian( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class Rebinil : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public Rebinil()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Rebinil";
			Title = "the healer";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new Sandals( 0x715 ) );
			AddItem( new FemaleElvenRobe( 0x742 ) );
			AddItem( new RoyalCirclet() );

		}

		public Rebinil( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class Aluniol : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public Aluniol()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Aluniol";
			Title = "the healer";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots( 0x1BB ) );
			AddItem( new MaleElvenRobe( 0x47E ) );
			AddItem( new WildStaff() );

		}

		public Aluniol( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class Olaeni : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public Olaeni()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Olaeni";
			Title = "the thaumaturgist";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new Shoes( 0x75A ) );
			AddItem( new FemaleElvenRobe( 0x13 ) );
			AddItem( new MagicWand() );
			AddItem( new GemmedCirclet() );

		}

		public Olaeni( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class Bolaevin : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public Bolaevin()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Bolaevin";
			Title = "the arcanist";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots( 0x3B2 ) );
			AddItem( new RoyalCirclet() );
			AddItem( new LeafChest() );
			AddItem( new LeafArms() );

			Item item;

			item = new LeafLegs();
			item.Hue = 0x1BB;
			AddItem( item );

		}

		public Bolaevin( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class LorekeeperAneen : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public LorekeeperAneen()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Lorekeeper Aneen";
			Title = "the keeper of tradition";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new Sandals( 0x1BB ) );
			AddItem( new MaleElvenRobe( 0x48F) );
			AddItem( new MagicWand() );

		}

		public LorekeeperAneen( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class Daelas : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public Daelas()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Daelas";
			Title = "the arborist";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots( 0x901 ) );
			AddItem( new ElvenPants( 0x8AB) );

			Item item;

			item = new LeafChest();
			item.Hue = 0x8B0;
			AddItem( item );

			item = new LeafGloves();
			item.Hue = 0x1BB;
			AddItem( item );

		}

		public Daelas( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class Alelle : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public Alelle()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Alelle";
			Title = "the arborist";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new ElvenBoots( 0x1BB ) );

			Item item;

			item = new FemaleLeafChest();
			item.Hue = 0x3A;
			AddItem( item );

			item = new LeafLegs();
			item.Hue = 0x74C;
			AddItem( item );

			item = new LeafGloves();
			item.Hue = 0x1BB;
			AddItem( item );

		}

		public Alelle( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Nillaen (The Heartwood)" )]
	public class LorekeeperNillaen : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		[Constructable]
		public LorekeeperNillaen()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Lorekeeper Nillaen";
			Title = "the keeper of tradition";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Shoes( 0x1BB ) );
			AddItem( new LongPants( 0x1FB ) );
			AddItem( new ElvenShirt() );
			AddItem( new GemmedCirclet() );
			AddItem( new BodySash( 0x25 ) );
			AddItem( new BlackStaff() );
		}

		public LorekeeperNillaen( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Ryal (The Heartwood)" )]
	public class LorekeeperRyal : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074204, // Greetings seeker.  I have an urgent matter for you, if you are willing.
				1074200 // Thank goodness you are here, there’s no time to lose.
			) );
		}

		[Constructable]
		public LorekeeperRyal()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Lorekeeper Ryal";
			Title = "the keeper of tradition";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots( 0x1BB ) );
			AddItem( new LeafTonlet() );
			AddItem( new ElvenShirt( 0x2DD ) );
			AddItem( new Cloak( 0x219 ) );
			AddItem( new GnarledStaff() );
		}

		public LorekeeperRyal( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Braen (The Heartwood)" )]
	public class Braen : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, 1074187 ); // Want a job?
		}

		[Constructable]
		public Braen()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Braen";
			Title = "the thaumaturgist";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Sandals( 0x714 ) );
			AddItem( new MaleElvenRobe( 0x64A ) );
			AddItem( new MagicWand() );
		}

		public Braen( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[QuesterName( "Elder Acob (The Heartwood)" )]
	public class ElderAcob : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, 1074197 ); // Pardon me, but if you could spare some time I’d greatly appreciate it.
		}

		[Constructable]
		public ElderAcob()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Elder Acob";
			Title = "the wise";
			Race = Race.Elf;
			BodyValue = 0x25D;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots( 0x714 ) );
			AddItem( new ElvenShirt( Utility.RandomBrightHue() ) );
			AddItem( new HidePants() );
		}

		public ElderAcob( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class LorekeeperCalendor : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, 1074204 ); // Greetings seeker.  I have an urgent matter for you, if you are willing.
		}

		[Constructable]
		public LorekeeperCalendor()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Lorekeeper Calendor";
			Title = "the keeper of tradition";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new ElvenBoots( 0x714 ) );
			AddItem( new ElvenShirt( Utility.RandomOrangeHue() ) );
			AddItem( new Kilt( Utility.RandomOrangeHue() ) );
			AddItem( new RoyalCirclet() );
		}

		public LorekeeperCalendor( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class LorekeeperSiarra : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, 1074206 ); // Excuse me please traveler, might I have a little of your time?
		}

		[Constructable]
		public LorekeeperSiarra()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Lorekeeper Siarra";
			Title = "the keeper of tradition";
			Race = Race.Elf;
			BodyValue = 0x25E;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Focus, 60.0, 80.0 );

			AddItem( new Sandals( 0x1BB ) );
			AddItem( new ElvenShirt() );
			AddItem( new LeafTonlet() );
			AddItem( new GemmedCirclet() );
		}

		public LorekeeperSiarra( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	#endregion
}
