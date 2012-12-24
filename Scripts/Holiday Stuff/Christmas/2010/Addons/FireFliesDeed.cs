using Server;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;
using System;

namespace Server.Items
{
	public class Fireflies : Item, IAddon
	{
		public override int LabelNumber { get { return 1150061; }} 

		public Item Deed
		{
			get
			{
				FirefliesDeed deed = new FirefliesDeed();

				return deed;
			}
		}

		public bool FacingSouth
		{
			get
			{
				if( ItemID == 0x2336 )
					return true;

				return false;
			}
		}

		[Constructable]
		public Fireflies()
			: this( 0x1596 )
		{
		}

		[Constructable]
		public Fireflies( int itemID )
			: base( itemID )
		{
			LootType = LootType.Blessed;
			Movable = false;
		}

		public Fireflies( Serial serial )
			: base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			if( from.InRange( Location, 3 ) )
			{
				BaseHouse house = BaseHouse.FindHouseAt( this );

				if( house != null && house.IsOwner( from ) )
				{
					from.CloseGump( typeof( RewardDemolitionGump ) );
					from.SendGump( new RewardDemolitionGump( this, 1049783 ) ); // Do you wish to re-deed this decoration?
				}
				else
					from.SendLocalizedMessage( 1049784 ); // You can only re-deed this decoration if you are the house owner or originally placed the decoration.
			}
			else
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}

		public bool CouldFit( IPoint3D p, Map map )
		{
			if( map == null || !map.CanFit( p.X, p.Y, p.Z, ItemData.Height ) )
				return false;

			if( FacingSouth )
				return BaseAddon.IsWall( p.X, p.Y - 1, p.Z, map ); // north wall
			else
				return BaseAddon.IsWall( p.X - 1, p.Y, p.Z, map ); // west wall
		}
	}

	public class FirefliesDeed : Item
	{
		public override int LabelNumber { get { return 1150061; } } 

		[Constructable]
		public FirefliesDeed()
			: base( 0x14F0 )
		{
			LootType = LootType.Blessed;
			Weight = 1.0;
		}

		public FirefliesDeed( Serial serial )
			: base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			if( IsChildOf( from.Backpack ) )
			{
				BaseHouse house = BaseHouse.FindHouseAt( from );

				if( house != null && house.IsOwner( from ) )
				{
					from.CloseGump( typeof( FacingGump ) );

					if( !from.SendGump( new FacingGump( this, from ) ) )
					{
						from.SendLocalizedMessage( 1150062  ); // You fail to re-deed the holiday fireflies.
					}
				}
				else
					from.SendLocalizedMessage( 502092 ); // You must be in your house to do this.
			}
			else
				from.SendLocalizedMessage( 1042038 ); // You must have the object in your backpack to use it.
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}

		private class FacingGump : Gump
		{
			private FirefliesDeed m_Deed;
			private Mobile m_Placer;

			private enum Buttons
			{
				Cancel,
				South,
				East
			}

			public FacingGump( FirefliesDeed deed, Mobile player )
				: base( 150, 50 )
			{
				m_Deed = deed;
				m_Placer = player;

				Closable = true;
				Disposable = true;
				Dragable = true;
				Resizable = false;

				AddPage( 0 );

				AddBackground( 0, 0, 300, 150, 0xA28 );

				AddItem( 90, 30, 0x2332 );
				AddItem( 180, 30, 0x2336 );

				AddButton( 50, 35, 0x868, 0x869, ( int )Buttons.East, GumpButtonType.Reply, 0 );
				AddButton( 145, 35, 0x868, 0x869, ( int )Buttons.South, GumpButtonType.Reply, 0 );
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				int m_ItemID;

				switch( (int)info.ButtonID )
				{
					case ( int )Buttons.East: m_ItemID = 0x2332;  break;
					case ( int )Buttons.South: m_ItemID = 0x2336; break;
					default: return;
				}

				m_Placer.Target = new InternalTarget( m_Deed,  m_ItemID  );

			}
		}

		private class InternalTarget : Target
		{
			private FirefliesDeed m_FirefliesDeed;
			private int m_ItemID;

			public InternalTarget( FirefliesDeed m_Deed, int itemid )
				: base( -1, true, TargetFlags.None )
			{
				m_FirefliesDeed = m_Deed;
				m_ItemID = itemid;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if( m_FirefliesDeed == null || m_FirefliesDeed.Deleted )
					return;

				if( m_FirefliesDeed.IsChildOf( from.Backpack ) )
				{
					BaseHouse house = BaseHouse.FindHouseAt( from );

					if( house != null && house.IsOwner( from ) )
					{
						IPoint3D p = targeted as IPoint3D;
						Map map = from.Map;

						if( p == null || map == null || map == Map.Internal )
							return;

						Point3D p3d = new Point3D( p );
						ItemData id = TileData.ItemTable[ m_ItemID & TileData.MaxItemValue ];

						if( map.CanFit( p3d, id.Height ) )
						{
							house = BaseHouse.FindHouseAt( p3d, map, id.Height );

							if( house != null && house.IsOwner( from ) )
							{
								bool north = BaseAddon.IsWall( p3d.X, p3d.Y - 1, p3d.Z, map );
								bool west = BaseAddon.IsWall( p3d.X - 1, p3d.Y, p3d.Z, map );

								bool isclear = true;

								foreach( Item item in Map.Malas.GetItemsInRange( p3d, 0 ) )
								{
									if( item is Fireflies )
									{
										isclear = false;
									}
								}

								if( ( ( m_ItemID == 0x2336 && north ) || ( m_ItemID == 0x2332 && west ) ) && isclear )
								{
									Fireflies flies = new Fireflies( m_ItemID );

									house.Addons.Add( flies );

									flies.MoveToWorld( p3d, from.Map );

									m_FirefliesDeed.Delete();
								}

								else
									from.SendLocalizedMessage( 1150065 ); // Holiday fireflies must be placed next to a wall.
							}
							else
								from.SendLocalizedMessage( 1042036 ); // That location is not in your house.
						}
						else
							from.SendLocalizedMessage( 500269 ); // You cannot build that there.
					}
					else
						from.SendLocalizedMessage( 502092 ); // You must be in your house to do this.
				}
				else
					from.SendLocalizedMessage( 1042038 ); // You must have the object in your backpack to use it.
			}
		}
	}
}