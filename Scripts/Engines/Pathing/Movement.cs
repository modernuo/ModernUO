using System;
using System.Collections;
using System.Collections.Generic;
using Server;

namespace Server.Movement
{
	public class MovementImpl : IMovementImpl
	{
		private const int PersonHeight = 16;
		private const int StepHeight = 2;

		private const TileFlag ImpassableSurface = TileFlag.Impassable | TileFlag.Surface;

		private static bool m_AlwaysIgnoreDoors;
		private static bool m_IgnoreMovableImpassables;

		public static bool AlwaysIgnoreDoors{ get{ return m_AlwaysIgnoreDoors; } set{ m_AlwaysIgnoreDoors = value; } }
		public static bool IgnoreMovableImpassables{ get{ return m_IgnoreMovableImpassables; } set{ m_IgnoreMovableImpassables = value; } }

		public static void Configure()
		{
			Movement.Impl = new MovementImpl();
		}

		private MovementImpl()
		{
		}

		private bool IsOk( bool ignoreDoors, int ourZ, int ourTop, Tile[] tiles, List<Item> items )
		{
			for ( int i = 0; i < tiles.Length; ++i )
			{
				Tile check = tiles[i];
				ItemData itemData = TileData.ItemTable[check.ID & 0x3FFF];

				if ( (itemData.Flags & ImpassableSurface) != 0 ) // Impassable || Surface
				{
					int checkZ = check.Z;
					int checkTop = checkZ + itemData.CalcHeight;

					if ( checkTop > ourZ && ourTop > checkZ )
						return false;
				}
			}

			for ( int i = 0; i < items.Count; ++i )
			{
				Item item = items[i];
				int itemID = item.ItemID & 0x3FFF;
				ItemData itemData = TileData.ItemTable[itemID];
				TileFlag flags = itemData.Flags;

				if ( (flags & ImpassableSurface) != 0 ) // Impassable || Surface
				{
					if ( ignoreDoors && ((flags & TileFlag.Door) != 0 || itemID == 0x692 || itemID == 0x846 || itemID == 0x873 || (itemID >= 0x6F5 && itemID <= 0x6F6)) )
						continue;

					int checkZ = item.Z;
					int checkTop = checkZ + itemData.CalcHeight;

					if ( checkTop > ourZ && ourTop > checkZ )
						return false;
				}
			}

			return true;
		}

		private List<Item>[] m_Pools = new List<Item>[4]
			{
				new List<Item>(), new List<Item>(),
				new List<Item>(), new List<Item>(),
			};
		
		private List<Sector> m_Sectors = new List<Sector>();

