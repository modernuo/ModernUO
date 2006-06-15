using System;
using Server;

namespace Server.Engines.Mahjong
{
	public class MahjongDealerIndicator
	{
		public static MahjongPieceDim GetDimensions( Point2D position, MahjongPieceDirection direction )
		{
			if ( direction == MahjongPieceDirection.Up || direction == MahjongPieceDirection.Down )
				return new MahjongPieceDim( position, 40, 20 );
			else
				return new MahjongPieceDim( position, 20, 40 );
		}

		private MahjongGame m_Game;
		private Point2D m_Position;
		private MahjongPieceDirection m_Direction;
		private MahjongWind m_Wind;

		public MahjongGame Game { get { return m_Game; } }
		public Point2D Position { get { return m_Position; } }
		public MahjongPieceDirection Direction { get { return m_Direction; } }
		public MahjongWind Wind { get { return m_Wind; } }

		public MahjongDealerIndicator( MahjongGame game, Point2D position, MahjongPieceDirection direction, MahjongWind wind )
		{
			m_Game = game;
			m_Position = position;
			m_Direction = direction;
			m_Wind = wind;
		}

		public MahjongPieceDim Dimensions
		{
			get { return GetDimensions( m_Position, m_Direction ); }
		}

		public void Move( Point2D position, MahjongPieceDirection direction, MahjongWind wind )
		{
			MahjongPieceDim dim = GetDimensions( position, direction );

			if ( !dim.IsValid() )
				return;

			m_Position = position;
			m_Direction = direction;
			m_Wind = wind;

			m_Game.Players.SendGeneralPacket( true, true );
		}

		public void Save( GenericWriter writer )
		{
			writer.Write( (int) 0 ); // version

			writer.Write( m_Position );
			writer.Write( (int) m_Direction );
			writer.Write( (int) m_Wind );
		}

		public MahjongDealerIndicator( MahjongGame game, GenericReader reader )
		{
			m_Game = game;

			int version = reader.ReadInt();

			m_Position = reader.ReadPoint2D();
			m_Direction = (MahjongPieceDirection) reader.ReadInt();
			m_Wind = (MahjongWind) reader.ReadInt();
		}
	}
}