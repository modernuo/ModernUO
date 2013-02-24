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
	public class WarriorsOfTheGemkeeper : MLQuest
	{
		public override Type NextQuest { get { return typeof( CloseEnough ); } }

		public WarriorsOfTheGemkeeper()
		{
			Activated = true;
			Title = 1074536; // Warriors of the Gemkeeper
			Description = 1074537; // Here we honor the Gemkeeper's Apprentice and seek to aid her efforts against the humans responsible for the death of her teacher - and the destruction of the elven way of life.  Our tales speak of a fierce race of servants of the Gemkeeper, the men-bulls whose battle-skill was renowned. It is desireable to discover the fate of these noble creatures after the Rupture.  Will you seek information?
			RefusalMessage = 1074063; // Fine then, I'm shall find another to run my errands then.
			InProgressMessage = 1074540; // I care not how you get the information.  Kill as many humans as you must ... but find the fate of the minotaurs.  Perhaps another of the Gemkeeper's servants has the knowledge we seek.
			CompletionMessage = 1074542; // What have you found?

			Objectives.Add( new CollectObjective( 1, typeof( FragmentOfAMap ), "fragment of a map" ) );

			Rewards.Add( new DummyReward( 1074876 ) ); // Knowledge of the legendary minotaur.
		}
	}

	public class CloseEnough : MLQuest
	{
		public override Type NextQuest { get { return typeof( TakingTheBullByTheHorns ); } }
		public override bool IsChainTriggered { get { return true; } }

		public CloseEnough()
		{
			Activated = true;
			Title = 1074544; // Close Enough
			Description = 1074546; // Ah ha!  You see here ... and over here ... The map fragment places the city of the bull-men, Labyrinth, on that piece of Sosaria that was thrown into the sky.  Hmmm, I would have you go there and seek out these warriors to see if they might join our cause.  But, legend speaks of a mighty barrier to prevent invasion of the city. Take this map to Canir and explain the problem. Perhaps she can devise a solution.
			RefusalMessage = 1074063; // Fine then, I'm shall find another to run my errands then.
			InProgressMessage = 1074548; // Canir is nearby, run and speak with her.
			CompletionMessage = 1074549; // Yes?  What do you want?  I'm very busy.

			Objectives.Add( new DeliverObjective( typeof( FragmentOfAMapDelivery ), 1, "fragment of a map", typeof( Canir ) ) );

			Rewards.Add( new DummyReward( 1074876 ) ); // Knowledge of the legendary minotaur.
		}
	}

	public class TakingTheBullByTheHorns : MLQuest
	{
		public override Type NextQuest { get { return typeof( EmissaryToTheMinotaur ); } }
		public override bool IsChainTriggered { get { return true; } }

		public TakingTheBullByTheHorns()
		{
			Activated = true;
			Title = 1074551; // Taking the Bull by the Horns
			Description = 1074553; // Interesting.  I believe I have a way.  I will need some materials to infuse you with the essence of a bull-man, so you can fool their defenses.  The most similar beast to the original Baratarian bull that the minotaur were bred from is undoubtedly the mighty Gaman, native to the Lands of the Feudal Lords.  I need horns, in great quantity to undertake this magic.
			RefusalMessage = 1074554; // Oh come now, don't be afraid.  The magic won't harm you.
			InProgressMessage = 1074555; // I cannot grant you the ability to pass through the bull-men's defenses without the gaman horns.
			CompletionMessage = 1074556; // You've returned at last!  Give me just a moment to examine what you've brought and I can perform the magic that will allow you enter the Labyrinth.

			Objectives.Add( new CollectObjective( 20, typeof( GamanHorns ), "gaman horns" ) );

			Rewards.Add( new DummyReward( 1074876 ) ); // Knowledge of the legendary minotaur.
		}
	}

	public class EmissaryToTheMinotaur : MLQuest
	{
		public override bool IsChainTriggered { get { return true; } }

		public EmissaryToTheMinotaur()
		{
			Activated = true;
			Title = 1074824; // Emissary to the Minotaur
			Description = 1074825; // *whew*  It is done!  The fierce essence of the bull has been infused into your aura.  You are able now to breach the ancient defenses of the city.  Go forth and seek the minotaur -- and then return with wonderous tales and evidence of your visit to the Labyrinth.
			RefusalMessage = 1074827; // As you wish.  I can't understand why you'd pass up such a remarkable opportunity.  Think of the adventures you would have.
			InProgressMessage = 1074828; // You won't reach the minotaur city by loitering around here!  What are you waiting for?  You need to get to Malas and find the access point for the island.  You'll be renowned for your discovery!
			CompletionMessage = 1074829; // Oh! You've returned at last!  I can't wait to hear the tales ... but first, let me see those artifacts.  You've certainly earned this reward.

			Objectives.Add( new CollectObjective( 3, typeof( MinotaurArtifact ), "minotaur artifacts" ) );

			Rewards.Add( ItemReward.Strongbox );
		}
	}
}
