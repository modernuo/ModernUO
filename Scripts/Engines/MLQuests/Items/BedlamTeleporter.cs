using System;
using Server;
using Server.Engines.MLQuests;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Items
{
	public class BedlamTeleporter : Item
	{
		public override int LabelNumber { get { return 1074161; } } // Access to Bedlam by invitation only

		private static readonly Point3D PointDest = new Point3D( 120, 1682, 0 );
		private static readonly Map MapDest = Map.Malas;

		public BedlamTeleporter()
			: base( 0x124D )
		{
			Movable = false;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
				return;
			}

			MLQuestContext context;

			if ( from is PlayerMobile && ( context = MLQuestSystem.GetContext( (PlayerMobile)from ) ) != null && context.BedlamAccess )
			{
				BaseCreature.TeleportPets( from, PointDest, MapDest );
				from.MoveToWorld( PointDest, MapDest );
			}
			else
			{
				from.SendLocalizedMessage( 1074276 ); // You press and push on the iron maiden, but nothing happens.
			}
		}

		public BedlamTeleporter( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
