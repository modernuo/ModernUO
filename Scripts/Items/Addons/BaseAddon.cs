using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Multis;
using Server.Regions;

namespace Server.Items
{
	public enum AddonFitResult
	{
		Valid,
		Blocked,
		NotInHouse,
		DoorsNotClosed,
		DoorTooClose,
		NoWall
	}

	public interface IAddon
	{
		Item Deed{ get; }

		bool CouldFit( IPoint3D p, Map map );
	}

	public abstract class BaseAddon : Item, IChopable, IAddon
	{
		private List<AddonComponent> m_Components;

		public void AddComponent( AddonComponent c, int x, int y, int z )
		{
			if ( Deleted )
				return;

			m_Components.Add( c );

			c.Addon = this;
			c.Offset = new Point3D( x, y, z );
			c.MoveToWorld( new Point3D( X + x, Y + y, Z + z ), Map );
		}

		public BaseAddon() : base( 1 )
		{
			Movable = false;
			Visible = false;

			m_Components = new List<AddonComponent>();
		}

		public virtual bool RetainDeedHue{ get{ return false; } }

		public void OnChop( Mobile from )
		{
			BaseHouse house = BaseHouse.FindHouseAt( this );

			if ( house != null && house.IsOwner( from ) && house.Addons.Contains( this ) )
			{
				Effects.PlaySound( GetWorldLocation(), Map, 0x3B3 );
				from.SendLocalizedMessage( 500461 ); // You destroy the item.

				int hue = 0;

				if ( RetainDeedHue )
				{
					for ( int i = 0; hue == 0 && i < m_Components.Count; ++i )
					{
						AddonComponent c = m_Components[i];

						if ( c.Hue != 0 )
							hue = c.Hue;
					}
				}

				Delete();

				house.Addons.Remove( this );

				BaseAddonDeed deed = Deed;

				if ( deed != null )
				{
					if ( RetainDeedHue )
						deed.Hue = hue;

					from.AddToBackpack( deed );
				}
			}
		}

		public virtual BaseAddonDeed Deed{ get{ return null; } }

		Item IAddon.Deed
		{
			get{ return this.Deed; }
		}

		public List<AddonComponent> Components
		{
			get
			{
				return m_Components;
			}
		}

		public BaseAddon( Serial serial ) : base( serial )
		{
		}

		public bool CouldFit( IPoint3D p, Map map )
		{
			BaseHouse h = null;
			return ( CouldFit( p, map, null, ref h ) == AddonFitResult.Valid );
		}

		public AddonFitResult CouldFit( IPoint3D p, Map map, Mobile from, ref BaseHouse house )
		{
			if ( Deleted )
				return AddonFitResult.Blocked;

			foreach ( AddonComponent c in m_Components )
			{
				Point3D p3D = new Point3D( p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z );

				if ( !map.CanFit( p3D.X, p3D.Y, p3D.Z, c.ItemData.Height, false, true, ( c.Z == 0 ) ) )
					return AddonFitResult.Blocked;
				else if ( !CheckHouse( from, p3D, map, c.ItemData.Height, ref house ) )
					return AddonFitResult.NotInHouse;

				if ( c.NeedsWall )
				{
					Point3D wall = c.WallPosition;

					if ( !IsWall( p3D.X + wall.X, p3D.Y + wall.Y, p3D.Z + wall.Z, map ) )
						return AddonFitResult.NoWall;
				}
			}

			ArrayList doors = house.Doors;

			for ( int i = 0; i < doors.Count; ++i )
			{
				BaseDoor door = doors[i] as BaseDoor;

				if ( door != null && door.Open )
					return AddonFitResult.DoorsNotClosed;

				Point3D doorLoc = door.GetWorldLocation();
				int doorHeight = door.ItemData.CalcHeight;

				foreach ( AddonComponent c in m_Components )
				{
					Point3D addonLoc = new Point3D( p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z );
					int addonHeight = c.ItemData.CalcHeight;
						
					if ( Utility.InRange( doorLoc, addonLoc, 1 ) && (addonLoc.Z == doorLoc.Z || ((addonLoc.Z + addonHeight) > doorLoc.Z && (doorLoc.Z + doorHeight) > addonLoc.Z)) )
						return AddonFitResult.DoorTooClose;
				}
			}

			return AddonFitResult.Valid;
		}

		public static bool CheckHouse( Mobile from, Point3D p, Map map, int height, ref BaseHouse house )
		{
			house = BaseHouse.FindHouseAt( p, map, height );

			if ( from == null || house == null || !house.IsOwner( from ) )
				return false;

			return true;
		}

		public static bool IsWall( int x, int y, int z, Map map )
		{
			if ( map == null )
				return false;

			Tile[] tiles = map.Tiles.GetStaticTiles( x, y, true );

			for ( int i = 0; i < tiles.Length; ++i )
			{
				Tile t = tiles[i];
				ItemData id = TileData.ItemTable[t.ID & 0x3FFF];

				if ( (id.Flags & TileFlag.Wall) != 0 && (z + 16) > t.Z && (t.Z + t.Height) > z )
					return true;
			}

			return false;
		}

		public virtual void OnComponentLoaded( AddonComponent c )
		{
		}

		public virtual void OnComponentUsed( AddonComponent c, Mobile from )
		{
		}

		public override void OnLocationChange( Point3D oldLoc )
		{
			if ( Deleted )
				return;

			foreach ( AddonComponent c in m_Components )
				c.Location = new Point3D( X + c.Offset.X, Y + c.Offset.Y, Z + c.Offset.Z );
		}

		public override void OnMapChange()
		{
			if ( Deleted )
				return;

			foreach ( AddonComponent c in m_Components )
				c.Map = Map;
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			foreach ( AddonComponent c in m_Components )
				c.Delete();
		}

		public virtual bool ShareHue{ get{ return true; } }

		[Hue, CommandProperty( AccessLevel.GameMaster )]
		public override int Hue
		{
			get
			{
				return base.Hue;
			}
			set
			{
				if ( base.Hue != value )
				{
					base.Hue = value;

					if ( !Deleted && this.ShareHue && m_Components != null )
					{
						foreach ( AddonComponent c in m_Components )
							c.Hue = value;
					}
				}
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.WriteItemList<AddonComponent>( m_Components );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				case 0:
				{
					m_Components = reader.ReadStrongItemList<AddonComponent>();
					break;
				}
			}

			if ( version < 1 && Weight == 0 )
				Weight = -1;
		}
	}
}