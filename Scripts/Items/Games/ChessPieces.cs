using System;
using Server;

namespace Server.Items
{
	public class PieceWhiteKing : BasePiece
	{
		public override string DefaultName
		{
			get { return "white king"; }
		}

		public PieceWhiteKing( BaseBoard board ) : base( 0x3587, board )
		{
		}

		public PieceWhiteKing( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceBlackKing : BasePiece
	{
		public override string DefaultName
		{
			get { return "black king"; }
		}

		public PieceBlackKing( BaseBoard board ) : base( 0x358E, board )
		{
		}

		public PieceBlackKing( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceWhiteQueen : BasePiece
	{
		public override string DefaultName
		{
			get { return "white queen"; }
		}

		public PieceWhiteQueen( BaseBoard board ) : base( 0x358A, board )
		{
		}

		public PieceWhiteQueen( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceBlackQueen : BasePiece
	{
		public override string DefaultName
		{
			get { return "black queen"; }
		}

		public PieceBlackQueen( BaseBoard board ) : base( 0x3591, board )
		{
		}

		public PieceBlackQueen( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceWhiteRook : BasePiece
	{
		public override string DefaultName
		{
			get { return "white rook"; }
		}

		public PieceWhiteRook( BaseBoard board ) : base( 0x3586, board )
		{
		}

		public PieceWhiteRook( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceBlackRook : BasePiece
	{
		public override string DefaultName
		{
			get { return "black rook"; }
		}

		public PieceBlackRook( BaseBoard board ) : base( 0x358D, board )
		{
		}

		public PieceBlackRook( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceWhiteBishop : BasePiece
	{
		public override string DefaultName
		{
			get { return "white bishop"; }
		}

		public PieceWhiteBishop( BaseBoard board ) : base( 0x3585, board )
		{
		}

		public PieceWhiteBishop( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceBlackBishop : BasePiece
	{
		public override string DefaultName
		{
			get { return "black bishop"; }
		}

		public PieceBlackBishop( BaseBoard board ) : base( 0x358C, board )
		{
		}

		public PieceBlackBishop( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceWhiteKnight : BasePiece
	{
		public override string DefaultName
		{
			get { return "white knight"; }
		}

		public PieceWhiteKnight( BaseBoard board ) : base( 0x3588, board )
		{
		}

		public PieceWhiteKnight( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceBlackKnight : BasePiece
	{
		public override string DefaultName
		{
			get { return "black knight"; }
		}

		public PieceBlackKnight( BaseBoard board ) : base( 0x358F, board )
		{
		}

		public PieceBlackKnight( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceWhitePawn : BasePiece
	{
		public override string DefaultName
		{
			get { return "white pawn"; }
		}

		public PieceWhitePawn( BaseBoard board ) : base( 0x3589, board )
		{
		}

		public PieceWhitePawn( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class PieceBlackPawn : BasePiece
	{
		public override string DefaultName
		{
			get { return "black pawn"; }
		}

		public PieceBlackPawn( BaseBoard board ) : base( 0x3590, board )
		{
		}

		public PieceBlackPawn( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}
