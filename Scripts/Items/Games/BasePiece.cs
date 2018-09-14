namespace Server.Items
{
	public class BasePiece : Item
	{
		public BaseBoard Board { get; set; }

		public override bool IsVirtualItem => true;

		public BasePiece( int itemID, BaseBoard board ) : base( itemID )
		{
			Board = board;
		}

		public BasePiece( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
			writer.Write( Board );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					Board = (BaseBoard)reader.ReadItem();

					if ( Board == null || Parent == null )
						Delete();

					break;
				}
			}
		}

		public override void OnSingleClick( Mobile from )
		{
			if ( Board == null || Board.Deleted )
				Delete();
			else if ( !IsChildOf( Board ) )
				Board.DropItem( this );
			else
				base.OnSingleClick( from );
		}

		public override bool OnDragLift( Mobile from )
		{
			if ( Board == null || Board.Deleted )
			{
				Delete();
				return false;
			}

			if ( !IsChildOf( Board ) )
			{
				Board.DropItem( this );
				return false;
			}
			return true;
		}

		public override bool CanTarget => false;

		public override bool DropToMobile( Mobile from, Mobile target, Point3D p )
		{
			return false;
		}

		public override bool DropToItem( Mobile from, Item target, Point3D p )
		{
			return ( target == Board && p.X != -1 && p.Y != -1 && base.DropToItem( from, target, p ) );
		}

		public override bool DropToWorld( Mobile from, Point3D p )
		{
			return false;
		}

		public override int GetLiftSound( Mobile from )
		{
			return -1;
		}
	}
}
