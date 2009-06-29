using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Spells;
using Server.Multis;
using Server.Network;
using Server.Targeting;
using Server.Accounting;
using Server.ContextMenus;
using Server.Engines.VeteranRewards;

namespace Server.Mobiles
{    
	public enum StatueType
	{
		Marble,
		Jade,
		Bronze
	}
	
	public enum StatuePose
	{
		Ready,
		Casting,
		Salute,
		AllPraiseMe,
		Fighting,
		HandsOnHips
	}
	
	public enum StatueMaterial
	{
		Antique,
		Dark,
		Medium, 
		Light
	}
	
	public class CharacterStatue : Mobile, IRewardItem
	{		
		private StatueType m_Type;
		private StatuePose m_Pose;		
		private StatueMaterial m_Material;
		
		[CommandProperty( AccessLevel.GameMaster )]
		public StatueType StatueType
		{
			get { return m_Type; }
			set { m_Type = value; InvalidateHues(); InvalidatePose(); }			
		}		
		
		[CommandProperty( AccessLevel.GameMaster )]
		public StatuePose Pose
		{
			get { return m_Pose; }
			set { m_Pose = value; InvalidatePose(); }			
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public StatueMaterial Material
		{
			get { return m_Material; }
			set { m_Material = value; InvalidateHues(); InvalidatePose(); }			
		}
		
		private Mobile m_SculptedBy;
		private DateTime m_SculptedOn;
		
		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile SculptedBy
		{
			get{ return m_SculptedBy; }
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime SculptedOn
		{
			get{ return m_SculptedOn; }
		}
		
		private CharacterStatuePlinth m_Plinth;
		
		public CharacterStatuePlinth Plinth
		{
			get { return m_Plinth; }
			set { m_Plinth = value; }				
		}
		
		private bool m_IsRewardItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get{ return m_IsRewardItem; }
			set{ m_IsRewardItem = value; }
		}
	
		public CharacterStatue( Mobile from, StatueType type ) : base()
		{		
			m_Type = type;
			m_Pose = StatuePose.Ready;
			m_Material = StatueMaterial.Antique;
			
			m_SculptedBy = from;
			m_SculptedOn = DateTime.Now;
			
			Direction = Direction.South;
			AccessLevel = AccessLevel.Counselor;
			Hits = HitsMax;
			Blessed = true;
			Frozen = true;
			
			CloneBody( from );
			CloneClothes( from );
			InvalidateHues();
		}

		public CharacterStatue( Serial serial ) : base( serial )
		{
		}    	
		
		public override void OnDoubleClick( Mobile from )
		{
			DisplayPaperdollTo( from );
		}		
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			if ( m_SculptedBy != null )
			{
				if ( m_SculptedBy.Title != null )
					list.Add( 1076202, m_SculptedBy.Title + " " + m_SculptedBy.Name ); // Sculpted by ~1_Name~
				else
					list.Add( 1076202, m_SculptedBy.Name ); // Sculpted by ~1_Name~
			}
		}   	
		
		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );
			
