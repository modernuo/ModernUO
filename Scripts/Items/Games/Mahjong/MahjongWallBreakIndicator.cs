namespace Server.Engines.Mahjong
{
	public class MahjongWallBreakIndicator
	{
		public static MahjongPieceDim GetDimensions( Point2D position )
		{
			return new MahjongPieceDim( position, 20, 20 );
		}

		public MahjongGame Game { get; }

		public Point2D Position { get; private set; }

		public MahjongWallBreakIndicator( MahjongGame game, Point2D position )
		{
			Game = game;
			Position = position;
		}

		public MahjongPieceDim Dimensions => GetDimensions( Position );

		public void Move( Point2D position )
		{
			MahjongPieceDim dim = GetDimensions( position );

			if ( !dim.IsValid() )
				return;

			Position = position;

			Game.Players.SendGeneralPacket( true, true );
		}

		public void Save( GenericWriter writer )
		{
			writer.Write( (int) 0 ); // version

			writer.Write( Position );
		}

		public MahjongWallBreakIndicator( MahjongGame game, GenericReader reader )
		{
			Game = game;

			int version = reader.ReadInt();

			Position = reader.ReadPoint2D();
		}
	}
}
