using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;

namespace Server.Items
{
	public class AddonContainerComponent : Item, IChopable
	{
		public virtual bool NeedsWall { get { return false; } }
		public virtual Point3D WallPosition { get { return Point3D.Zero; } }

		private Point3D m_Offset;
		private BaseAddonContainer m_Addon;

		[CommandProperty( AccessLevel.GameMaster )]
		public BaseAddonContainer Addon
		{
			get { return m_Addon; }
			set { m_Addon = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D Offset
		{
			get { return m_Offset; }
			set { m_Offset = value; }
		}

		[Hue, CommandProperty( AccessLevel.GameMaster )]
		public override int Hue
		{
			get { return base.Hue; }
			set
			{
				base.Hue = value;

				if ( m_Addon != null && m_Addon.ShareHue )
					m_Addon.Hue = value;
			}
		}

		[Constructable]
		public AddonContainerComponent( int itemID ) : base( itemID )
		{
			Movable = false;

			AddonComponent.ApplyLightTo( this );
		}

		public AddonContainerComponent( Serial serial ) : base( serial )
		{
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			if ( Addon != null )
				return Addon.OnDragDrop( from, dropped );

			return false;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( m_Addon != null )
				m_Addon.OnComponentUsed( this, from );
		}

		public override void OnLocationChange( Point3D old )
		{
			if ( m_Addon != null )
				m_Addon.Location = new Point3D( X - m_Offset.X, Y - m_Offset.Y, Z - m_Offset.Z );
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			if ( m_Addon != null )
				m_Addon.GetContextMenuEntries( from, list );
		}

		public override void OnMapChange()
		{
			if ( m_Addon != null )
				m_Addon.Map = Map;
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			if ( m_Addon != null )
				m_Addon.Delete();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_Addon );
			writer.Write( m_Offset );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Addon = reader.ReadItem() as BaseAddonContainer;
			m_Offset = reader.ReadPoint3D();

			if ( m_Addon != null )
				m_Addon.OnComponentLoaded( this );

			AddonComponent.ApplyLightTo( this );
		}

		public virtual void OnChop( Mobile from )
		{
			if ( m_Addon != null && from.InRange( GetWorldLocation(), 3 ) )
				m_Addon.OnChop( from );
			else
				from.SendLocalizedMessage( 500446 ); // That is too far away.
		}
	}

	public class LocalizedContainerComponent : AddonContainerComponent
	{
		private int m_LabelNumber;

		public override int LabelNumber
		{
			get
			{
				if ( m_LabelNumber > 0 )
					return m_LabelNumber;

				return base.LabelNumber;
			}
		}

		public LocalizedContainerComponent( int itemID, int labelNumber ) : base( itemID )
		{
			m_LabelNumber = labelNumber;
		}

		public LocalizedContainerComponent( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_LabelNumber );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_LabelNumber = reader.ReadInt();
		}
	}
}
