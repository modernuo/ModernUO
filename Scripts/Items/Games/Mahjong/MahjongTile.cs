namespace Server.Engines.Mahjong
{
	public class MahjongTile
	{
		public static MahjongPieceDim GetDimensions( Point2D position, MahjongPieceDirection direction )
		{
			if ( direction == MahjongPieceDirection.Up || direction == MahjongPieceDirection.Down )
				return new MahjongPieceDim( position, 20, 30 );
			return new MahjongPieceDim( position, 30, 20 );
		}

		private MahjongGame m_Game;
		private int m_Number;
		private MahjongTileType m_Value;
		protected Point2D m_Position;
		private int m_StackLevel;
		private MahjongPieceDirection m_Direction;
		private bool m_Flipped;

		public MahjongGame Game  => m_Game;
		public int Number  => m_Number;
		public MahjongTileType Value  => m_Value;
		public Point2D Position  => m_Position;
		public int StackLevel  => m_StackLevel;
		public MahjongPieceDirection Direction  => m_Direction;
		public bool Flipped  => m_Flipped;

		public MahjongTile( MahjongGame game, int number, MahjongTileType value, Point2D position, int stackLevel, MahjongPieceDirection direction, bool flipped )
		{
			m_Game = game;
			m_Number = number;
			m_Value = value;
			m_Position = position;
			m_StackLevel = stackLevel;
			m_Direction = direction;
			m_Flipped = flipped;
		}

		public MahjongPieceDim Dimensions => GetDimensions( m_Position, m_Direction );

		public bool IsMovable => m_Game.GetStackLevel( Dimensions ) <= m_StackLevel;

		public void Move( Point2D position, MahjongPieceDirection direction, bool flip, int validHandArea )
		{
			MahjongPieceDim dim = GetDimensions( position, direction );
			int curHandArea = Dimensions.GetHandArea();
			int newHandArea = dim.GetHandArea();

			if ( !IsMovable || !dim.IsValid() || ( validHandArea >= 0 && ((curHandArea >= 0 && curHandArea != validHandArea) || (newHandArea >= 0 && newHandArea != validHandArea)) ) )
				return;

			m_Position = position;
			m_Direction = direction;
			m_StackLevel = -1; // Avoid self interference
			m_StackLevel = m_Game.GetStackLevel( dim ) + 1;
			m_Flipped = flip;

			m_Game.Players.SendTilePacket( this, true, true );
		}

		public void Save( GenericWriter writer )
		{
			writer.Write( (int) 0 ); // version

			writer.Write( m_Number );
			writer.Write( (int) m_Value );
			writer.Write( m_Position );
			writer.Write( m_StackLevel );
			writer.Write( (int) m_Direction );
			writer.Write( m_Flipped );
		}

		public MahjongTile( MahjongGame game, GenericReader reader )
		{
			m_Game = game;

			int version = reader.ReadInt();

			m_Number = reader.ReadInt();
			m_Value = (MahjongTileType) reader.ReadInt();
			m_Position = reader.ReadPoint2D();
			m_StackLevel = reader.ReadInt();
			m_Direction = (MahjongPieceDirection) reader.ReadInt();
			m_Flipped = reader.ReadBool();
		}
	}
}
