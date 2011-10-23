using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.ContextMenus;

namespace Server.Items
{
	public class FishBowl : Container
	{		
		public override int LabelNumber{ get{ return 1074499; } } // A fish bowl
		
		public bool Empty
		{
			get{ return Items.Count == 0; }
		}
		
		public BaseFish Fish
		{
			get
			{
				if ( Empty )
					return null;
					
				if ( Items[ 0 ] is BaseFish )
					return (BaseFish) Items[ 0 ];
					
				return null;
			}
		}
		
		[Constructable]
		public FishBowl() : base( 0x241C )
		{
			Hue = 0x47E;			
			MaxItems = 1;
			Weight = 1;
		}

		public FishBowl( Serial serial ) : base( serial )
		{		
		}
		
		public override void OnDoubleClick( Mobile from )
		{			
		}
		
		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			if ( dropped is BaseFish && Empty )
			{
				((BaseFish) dropped).StopTimer();
				InvalidateProperties();
				
				return base.OnDragDrop( from, dropped );	
			}
			else
			{
				from.SendLocalizedMessage( 1074836 ); // The container can not hold that type of object.
				return false;
			}
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			list.Add( 1074499 ); // A fish bowl
			list.Add( 1072788, Weight.ToString() ); // Weight: ~1_WEIGHT~ stone
			
			if ( !Empty )
				list.Add( 1074494, "#" + Fish.LabelNumber ); // Contains: ~1_CREATURE~
				
			list.Add( 1073841, "{0}\t{1}\t{2}", Items.Count, MaxItems, GetTotal( TotalType.Weight ) ); // Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~ stones
		}
		
		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );
			
			if ( !Empty )
				list.Add( new RemoveCreature( this ) );
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
		
		private class RemoveCreature : ContextMenuEntry
		{
			private FishBowl m_Bowl;
		
			public RemoveCreature( FishBowl bowl ) : base( 6242, 0 ) // Remove creature
			{
				m_Bowl = bowl;
			}
			
			public override void OnClick()
			{
				if ( m_Bowl == null || m_Bowl.Deleted )
					return;
					
				BaseFish fish = m_Bowl.Fish;
			
				if ( fish != null )
				{
					if ( !Owner.From.PlaceInBackpack( fish ) )
						Owner.From.SendLocalizedMessage( 1074496 ); // There is no room in your pack for the creature.
					else
					{
						Owner.From.SendLocalizedMessage( 1074495 ); // The creature has been removed from the fish bowl.	
						fish.StartTimer();
						m_Bowl.InvalidateProperties();	
					}
				}
			}
		}
	}
}