		private bool Check( Map map, Mobile m, List<Item> items, int x, int y, int startTop, int startZ, bool canSwim, bool cantWalk, out int newZ )
		{
			newZ = 0;

			Tile[] tiles = map.Tiles.GetStaticTiles( x, y, true );
			Tile landTile = map.Tiles.GetLandTile( x, y );

			bool landBlocks = (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Impassable) != 0;
			bool considerLand = !landTile.Ignored;

			if ( landBlocks && canSwim && (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Wet) != 0 )	//Impassable, Can Swim, and Is water.  Don't block it.
				landBlocks = false;
			else if ( cantWalk && (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Wet) == 0 )	//Can't walk and it's not water
				landBlocks = true;

			int landZ = 0, landCenter = 0, landTop = 0;

			map.GetAverageZ( x, y, ref landZ, ref landCenter, ref landTop );

			bool moveIsOk = false;

			int stepTop = startTop + StepHeight;
			int checkTop = startZ + PersonHeight;

			bool ignoreDoors = ( m_AlwaysIgnoreDoors || !m.Alive || m.Body.BodyID == 0x3DB || m.IsDeadBondedPet );

			#region Tiles
			for ( int i = 0; i < tiles.Length; ++i )
			{
				Tile tile = tiles[i];
				ItemData itemData = TileData.ItemTable[tile.ID & 0x3FFF];
				TileFlag flags = itemData.Flags;

				if ( (flags & ImpassableSurface) == TileFlag.Surface || (canSwim && (flags & TileFlag.Wet) != 0) ) // Surface && !Impassable
				{
					if ( cantWalk && (flags & TileFlag.Wet) == 0 )
						continue;

					int itemZ = tile.Z;
					int itemTop = itemZ;
					int ourZ = itemZ + itemData.CalcHeight;
					int ourTop = ourZ + PersonHeight;
					int testTop = checkTop;

					if ( moveIsOk )
					{
						int cmp = Math.Abs( ourZ - m.Z ) - Math.Abs( newZ - m.Z );

						if ( cmp > 0 || (cmp == 0 && ourZ > newZ) )
							continue;
					}

					if ( ourZ + PersonHeight > testTop )
						testTop = ourZ + PersonHeight;

					if ( !itemData.Bridge )
						itemTop += itemData.Height;

					if ( stepTop >= itemTop )
					{
						int landCheck = itemZ;

						if ( itemData.Height >= StepHeight )
							landCheck += StepHeight;
						else
							landCheck += itemData.Height;

						if ( considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ )
							continue;

						if ( IsOk( ignoreDoors, ourZ, testTop, tiles, items ) )
						{
							newZ = ourZ;
							moveIsOk = true;
						}
					}
				}
			}
			#endregion

			#region Items
			for ( int i = 0; i < items.Count; ++i )
			{
				Item item = items[i];
				ItemData itemData = item.ItemData;
				TileFlag flags = itemData.Flags;

				if ( !item.Movable && ((flags & ImpassableSurface) == TileFlag.Surface || (m.CanSwim && (flags & TileFlag.Wet) != 0)) ) // Surface && !Impassable && !Movable
				{
					if ( cantWalk && (flags & TileFlag.Wet) == 0 )
						continue;

					int itemZ = item.Z;
					int itemTop = itemZ;
					int ourZ = itemZ + itemData.CalcHeight;
					int ourTop = ourZ + PersonHeight;
					int testTop = checkTop;

					if ( moveIsOk )
					{
						int cmp = Math.Abs( ourZ - m.Z ) - Math.Abs( newZ - m.Z );

						if ( cmp > 0 || (cmp == 0 && ourZ > newZ) )
							continue;
					}

					if ( ourZ + PersonHeight > testTop )
						testTop = ourZ + PersonHeight;

					if ( !itemData.Bridge )
						itemTop += itemData.Height;

					if ( stepTop >= itemTop )
					{
						int landCheck = itemZ;

						if ( itemData.Height >= StepHeight )
							landCheck += StepHeight;
						else
							landCheck += itemData.Height;

						if ( considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ )
							continue;

						if ( IsOk( ignoreDoors, ourZ, testTop, tiles, items ) )
						{
							newZ = ourZ;
							moveIsOk = true;
						}
					}
				}
			}

			#endregion

			if ( considerLand && !landBlocks && stepTop >= landZ )
			{
				int ourZ = landCenter;
				int ourTop = ourZ + PersonHeight;
				int testTop = checkTop;

				if ( ourZ + PersonHeight > testTop )
					testTop = ourZ + PersonHeight;

				bool shouldCheck = true;

				if ( moveIsOk )
				{
					int cmp = Math.Abs( ourZ - m.Z ) - Math.Abs( newZ - m.Z );

					if ( cmp > 0 || (cmp == 0 && ourZ > newZ) )
						shouldCheck = false;
				}

				if ( shouldCheck && IsOk( ignoreDoors, ourZ, testTop, tiles, items ) )
				{
					newZ = ourZ;
					moveIsOk = true;
				}
			}

			return moveIsOk;
		}

