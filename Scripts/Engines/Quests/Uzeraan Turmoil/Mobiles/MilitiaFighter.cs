using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Haven
{
	public class MilitiaFighter : BaseCreature
	{
		[Constructable]
		public MilitiaFighter() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			InitStats( 40, 30, 5 );
			Title = "the Militia Fighter";

			SpeechHue = Utility.RandomDyedHue();

			Hue = Utility.RandomSkinHue();

			Female = false;
			Body = 0x190;
			Name = NameList.RandomName( "male" );

			Utility.AssignRandomHair( this );
			Utility.AssignRandomFacialHair( this, HairHue );

			AddItem( new ThighBoots( 0x1BB ) );
			AddItem( new LeatherChest() );
			AddItem( new LeatherArms() );
			AddItem( new LeatherLegs() );
			AddItem( new LeatherCap() );
			AddItem( new LeatherGloves() );
			AddItem( new LeatherGorget() );

			Item weapon;
			switch ( Utility.Random( 6 ) )
			{
				case 0: weapon = new Broadsword(); break;
				case 1: weapon = new Cutlass(); break;
				case 2: weapon = new Katana(); break;
				case 3: weapon = new Longsword(); break;
				case 4: weapon = new Scimitar(); break;
				default: weapon = new VikingSword(); break;
			}
			weapon.Movable = false;
			AddItem( weapon );

			Item shield = new BronzeShield();
			shield.Movable = false;
			AddItem( shield );

			SetSkill( SkillName.Swords, 20.0 );
		}

		public override bool ClickTitle { get { return false; } }

		public override bool IsEnemy( Mobile m )
		{
			if ( m.Player || m is BaseVendor )
				return false;

			if ( m is BaseCreature )
			{
				BaseCreature bc = (BaseCreature)m;

				Mobile master = bc.GetMaster();
				if( master != null )
					return IsEnemy( master );
			}

			return m.Karma < 0;
		}

		public MilitiaFighter( Serial serial ) : base( serial )
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

	public class MilitiaFighterCorpse : Corpse
	{
		public MilitiaFighterCorpse( Mobile owner, HairInfo hair, FacialHairInfo facialhair, List<Item> equipItems ) : base( owner, hair, facialhair, equipItems )
		{
		}

		public override void AddNameProperty( ObjectPropertyList list )
		{
			if ( ItemID == 0x2006 ) // Corpse form
			{
				list.Add( "a human corpse" );
				list.Add( 1049318, this.Name ); // the remains of ~1_NAME~ the militia fighter
			}
			else
			{
				list.Add( 1049319 ); // the remains of a militia fighter
			}
		}

		public override void OnSingleClick( Mobile from )
		{
			int hue = Notoriety.GetHue( Server.Misc.NotorietyHandlers.CorpseNotoriety( from, this ) );

			if ( ItemID == 0x2006 ) // Corpse form
				from.Send( new MessageLocalized( Serial, ItemID, MessageType.Label, hue, 3, 1049318, "", Name ) ); // the remains of ~1_NAME~ the militia fighter
			else
				from.Send( new MessageLocalized( Serial, ItemID, MessageType.Label, hue, 3, 1049319, "", "" ) ); // the remains of a militia fighter
		}

		public override void Open( Mobile from, bool checkSelfLoot )
		{
			if ( from.InRange( this.GetWorldLocation(), 2 ) )
			{
				from.SendLocalizedMessage( 1049661, "", 0x22 ); // Thinking about his sacrifice, you can't bring yourself to loot the body of this militia fighter.
			}
		}

		public MilitiaFighterCorpse( Serial serial ) : base( serial )
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