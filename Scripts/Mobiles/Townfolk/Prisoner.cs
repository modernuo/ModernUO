using System;
using Server.Items;
using Server.Engines.MLQuests;

namespace Server.Mobiles.Townfolk
{
	public class Prisoner : BaseEscortable
	{
		[Constructable]
		public Prisoner()
		{
			Title = Female ? "the noblewoman" : "the nobleman";

			CantWalk = true;
			IsPrisoner = true;
		}

		public override bool CanTeach { get { return true; } }
		public override bool ClickTitle { get { return false; } }

		public override void InitOutfit()
		{
			if ( Female )
			{
				AddItem( new FancyDress( Utility.RandomNondyedHue() ) );
			}
			else
			{
				AddItem( new FancyShirt( Utility.RandomNondyedHue() ) );
				AddItem( new LongPants( Utility.RandomNondyedHue() ) );
			}

			if ( Utility.RandomBool() )
				AddItem( new Boots() );
			else
				AddItem( new ThighBoots() );

			Utility.AssignRandomHair( this );
			Utility.AssignRandomFacialHair( this, HairHue );
		}

		public override void Shout( PlayerMobile pm )
		{
			/*
			 * 502261 - HELP!
			 * 502262 - Help me!
			 * 502263 - Canst thou aid me?!
			 * 502264 - Help a poor prisoner!
			 * 502265 - Help! Please!
			 * 502266 - Aaah! Help me!
			 * 502267 - Go and get some help!
			 */
			MLQuestSystem.Tell( this, pm, Utility.Random( 502261, 7 ) );
		}

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			base.OnMovement( m, oldLocation );

			if ( CantWalk && InRange( m, 1 ) && !InRange( oldLocation, 1 ) && ( !m.Hidden || m.AccessLevel == AccessLevel.Player ) )
				Say( 502268 ); // Quickly, I beg thee! Unlock my chains! If thou dost look at me close thou canst see them.
		}

		public Prisoner( Serial serial )
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
}