		public bool CheckMovement( Mobile m, Map map, Point3D loc, Direction d, out int newZ )
		{
			if ( map == null || map == Map.Internal )
			{
				newZ = 0;
				return false;
			}

			int xStart = loc.X;
			int yStart = loc.Y;
			int xForward = xStart, yForward = yStart;
			int xRight = xStart, yRight = yStart;
			int xLeft = xStart, yLeft = yStart;

			bool checkDiagonals = ((int)d & 0x1) == 0x1;

			Offset( d, ref xForward, ref yForward );
			Offset( (Direction)(((int)d - 1) & 0x7), ref xLeft, ref yLeft );
			Offset( (Direction)(((int)d + 1) & 0x7), ref xRight, ref yRight );

			if ( xForward < 0 || yForward < 0 || xForward >= map.Width || yForward >= map.Height )
			{
				newZ = 0;
				return false;
			}

			int startZ, startTop;

			List<Item> itemsStart = m_Pools[0];
			List<Item> itemsForward = m_Pools[1];
			List<Item> itemsLeft = m_Pools[2];
			List<Item> itemsRight = m_Pools[3];

			bool ignoreMovableImpassables = m_IgnoreMovableImpassables;
			TileFlag reqFlags = ImpassableSurface;

			if ( m.CanSwim )
				reqFlags |= TileFlag.Wet;

			if ( checkDiagonals )
			{
				Sector sectorStart = map.GetSector( xStart, yStart );
				Sector sectorForward = map.GetSector( xForward, yForward );
				Sector sectorLeft = map.GetSector( xLeft, yLeft );
				Sector sectorRight = map.GetSector( xRight, yRight );

				List<Sector> sectors = m_Sectors;

				sectors.Add( sectorStart );

				if ( !sectors.Contains( sectorForward ) )
					sectors.Add( sectorForward );

				if ( !sectors.Contains( sectorLeft ) )
					sectors.Add( sectorLeft );

				if ( !sectors.Contains( sectorRight ) )
					sectors.Add( sectorRight );

				for ( int i = 0; i < sectors.Count; ++i )
				{
					Sector sector = sectors[i];

					for ( int j = 0; j < sector.Items.Count; ++j )
					{
						Item item = sector.Items[j];

						if ( ignoreMovableImpassables && item.Movable && item.ItemData.Impassable )
							continue;

						if ( (item.ItemData.Flags & reqFlags) == 0 )
							continue;

						if ( sector == sectorStart && item.AtWorldPoint( xStart, yStart ) && item.ItemID < 0x4000 )
							itemsStart.Add( item );
						else if ( sector == sectorForward && item.AtWorldPoint( xForward, yForward ) && item.ItemID < 0x4000 )
							itemsForward.Add( item );
						else if ( sector == sectorLeft && item.AtWorldPoint( xLeft, yLeft ) && item.ItemID < 0x4000 )
							itemsLeft.Add( item );
						else if ( sector == sectorRight && item.AtWorldPoint( xRight, yRight ) && item.ItemID < 0x4000 )
							itemsRight.Add( item );
					}
				}

				if ( m_Sectors.Count > 0 )
					m_Sectors.Clear();
			}
			else
			{
				Sector sectorStart = map.GetSector( xStart, yStart );
				Sector sectorForward = map.GetSector( xForward, yForward );

				if ( sectorStart == sectorForward )
				{
					for ( int i = 0; i < sectorStart.Items.Count; ++i )
					{
						Item item = sectorStart.Items[i];

						if ( ignoreMovableImpassables && item.Movable && item.ItemData.Impassable )
							continue;

						if ( (item.ItemData.Flags & reqFlags) == 0 )
							continue;

						if ( item.AtWorldPoint( xStart, yStart ) && item.ItemID < 0x4000 )
							itemsStart.Add( item );
						else if ( item.AtWorldPoint( xForward, yForward ) && item.ItemID < 0x4000 )
							itemsForward.Add( item );
					}
				}
				else
				{
					for ( int i = 0; i < sectorForward.Items.Count; ++i )
					{
						Item item = sectorForward.Items[i];

						if ( ignoreMovableImpassables && item.Movable && item.ItemData.Impassable )
							continue;

						if ( (item.ItemData.Flags & reqFlags) == 0 )
							continue;

						if ( item.AtWorldPoint( xForward, yForward ) && item.ItemID < 0x4000 )
							itemsForward.Add( item );
					}

					for ( int i = 0; i < sectorStart.Items.Count; ++i )
					{
						Item item = sectorStart.Items[i];

						if ( ignoreMovableImpassables && item.Movable && item.ItemData.Impassable )
							continue;

						if ( (item.ItemData.Flags & reqFlags) == 0 )
							continue;

						if ( item.AtWorldPoint( xStart, yStart ) && item.ItemID < 0x4000 )
							itemsStart.Add( item );
					}
				}
			}

			GetStartZ( m, map, loc, itemsStart, out startZ, out startTop );

			bool moveIsOk = Check( map, m, itemsForward, xForward, yForward, startTop, startZ, m.CanSwim, m.CantWalk, out newZ );

			if ( moveIsOk && checkDiagonals )
			{
				int hold;

				if ( m.Player && m.AccessLevel < AccessLevel.GameMaster ) {
					if ( !Check( map, m, itemsLeft, xLeft, yLeft, startTop, startZ, m.CanSwim, m.CantWalk, out hold ) || !Check( map, m, itemsRight, xRight, yRight, startTop, startZ, m.CanSwim, m.CantWalk, out hold ) )
						moveIsOk = false;
				} else {
					if ( !Check( map, m, itemsLeft, xLeft, yLeft, startTop, startZ, m.CanSwim, m.CantWalk, out hold ) && !Check( map, m, itemsRight, xRight, yRight, startTop, startZ, m.CanSwim, m.CantWalk, out hold ) )
						moveIsOk = false;
				}
			}

			for ( int i = 0; i < (checkDiagonals ? 4 : 2); ++i )
			{
				if ( m_Pools[i].Count > 0 )
					m_Pools[i].Clear();
			}

			if ( !moveIsOk )
				newZ = startZ;

			return moveIsOk;
		}