			if ( from.Alive )
			{
				BaseHouse house = BaseHouse.FindHouseAt( this );
								
				if ( ( house != null && house.IsCoOwner( from ) ) || (int) from.AccessLevel > (int) AccessLevel.Counselor )
					list.Add( new DemolishEntry( this ) );
			}
		}
		
		public override void OnAfterDelete()
		{
			base.OnAfterDelete();
			
			if ( m_Plinth != null && !m_Plinth.Deleted )
				m_Plinth.Delete();
		}
		
		protected override void OnMapChange( Map oldMap )
		{			
			InvalidatePose();
			
			if ( m_Plinth != null )
				m_Plinth.Map = Map;
		}
		
		protected override void OnLocationChange( Point3D oldLocation )
		{
			InvalidatePose();
			
			if ( m_Plinth != null )
				m_Plinth.Location = new Point3D( X, Y, Z - 5 );
		}
		
		public override bool CanBeRenamedBy( Mobile from )
		{
			return false;
		}
		
		public override bool CanBeDamaged()
		{
			return false;
		}
		
		public void OnRequestedAnimation( Mobile from )
		{				
			from.Send( new UpdateStatueAnimation( this, 1, m_Animation, m_Frames ) );
		}

		public override void OnAosSingleClick( Mobile from )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
			
			writer.Write( (int) m_Type );
			writer.Write( (int) m_Pose );
			writer.Write( (int) m_Material );
			
			writer.Write( (Mobile) m_SculptedBy );
			writer.Write( (DateTime) m_SculptedOn );			
			
			writer.Write( (Item) m_Plinth );
			writer.Write( (bool) m_IsRewardItem );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
			
			m_Type = (StatueType) reader.ReadInt();
			m_Pose = (StatuePose) reader.ReadInt();
			m_Material = (StatueMaterial) reader.ReadInt();
			
			m_SculptedBy = reader.ReadMobile();
			m_SculptedOn = reader.ReadDateTime();
			
			m_Plinth = reader.ReadItem() as CharacterStatuePlinth;
			m_IsRewardItem = reader.ReadBool();
			
			InvalidatePose();
			
			Frozen = true;
		}
		
		public void Sculpt( Mobile by )
		{
			m_SculptedBy = by;
			m_SculptedOn = DateTime.Now;
			
			InvalidateProperties();
		}
		
		public void Demolish( Mobile by )
		{
			CharacterStatueDeed deed = new CharacterStatueDeed( null );
		
			if ( by.PlaceInBackpack( deed ) )
			{
				Internalize();
				
				deed.Statue = this;
				deed.IsRewardItem = m_IsRewardItem;
				
				if ( m_Plinth != null )
					m_Plinth.Internalize();
			}
			else
			{
				by.SendLocalizedMessage( 500720 ); // You don't have enough room in your backpack!
				deed.Delete();
			}
		}
		
		public void Restore( CharacterStatue from )
		{		
			m_Material = from.Material;
			m_Pose = from.Pose;
			
			Direction = from.Direction;
			
			CloneBody( from );
			CloneClothes( from );
			
			InvalidateHues();
			InvalidatePose();
		}
		
		public void CloneBody( Mobile from )
		{
			Name = from.Name;
			BodyValue = from.BodyValue;
			HairItemID = from.HairItemID;
			FacialHairItemID = from.FacialHairItemID;			
		}
		
		public void CloneClothes( Mobile from )
		{
			for ( int i = Items.Count - 1; i >= 0; i -- )
				Items[ i ].Delete();
			
			for ( int i = from.Items.Count - 1; i >= 0; i -- )
			{
				Item item = from.Items[ i ];
				
				if ( item.Layer != Layer.Backpack && item.Layer != Layer.Mount && item.Layer != Layer.Bank )
					AddItem( CloneItem( item ) );
			}
		}
		
		public Item CloneItem( Item item )
		{
			Item cloned = new Item( item.ItemID );
			cloned.Layer = item.Layer;
			cloned.Name = item.Name;
			cloned.Hue = item.Hue;
			cloned.Weight = item.Weight;
			cloned.Movable = false;
			
			return cloned;
		}
		
		public void InvalidateHues()
		{
			Hue = 0xB8F + (int) m_Type * 4 + (int) m_Material;
			
			HairHue = Hue;
			
			if ( FacialHairItemID > 0 )
				FacialHairHue = Hue;
			
			for ( int i = Items.Count - 1; i >= 0; i -- )
				Items[ i ].Hue = Hue;
			
			if ( m_Plinth != null )
				m_Plinth.InvalidateHue();
		}
		
		private int m_Animation;
		private int m_Frames;
		
		public void InvalidatePose()
		{		
			switch ( m_Pose )
			{
				case StatuePose.Ready: 
						m_Animation = 4;
						m_Frames = 0;
						break;
				case StatuePose.Casting:
						m_Animation = 16;
						m_Frames = 2;
						break;
				case StatuePose.Salute:
						m_Animation = 33;
						m_Frames = 1;
						break;
				case StatuePose.AllPraiseMe:
						m_Animation = 17;
						m_Frames = 4;
						break;
				case StatuePose.Fighting:
						m_Animation = 31;
						m_Frames = 5;
						break;
				case StatuePose.HandsOnHips:
						m_Animation = 6;
						m_Frames = 1;
						break;
			}

			if( Map != null )
			{
				ProcessDelta();

				Packet p = null;

				IPooledEnumerable eable = Map.GetClientsInRange( Location );

				foreach( NetState state in eable )
				{
					state.Mobile.ProcessDelta();

					if( p == null )
						p = Packet.Acquire( new UpdateStatueAnimation( this, 1, m_Animation, m_Frames ) );

					state.Send( p );
				}

				Packet.Release( p );

				eable.Free();
			}
		}
		
		private class DemolishEntry : ContextMenuEntry
		{
			private CharacterStatue m_Statue;

			public DemolishEntry( CharacterStatue statue ) : base( 6275, 2 )
			{
				m_Statue = statue;
			}
	
			public override void OnClick()
			{
				if ( m_Statue.Deleted )
					return;
					
				m_Statue.Demolish( Owner.From );
			}
		}
	}
	
	public class CharacterStatueDeed : Item, IRewardItem
	{
		public override int LabelNumber
		{ 
			get
			{ 
				if ( m_Statue != null )
				{
					switch ( m_Statue.StatueType )
					{
						case StatueType.Marble: return 1076189;
						case StatueType.Jade: return 1076188;
						case StatueType.Bronze: return 1076190;
					}
				}
					
				return 1076173; 
			} 
		}
		
		private CharacterStatue m_Statue;
		private bool m_IsRewardItem;
		
		[CommandProperty( AccessLevel.GameMaster )]
		public CharacterStatue Statue
		{
			get { return m_Statue; }	
			set { m_Statue = value; }	
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public StatueType StatueType
		{
			get
			{ 
				if ( m_Statue != null )
					return m_Statue.StatueType; 
				
				return StatueType.Marble;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get{ return m_IsRewardItem; }
			set{ m_IsRewardItem = value; InvalidateProperties(); }
		}
		
		public CharacterStatueDeed( CharacterStatue statue ) : base( 0x14F0 )
		{
			m_Statue = statue;
		
			LootType = LootType.Blessed;
			Weight = 1.0;
		}

		public CharacterStatueDeed( Serial serial ) : base( serial )
		{
		}    
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			if ( m_IsRewardItem )
				list.Add( 1076222 ); // 6th Year Veteran Reward
			
			if ( m_Statue != null )
				list.Add( 1076231, m_Statue.Name ); // Statue of ~1_Name~
		}   	
		
		public override void OnDoubleClick( Mobile from )
		{
			Account acct = from.Account as Account;
			
			if ( acct != null && from.AccessLevel == AccessLevel.Player )
			{	
				TimeSpan time = TimeSpan.FromDays( RewardSystem.RewardInterval.TotalDays * 6 ) - ( DateTime.Now - acct.Created );
				
				if ( time > TimeSpan.Zero )
				{
					from.SendLocalizedMessage( 1008126, true, Math.Ceiling( time.TotalDays / RewardSystem.RewardInterval.TotalDays ).ToString() ); // Your account is not old enough to use this item. Months until you can use this item : 
					return;
				}
			}
				
			if ( IsChildOf( from.Backpack ) )
			{
				if ( !from.IsBodyMod )
				{
					from.SendLocalizedMessage( 1076194 ); // Select a place where you would like to put your statue.
					from.Target = new CharacterStatueTarget( this, StatueType );
				}
				else
					from.SendLocalizedMessage( 1073648 ); // You may only proceed while in your original state...
			}
			else
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
		}		
		
		public override void OnDelete()
		{
			base.OnDelete();
			
			if ( m_Statue != null )
				m_Statue.Delete();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
			
			writer.Write( (Mobile) m_Statue );
			writer.Write( (bool) m_IsRewardItem );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
			
			m_Statue = reader.ReadMobile() as CharacterStatue;
			m_IsRewardItem = reader.ReadBool();
		}
	}
	
	public class CharacterStatueTarget : Target
	{
		private Item m_Maker;
		private StatueType m_Type;
	
		public CharacterStatueTarget( Item maker, StatueType type ) : base( -1, true, TargetFlags.None )
		{
			m_Maker = maker;
			m_Type = type;
		}
		
		protected override void OnTarget( Mobile from, object targeted )
		{
			IPoint3D p = targeted as IPoint3D;
			Map map = from.Map;

			if ( p == null || map == null || m_Maker == null || m_Maker.Deleted )
				return;
				
			if ( m_Maker.IsChildOf( from.Backpack ) )
			{
				SpellHelper.GetSurfaceTop( ref p );			
				BaseHouse house = null;
				Point3D loc = new Point3D( p );
				
				if ( targeted is Item && !((Item) targeted).IsLockedDown && !((Item) targeted).IsSecure && !(targeted is AddonComponent) )
				{
					from.SendLocalizedMessage( 1076191 ); // Statues can only be placed in houses.
					return;
				}
				else if ( from.IsBodyMod )
				{
					from.SendLocalizedMessage( 1073648 ); // You may only proceed while in your original state...
					return;
				}
								
				AddonFitResult result = CouldFit( loc, map, from, ref house );
				
				if ( result == AddonFitResult.Valid )
				{				
					CharacterStatue statue = new CharacterStatue( from, m_Type );
					CharacterStatuePlinth plinth = new CharacterStatuePlinth( statue );
										
					house.Addons.Add( plinth );
					
					if ( m_Maker is IRewardItem )
						statue.IsRewardItem = ( (IRewardItem) m_Maker).IsRewardItem;
						
					statue.Plinth = plinth;
					plinth.MoveToWorld( loc, map );
					statue.InvalidatePose();

					from.CloseGump( typeof( CharacterStatueGump ) );
					from.SendGump( new CharacterStatueGump( m_Maker, statue, from ) );
				}
				else if ( result == AddonFitResult.Blocked )
					from.SendLocalizedMessage( 500269 ); // You cannot build that there.
				else if ( result == AddonFitResult.NotInHouse )
					from.SendLocalizedMessage( 1076192 ); // Statues can only be placed in houses where you are the owner or co-owner.
				else if ( result == AddonFitResult.DoorsNotClosed )
					from.SendMessage( "You must close all house doors before placing this." );
				else if ( result == AddonFitResult.DoorTooClose )
					from.SendLocalizedMessage( 500271 ); // You cannot build near the door.				
			}
			else
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
		}
		
		public static AddonFitResult CouldFit( Point3D p, Map map, Mobile from, ref BaseHouse house )
		{			
			if ( !map.CanFit( p.X, p.Y, p.Z, 20, true, true, true ) )
				return AddonFitResult.Blocked;
			else if ( !BaseAddon.CheckHouse( from, p, map, 20, ref house ) )
				return AddonFitResult.NotInHouse;
			else
				return CheckDoors( p, 20, house );
		}
		
		public static AddonFitResult CheckDoors( Point3D p, int height, BaseHouse house )
		{
			ArrayList doors = house.Doors;

			for ( int i = 0; i < doors.Count; i ++ )
			{
				BaseDoor door = doors[ i ] as BaseDoor;

				if ( door != null && door.Open )
					return AddonFitResult.DoorsNotClosed;

				Point3D doorLoc = door.GetWorldLocation();
				int doorHeight = door.ItemData.CalcHeight;
				
				if ( Utility.InRange( doorLoc, p, 1 ) && (p.Z == doorLoc.Z || ((p.Z + height) > doorLoc.Z && (doorLoc.Z + doorHeight) > p.Z)) )
					return AddonFitResult.DoorTooClose;
			}
			
			return AddonFitResult.Valid;
		}
	}
}
