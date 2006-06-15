using System;
using System.Collections;

namespace Server.Items
{
	public class Chessboard : BaseBoard
	{
		public override int LabelNumber{ get{ return 1016450; } } // a chessboard

		[Constructable]
		public Chessboard() : base( 0xFA6 )
		{
		}

		public override void CreatePieces()
		{
			for ( int i = 0; i < 8; i++ )
			{
				CreatePiece( new PieceBlackPawn( this ), ( 25 * i ) + 43, 42 );
				CreatePiece( new PieceWhitePawn( this ), ( 25 * i ) + 43, 167 );
			}

			// Rook
			CreatePiece( new PieceBlackRook( this ), 42, 5 );
			CreatePiece( new PieceBlackRook( this ), 216, 5 );

			CreatePiece( new PieceWhiteRook( this ), 42, 180 );
			CreatePiece( new PieceWhiteRook( this ), 216, 180 );

			// Knight
			CreatePiece( new PieceBlackKnight( this ), 66, 7 );
			CreatePiece( new PieceBlackKnight( this ), 190, 7 );

			CreatePiece( new PieceWhiteKnight( this ), 66, 182 );
			CreatePiece( new PieceWhiteKnight( this ), 190, 182 );
					
			// Bishop
			CreatePiece( new PieceBlackBishop( this ), 94, 7 );
			CreatePiece( new PieceBlackBishop( this ), 168, 7 );

			CreatePiece( new PieceWhiteBishop( this ), 94, 182 );
			CreatePiece( new PieceWhiteBishop( this ), 168, 182 );
			
			// Queen
			CreatePiece( new PieceBlackQueen( this ), 142, 5 );
			CreatePiece( new PieceWhiteQueen( this ), 142, 180 );

			// King
			CreatePiece( new PieceBlackKing( this ), 117, 5 );
			CreatePiece( new PieceWhiteKing( this ), 117, 180 );
		}

		public Chessboard( Serial serial ) : base( serial )
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