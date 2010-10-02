using System;
using System.Collections.Generic;
using Server;
using Server.Multis;
using Server.Targeting;
using Server.Engines.Craft;

namespace Server.Items
{
	[Flipable( 0x14F0, 0x14EF )]
	public abstract class BaseAddonDeed : Item, ICraftable
	{
		public abstract BaseAddon Addon{ get; }

		public BaseAddonDeed() : base( 0x14F0 )
		{
			Weight = 1.0;

			if ( !Core.AOS )
				LootType = LootType.Newbied;
		}

		public BaseAddonDeed( Serial serial ) : base( serial )
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

			if ( Weight == 0.0 )
				Weight = 1.0;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( IsChildOf( from.Backpack ) )
				from.Target = new InternalTarget( this );
			else
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
		}
		
		#region ICraftable Members
		public virtual int OnCraft( int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue )
		{
			Type resourceType = typeRes;

			if ( resourceType == null )
				resourceType = craftItem.Resources.GetAt( 0 ).ItemType;
			
			CraftResource resource = CraftResources.GetFromType( resourceType );
			
			CraftContext context = craftSystem.GetContext( from );
			
			if ( context != null && context.DoNotColor )
				Hue = 0;
			else if ( resource == CraftResource.None )
				Hue = resHue;
			else
				Hue = CraftResources.GetHue( resource );

			return 1;
		}
		#endregion

		private class InternalTarget : Target
		{
			private BaseAddonDeed m_Deed;

			public InternalTarget( BaseAddonDeed deed ) : base( -1, true, TargetFlags.None )
			{
				m_Deed = deed;

				CheckLOS = false;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				IPoint3D p = targeted as IPoint3D;
				Map map = from.Map;

				if ( p == null || map == null || m_Deed.Deleted )
					return;

				if ( m_Deed.IsChildOf( from.Backpack ) )
				{
					BaseAddon addon = m_Deed.Addon;

					Server.Spells.SpellHelper.GetSurfaceTop( ref p );

					BaseHouse house = null;

					AddonFitResult res = addon.CouldFit( p, map, from, ref house );

					if ( res == AddonFitResult.Valid )
						addon.MoveToWorld( new Point3D( p ), map );
					else if ( res == AddonFitResult.Blocked )
						from.SendLocalizedMessage( 500269 ); // You cannot build that there.
					else if ( res == AddonFitResult.NotInHouse )
						from.SendLocalizedMessage( 500274 ); // You can only place this in a house that you own!
					else if ( res == AddonFitResult.DoorTooClose )
						from.SendLocalizedMessage( 500271 ); // You cannot build near the door.
					else if ( res == AddonFitResult.NoWall )
						from.SendLocalizedMessage( 500268 ); // This object needs to be mounted on something.
					
					if ( res == AddonFitResult.Valid )
					{
						m_Deed.Delete();
						house.Addons.Add( addon );
					}
					else
					{
						addon.Delete();
					}
				}
				else
				{
					from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
				}
			}
		}
	}
}