		public bool CheckMovement( Mobile m, Direction d, out int newZ )
		{
			return CheckMovement( m, m.Map, m.Location, d, out newZ );
		}

		private void GetStartZ( Mobile m, Map map, Point3D loc, List<Item> itemList, out int zLow, out int zTop )
		{
			int xCheck = loc.X, yCheck = loc.Y;

			Tile landTile = map.Tiles.GetLandTile( xCheck, yCheck );
			int landZ = 0, landCenter = 0, landTop = 0;
			bool landBlocks = (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Impassable) != 0;

			if ( landBlocks && m.CanSwim && (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Wet) != 0 )
				landBlocks = false;
			else if ( m.CantWalk && (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Wet) == 0 )
				landBlocks = true;

			map.GetAverageZ( xCheck, yCheck, ref landZ, ref landCenter, ref landTop );

			bool considerLand = !landTile.Ignored;

			int zCenter = zLow = zTop = 0;
			bool isSet = false;

			if ( considerLand && !landBlocks && loc.Z >= landCenter )
			{
				zLow = landZ;
				zCenter = landCenter;

				if ( !isSet || landTop > zTop )
					zTop = landTop;

				isSet = true;
			}

			Tile[] staticTiles = map.Tiles.GetStaticTiles( xCheck, yCheck, true );

			for ( int i = 0; i < staticTiles.Length; ++i )
			{
				Tile tile = staticTiles[i];
				ItemData id = TileData.ItemTable[tile.ID & 0x3FFF];

				int calcTop = (tile.Z + id.CalcHeight);

				if ( (!isSet || calcTop >= zCenter) && ( (id.Flags & TileFlag.Surface) != 0 || ( m.CanSwim && (id.Flags&TileFlag.Wet) != 0 ) ) && loc.Z >= calcTop )
				{
					if ( m.CantWalk && (id.Flags & TileFlag.Wet) == 0 )
						continue;

					zLow = tile.Z;
					zCenter = calcTop;

					int top = tile.Z + id.Height;

					if ( !isSet || top > zTop )
						zTop = top;

					isSet = true;
				}
			}

			for ( int i = 0; i < itemList.Count; ++i )
			{
				Item item = itemList[i];

				ItemData id = item.ItemData;

				int calcTop = item.Z + id.CalcHeight;

				if ( (!isSet || calcTop >= zCenter) && ( (id.Flags & TileFlag.Surface) != 0 || ( m.CanSwim && (id.Flags&TileFlag.Wet) != 0 ) ) && loc.Z >= calcTop )
				{
					if ( m.CantWalk && (id.Flags & TileFlag.Wet) == 0 )
						continue;

					zLow = item.Z;
					zCenter = calcTop;

					int top = item.Z + id.Height;

					if ( !isSet || top > zTop )
						zTop = top;

					isSet = true;
				}
			}

			if ( !isSet )
				zLow = zTop = loc.Z;
			else if ( loc.Z > zTop )
				zTop = loc.Z;
		}

		public void Offset( Direction d, ref int x, ref int y )
		{
			switch ( d & Direction.Mask )
			{
				case Direction.North: --y; break;
				case Direction.South: ++y; break;
				case Direction.West:  --x; break;
				case Direction.East:  ++x; break;
				case Direction.Right: ++x; --y; break;
				case Direction.Left:  --x; ++y; break;
				case Direction.Down:  ++x; ++y; break;
				case Direction.Up:    --x; --y; break;
			}
		}
	}
}