using System;
using Server;
using Server.Mobiles;

namespace Server.Engines.Quests
{
	public abstract class DynamicTeleporter : Item
	{
		public override int LabelNumber{ get{ return 1049382; } } // a magical teleporter

		public DynamicTeleporter() : this( 0x1822, 0x482 )
		{
		}

		public DynamicTeleporter( int itemID, int hue ) : base( itemID )
		{
			Movable = false;
			Hue = hue;
		}

		public abstract bool GetDestination( PlayerMobile player, ref Point3D loc, ref Map map );

		public virtual int NotWorkingMessage{ get{ return 500309; } } // Nothing Happens.

		public override bool OnMoveOver( Mobile m )
		{
			PlayerMobile pm = m as PlayerMobile;

			if ( pm != null )
			{
				Point3D loc = Point3D.Zero;
				Map map = null;

				if ( GetDestination( pm, ref loc, ref map ) )
				{
					BaseCreature.TeleportPets( pm, loc, map );

					pm.PlaySound( 0x1FE );
					pm.MoveToWorld( loc, map );

					return false;
				}
				else
				{
					pm.SendLocalizedMessage( this.NotWorkingMessage );
				}
			}

			return base.OnMoveOver( m );
		}

		public DynamicTeleporter( Serial serial ) : base( serial )
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