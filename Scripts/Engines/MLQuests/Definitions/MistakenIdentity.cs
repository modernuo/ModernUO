using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Engines.MLQuests.Items;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
	#region Quests

	public class MistakenIdentity : MLQuest
	{
		public override Type NextQuest { get { return typeof( YouScratchMyBack ); } }

		public MistakenIdentity()
		{
			Activated = true;
			Title = 1074573; // Mistaken Identity
			Description = 1074574; // What do you want?  Wonderful, another whining request for a refund on tuition.  You know, experiences like that are invaluable ... and infrequent.  Having the opportunity to test yourself under such realistic situations isn't something the college offers all students.  Fine. Fine.  You'll need to submit a refund request form in triplicate before I can return your 1,000,000 gold tuition.  You'll need to get some signatures and a few other odds and ends.
			RefusalMessage = 1074606; // If you're not willing to follow the proper process then go away.
			InProgressMessage = 1074605; // You're not getting a refund without the proper forms and signatures.
			CompletionMessage = 1074607; // Oh blast!  Not another of those forms.  I'm so sick of this endless paperwork.
			CompletionNotice = CompletionNoticeShort;

			Objectives.Add( new DeliverObjective( typeof( TuitionReimbursementForm ), 1, "Tuition Reimbursement Form", typeof( Gorrow ) ) );

			Rewards.Add( new DummyReward( 1074634 ) ); // Tuition Reimbursement
		}

		public override void Generate()
		{
			base.Generate();

			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Aernya" ), new Point3D( 2095, 1380, -90 ), Map.Malas );
		}
	}

	public class YouScratchMyBack : MLQuest
	{
		public override Type NextQuest { get { return typeof( FoolingAernya ); } }
		public override bool IsChainTriggered { get { return true; } }

		public YouScratchMyBack()
		{
			Activated = true;
			Title = 1074608; // You Scratch My Back
			Description = 1074609; // Heh.  Heheheh.  Good one.  You're not a Bedlam student and you're definitely not eligible for a tuition refund.  Heheheh. That old witch Aernya doesn't see as well as she used to you know.  Otherwise, she would have ... hmmm, wait a minute.  I sense a certain 'opportunity' here.  I'll sign your forms in return for a little help with a project of my own.  What do you say?
			RefusalMessage = 1074615; // Hehehe.  Your choice.
			InProgressMessage = 1074616; // I'm something of a gourmet, you see.  It's tough getting some of the ingredients, though.  Bring me back some pixie legs, unicorn ribs and ki-rin brains and I'll sign your form.
			CompletionMessage = 1074617; // Oh excellent, you're back.  I'll get the oven going.  That thing about pixie legs, you see, is that they burn and dry out if you're not really careful.  Taste just like chicken too!
			CompletionNotice = CompletionNoticeShortReturn;

			Objectives.Add( new CollectObjective( 1, typeof( UnicornRibs ), "Unicorn Ribs" ) );
			Objectives.Add( new CollectObjective( 2, typeof( KirinBrains ), "Ki-Rin Brains" ) );
			Objectives.Add( new CollectObjective( 5, typeof( PixieLeg ), "Pixie Leg" ) );

			Rewards.Add( new DummyReward( 1074634 ) ); // Tuition Reimbursement
		}

		public override void Generate()
		{
			base.Generate();

			PutSpawner( new Spawner( 1, 5, 10, 0, 4, "Gorrow" ), new Point3D( 993, 512, -50 ), Map.Malas );
		}
	}

	public class FoolingAernya : MLQuest
	{
		public override Type NextQuest { get { return typeof( NotQuiteThatEasy ); } }
		public override bool IsChainTriggered { get { return true; } }

		public FoolingAernya()
		{
			Activated = true;
			Title = 1074618; // Fooling Aernya
			Description = 1074619; // Now that I've signed your papers you'd better get back to that witch Aernya.  Mmmm mmm smell those ribs!
			RefusalMessage = 1074620; // Giving up on your scheme eh?  Suit yourself.
			InProgressMessage = 1074621; // You better hurry back to Mistress Aernya with that signed form.  The college only has so much money and with enough claims you may find yourself unable to get your tuition refunded.  *wink*
			CompletionMessage = 1074622; // What?  Hrmph.  Gorrow signed your form did he?  Let me see that.  *squint*
			CompletionNotice = CompletionNoticeShort;

			Objectives.Add( new DeliverObjective( typeof( SignedTuitionReimbursementForm ), 1, "Signed Tuition Reimbursement Form", typeof( Aernya ) ) );

			Rewards.Add( new DummyReward( 1074634 ) ); // Tuition Reimbursement
		}
	}

	public class NotQuiteThatEasy : MLQuest
	{
		public override Type NextQuest { get { return typeof( ConvinceMe ); } }
		public override bool IsChainTriggered { get { return true; } }

		public NotQuiteThatEasy()
		{
			Activated = true;
			Title = 1074623; // Not Quite That Easy
			Description = 1074624; // I wouldn't be too smug just yet, whiner.  You still need Master Gnosos' signature before I can cut your refund.  Last I heard, he's coordinating the recovery of the portions of the college that are currently overrun.  *nasty smile*  Off with you.
			RefusalMessage = 1074626; // Coward.
			InProgressMessage = 1074627; // What are you waiting for?  The iron maiden is still the portal to Bedlam.
			CompletionMessage = 1074628; // Made it through did you?  Did you happen to see Red Death out there?  Big horse, skeletal ... burning eyes?  No?  What's this?  Forms?  FORMS?  I'm up to my eyebrows in ravenous out-of-control undead and you want a signature?
			CompletionNotice = CompletionNoticeShort;

			Objectives.Add( new DeliverObjective( typeof( SignedTuitionReimbursementForm ), 1, "Signed Tuition Reimbursement Form", typeof( MasterGnosos ) ) );

			Rewards.Add( new DummyReward( 1074634 ) ); // Tuition Reimbursement
		}

		public override void Generate()
		{
			base.Generate();

			PutDeco( new BedlamTeleporter(), new Point3D( 2067, 1371, -75 ), Map.Malas );
		}

		public override void OnAccepted( MLQuestInstance instance )
		{
			instance.PlayerContext.BedlamAccess = true; // Permanent access
		}
	}

	public class ConvinceMe : MLQuest
	{
		public override Type NextQuest { get { return typeof( TuitionReimbursement ); } }
		public override bool IsChainTriggered { get { return true; } }

		public ConvinceMe()
		{
			Activated = true;
			Title = 1074629; // Convince Me
			Description = 1074630; // I'm not signing any forms until the situation here is under control.  So, you can either help out or you can forget getting your tuition refund.  Which will it be?  Help control the shambling dead?
			RefusalMessage = 1074631; // No signature for you.
			InProgressMessage = 1074632; // No signature for you until you kill off some of the shambling dead out there and destroy that blasted horse.
			CompletionMessage = 1074633; // Pulled it off huh?  Well then you've earned this signature!
			CompletionNotice = CompletionNoticeShortReturn;

			QuestArea bedlam = new QuestArea( 1074835, "Bedlam" ); // Bedlam

			Objectives.Add( new KillObjective( 1, new Type[] { typeof( RedDeath ) }, "Red Death", bedlam ) );
			Objectives.Add( new KillObjective( 10, new Type[] { typeof( GoreFiend ) }, "gore fiends", bedlam ) );
			Objectives.Add( new KillObjective( 8, new Type[] { typeof( RottingCorpse ) }, "rotting corpses", bedlam ) );

			Rewards.Add( new DummyReward( 1074634 ) ); // Tuition Reimbursement
		}

		public override void Generate()
		{
			base.Generate();

			PutSpawner( new Spawner( 1, 5, 10, 0, 3, "MasterGnosos" ), new Point3D( 87, 1639, 0 ), Map.Malas );
		}
	}

	public class TuitionReimbursement : MLQuest
	{
		public override bool IsChainTriggered { get { return true; } }

		public TuitionReimbursement()
		{
			Activated = true;
			Title = 1074634; // Tuition Reimbursement
			Description = 1074635; // Well, there you are.  I've added my signature to that of Gorrow, so you should be set to return to Mistress Aernya and get your tuition refunded.
			RefusalMessage = 1074636; // Great! If you're going to stick around here, I know we have more tasks for you to perform.
			InProgressMessage = 1074637; // Just head out the main gates there and you'll find yourself embracing the iron maiden in the Bloodletter's Guild.
			CompletionMessage = 1074638; // *disinterested stare*  What?  Oh, you've gotten your form filled in.  How nice.  *glare*  And I'd hoped you'd drop this charade before I was forced to rub your nose in it.  *nasty smile*  You're not even a student and as such, you're not eligible for a refund -- you've never paid tuition.  For your services, Master Gnosos has recommended you receive pay.  So here.  Now go away.
			CompletionNotice = CompletionNoticeShort;

			Objectives.Add( new DeliverObjective( typeof( CompletedTuitionReimbursementForm ), 1, "Completed Tuition Reimbursement Form", typeof( Aernya ) ) );

			Rewards.Add( ItemReward.Strongbox );
		}
	}

	#endregion

	#region Mobiles

	[QuesterName( "Aernya (Umbra)" )]
	public class Aernya : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		[Constructable]
		public Aernya()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Aernya";
			Title = "the Mistress of Admissions";
			Race = Race.Human;
			BodyValue = 0x191;
			Female = true;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			Utility.AssignRandomHair( this, true );

			AddItem( new Backpack() );
			AddItem( new Sandals( Utility.RandomNeutralHue() ) );
			AddItem( new Skirt( Utility.RandomBool() ? 0x1 : 0x0 ) );
			AddItem( new Cloak( Utility.RandomBrightHue() ) );
			AddItem( new FancyShirt( Utility.RandomBool() ? 0x3B2 : 0x3B3 ) );
		}

		public Aernya( Serial serial )
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

	[QuesterName( "Gorrow (Luna)" )]
	public class Gorrow : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, Utility.RandomList(
				1074200, // Thank goodness you are here, there’s no time to lose.
				1074203 // Hello friend. I realize you are busy but if you would be willing to render me a service I can assure you that you will be judiciously renumerated.
			) );
		}

		[Constructable]
		public Gorrow()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Gorrow";
			Title = "the Mayor";
			Race = Race.Human;
			BodyValue = 0x190;
			Female = false;
			Hue = Race.RandomSkinHue();
			InitStats( 100, 100, 25 );

			AddItem( new Backpack() );
			AddItem( new Shoes( 0x1BB ) );
			AddItem( new Tunic( Utility.RandomNeutralHue() ) );
			AddItem( new LongPants( 0x901 ) );
			AddItem( new Cloak( Utility.RandomRedHue() ) );
		}

		public Gorrow( Serial serial )
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

	[QuesterName( "Master Gnosos (Bedlam)" )]
	public class MasterGnosos : BaseCreature
	{
		public override bool IsInvulnerable { get { return true; } }
		public override bool CanTeach { get { return true; } }

		public override bool CanShout { get { return true; } }
		public override void Shout( PlayerMobile pm )
		{
			MLQuestSystem.Tell( this, pm, 1074186 ); // Come here, I have a task.
		}

		[Constructable]
		public MasterGnosos()
			: base( AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2 )
		{
			Name = "Master Gnosos";
			Title = "the necromancer";
			Race = Race.Human;
			BodyValue = 0x190;
			Female = false;
			Hue = 0x83E8;
			InitStats( 100, 100, 25 );

			HairItemID = 0x2049;
			FacialHairItemID = 0x204B;

			AddItem( new Backpack() );
			AddItem( new Shoes( 0x485 ) );
			AddItem( new Robe( 0x497 ) );

			SetSkill( SkillName.EvalInt, 60.0, 80.0 );
			SetSkill( SkillName.Inscribe, 60.0, 80.0 );
			SetSkill( SkillName.MagicResist, 60.0, 80.0 );
			SetSkill( SkillName.SpiritSpeak, 60.0, 80.0 );
			SetSkill( SkillName.Meditation, 60.0, 80.0 );
			SetSkill( SkillName.Necromancy, 60.0, 80.0 );
		}

		public MasterGnosos( Serial serial )
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
