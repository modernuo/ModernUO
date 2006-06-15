/***************************************************************************
 *                            TileMatrixPatch.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: TileMatrixPatch.cs 104 2006-02-04 07:04:40Z mark $
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.IO;

namespace Server
{
	public class TileMatrixPatch
	{
		private int m_LandBlocks, m_StaticBlocks;

		private static bool m_Enabled = true;

		public static bool Enabled
		{
			get
			{
				return m_Enabled;
			}
			set
			{
				m_Enabled = value;
			}
		}

#if !MONO
		[System.Runtime.InteropServices.DllImport( "Kernel32" )]
		private unsafe static extern int _lread( IntPtr hFile, void *lpBuffer, int wBytes );
#endif

		public int LandBlocks
		{
			get
			{
				return m_LandBlocks;
			}
		}

		public int StaticBlocks
		{
			get
			{
				return m_StaticBlocks;
			}
		}

		public TileMatrixPatch( TileMatrix matrix, int index )
		{
			if ( !m_Enabled )
				return;

			string mapDataPath = Core.FindDataFile( "mapdif{0}.mul", index );
			string mapIndexPath = Core.FindDataFile( "mapdifl{0}.mul", index );

			if ( File.Exists( mapDataPath ) && File.Exists( mapIndexPath ) )
				m_LandBlocks = PatchLand( matrix, mapDataPath, mapIndexPath );

			string staDataPath = Core.FindDataFile( "stadif{0}.mul", index );
			string staIndexPath = Core.FindDataFile( "stadifl{0}.mul", index );
			string staLookupPath = Core.FindDataFile( "stadifi{0}.mul", index );

			if ( File.Exists( staDataPath ) && File.Exists( staIndexPath ) && File.Exists( staLookupPath ) )
				m_StaticBlocks = PatchStatics( matrix, staDataPath, staIndexPath, staLookupPath );
		}

		private unsafe int PatchLand( TileMatrix matrix, string dataPath, string indexPath )
		{
			using ( FileStream fsData = new FileStream( dataPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			{
				using ( FileStream fsIndex = new FileStream( indexPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryReader indexReader = new BinaryReader( fsIndex );

					int count = (int)(indexReader.BaseStream.Length / 4);

					for ( int i = 0; i < count; ++i )
					{
						int blockID = indexReader.ReadInt32();
						int x = blockID / matrix.BlockHeight;
						int y = blockID % matrix.BlockHeight;

						fsData.Seek( 4, SeekOrigin.Current );

						Tile[] tiles = new Tile[64];

						fixed ( Tile *pTiles = tiles )
						{
#if !MONO
							_lread( fsData.SafeFileHandle.DangerousGetHandle(), pTiles, 192 );
#else
							if ( m_Buffer == null || 192 > m_Buffer.Length )
								m_Buffer = new byte[192];

							fsData.Read( m_Buffer, 0, 192 );

							fixed ( byte *pbBuffer = m_Buffer )
							{
								Tile *pBuffer = (Tile *)pbBuffer;
								Tile *pEnd = pBuffer + 64;
								Tile *pCur = pTiles;

								while ( pBuffer < pEnd )
									*pCur++ = *pBuffer++;
							}
#endif
						}

						matrix.SetLandBlock( x, y, tiles );
					}
					
					indexReader.Close();

					return count;
				}
			}
		}

#if MONO
		private static byte[] m_Buffer;
#endif

		private static StaticTile[] m_TileBuffer = new StaticTile[128];

		private unsafe int PatchStatics( TileMatrix matrix, string dataPath, string indexPath, string lookupPath )
		{
			using ( FileStream fsData = new FileStream( dataPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			{
				using ( FileStream fsIndex = new FileStream( indexPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					using ( FileStream fsLookup = new FileStream( lookupPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					{
						BinaryReader indexReader = new BinaryReader( fsIndex );
						BinaryReader lookupReader = new BinaryReader( fsLookup );

						int count = (int)(indexReader.BaseStream.Length / 4);

						TileList[][] lists = new TileList[8][];

						for ( int x = 0; x < 8; ++x )
						{
							lists[x] = new TileList[8];

							for ( int y = 0; y < 8; ++y )
								lists[x][y] = new TileList();
						}

						for ( int i = 0; i < count; ++i )
						{
							int blockID = indexReader.ReadInt32();
							int blockX = blockID / matrix.BlockHeight;
							int blockY = blockID % matrix.BlockHeight;

							int offset = lookupReader.ReadInt32();
							int length = lookupReader.ReadInt32();
							lookupReader.ReadInt32(); // Extra

							if ( offset < 0 || length <= 0 )
							{
								matrix.SetStaticBlock( blockX, blockY, matrix.EmptyStaticBlock );
								continue;
							}

							fsData.Seek( offset, SeekOrigin.Begin );

							int tileCount = length / 7;

							if ( m_TileBuffer.Length < tileCount )
								m_TileBuffer = new StaticTile[tileCount];

							StaticTile[] staTiles = m_TileBuffer;//new StaticTile[tileCount];

							fixed ( StaticTile *pTiles = staTiles )
							{
#if !MONO
								_lread( fsData.SafeFileHandle.DangerousGetHandle(), pTiles, length );
#else
								if ( m_Buffer == null || length > m_Buffer.Length )
									m_Buffer = new byte[length];

								fsData.Read( m_Buffer, 0, length );

								fixed ( byte *pbBuffer = m_Buffer )
								{
									StaticTile *pCopyBuffer = (StaticTile *)pbBuffer;
									StaticTile *pCopyEnd = pCopyBuffer + tileCount;
									StaticTile *pCopyCur = pTiles;

									while ( pCopyBuffer < pCopyEnd )
										*pCopyCur++ = *pCopyBuffer++;
								}
#endif

								StaticTile *pCur = pTiles, pEnd = pTiles + tileCount;

								while ( pCur < pEnd )
								{
									lists[pCur->m_X & 0x7][pCur->m_Y & 0x7].Add( (short)((pCur->m_ID & 0x3FFF) + 0x4000), pCur->m_Z );
									++pCur;
								}

								Tile[][][] tiles = new Tile[8][][];

								for ( int x = 0; x < 8; ++x )
								{
									tiles[x] = new Tile[8][];

									for ( int y = 0; y < 8; ++y )
										tiles[x][y] = lists[x][y].ToArray();
								}

								matrix.SetStaticBlock( blockX, blockY, tiles );
							}
						}

						indexReader.Close();
						lookupReader.Close();

						return count;
					}
				}
			}
		}
	}
}