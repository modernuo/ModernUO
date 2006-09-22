/***************************************************************************
 *                               Compression.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
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
using System.IO;
using System.Runtime.InteropServices;

namespace Server.Network {
	/// <summary>
	/// Handles outgoing packet compression for the network.
	/// </summary>
	public class Compression {
		private static int[] m_Table = new int[514]
		{
			0x2, 0x000,	0x5, 0x01F,	0x6, 0x022,	0x7, 0x034,	0x7, 0x075,	0x6, 0x028,	0x6, 0x03B,	0x7, 0x032,
			0x8, 0x0E0,	0x8, 0x062,	0x7, 0x056,	0x8, 0x079,	0x9, 0x19D,	0x8, 0x097,	0x6, 0x02A,	0x7, 0x057,
			0x8, 0x071,	0x8, 0x05B,	0x9, 0x1CC,	0x8, 0x0A7,	0x7, 0x025,	0x7, 0x04F,	0x8, 0x066,	0x8, 0x07D,
			0x9, 0x191,	0x9, 0x1CE,	0x7, 0x03F,	0x9, 0x090,	0x8, 0x059,	0x8, 0x07B,	0x8, 0x091,	0x8, 0x0C6,
			0x6, 0x02D,	0x9, 0x186,	0x8, 0x06F,	0x9, 0x093,	0xA, 0x1CC,	0x8, 0x05A,	0xA, 0x1AE,	0xA, 0x1C0,
			0x9, 0x148,	0x9, 0x14A,	0x9, 0x082,	0xA, 0x19F,	0x9, 0x171,	0x9, 0x120,	0x9, 0x0E7,	0xA, 0x1F3,
			0x9, 0x14B,	0x9, 0x100,	0x9, 0x190,	0x6, 0x013,	0x9, 0x161,	0x9, 0x125,	0x9, 0x133,	0x9, 0x195,
			0x9, 0x173,	0x9, 0x1CA,	0x9, 0x086,	0x9, 0x1E9,	0x9, 0x0DB,	0x9, 0x1EC,	0x9, 0x08B,	0x9, 0x085,
			0x5, 0x00A,	0x8, 0x096,	0x8, 0x09C,	0x9, 0x1C3,	0x9, 0x19C,	0x9, 0x08F,	0x9, 0x18F,	0x9, 0x091,
			0x9, 0x087,	0x9, 0x0C6,	0x9, 0x177,	0x9, 0x089,	0x9, 0x0D6,	0x9, 0x08C,	0x9, 0x1EE,	0x9, 0x1EB,
			0x9, 0x084,	0x9, 0x164,	0x9, 0x175,	0x9, 0x1CD,	0x8, 0x05E,	0x9, 0x088,	0x9, 0x12B,	0x9, 0x172,
			0x9, 0x10A,	0x9, 0x08D,	0x9, 0x13A,	0x9, 0x11C,	0xA, 0x1E1,	0xA, 0x1E0,	0x9, 0x187,	0xA, 0x1DC,
			0xA, 0x1DF,	0x7, 0x074,	0x9, 0x19F,	0x8, 0x08D,	0x8, 0x0E4,	0x7, 0x079,	0x9, 0x0EA,	0x9, 0x0E1,
			0x8, 0x040,	0x7, 0x041,	0x9, 0x10B,	0x9, 0x0B0,	0x8, 0x06A,	0x8, 0x0C1,	0x7, 0x071,	0x7, 0x078,
			0x8, 0x0B1,	0x9, 0x14C,	0x7, 0x043,	0x8, 0x076,	0x7, 0x066,	0x7, 0x04D,	0x9, 0x08A,	0x6, 0x02F,
			0x8, 0x0C9,	0x9, 0x0CE,	0x9, 0x149,	0x9, 0x160,	0xA, 0x1BA,	0xA, 0x19E,	0xA, 0x39F,	0x9, 0x0E5,
			0x9, 0x194,	0x9, 0x184,	0x9, 0x126,	0x7, 0x030,	0x8, 0x06C,	0x9, 0x121,	0x9, 0x1E8,	0xA, 0x1C1,
			0xA, 0x11D,	0xA, 0x163,	0xA, 0x385,	0xA, 0x3DB,	0xA, 0x17D,	0xA, 0x106,	0xA, 0x397,	0xA, 0x24E,
			0x7, 0x02E,	0x8, 0x098,	0xA, 0x33C,	0xA, 0x32E,	0xA, 0x1E9,	0x9, 0x0BF,	0xA, 0x3DF,	0xA, 0x1DD,
			0xA, 0x32D,	0xA, 0x2ED,	0xA, 0x30B,	0xA, 0x107,	0xA, 0x2E8,	0xA, 0x3DE,	0xA, 0x125,	0xA, 0x1E8,
			0x9, 0x0E9,	0xA, 0x1CD,	0xA, 0x1B5,	0x9, 0x165,	0xA, 0x232,	0xA, 0x2E1,	0xB, 0x3AE,	0xB, 0x3C6,
			0xB, 0x3E2,	0xA, 0x205,	0xA, 0x29A,	0xA, 0x248,	0xA, 0x2CD,	0xA, 0x23B,	0xB, 0x3C5,	0xA, 0x251,
			0xA, 0x2E9,	0xA, 0x252,	0x9, 0x1EA,	0xB, 0x3A0,	0xB, 0x391,	0xA, 0x23C,	0xB, 0x392,	0xB, 0x3D5,
			0xA, 0x233,	0xA, 0x2CC,	0xB, 0x390,	0xA, 0x1BB,	0xB, 0x3A1,	0xB, 0x3C4,	0xA, 0x211,	0xA, 0x203,
			0x9, 0x12A,	0xA, 0x231,	0xB, 0x3E0,	0xA, 0x29B,	0xB, 0x3D7,	0xA, 0x202,	0xB, 0x3AD,	0xA, 0x213,
			0xA, 0x253,	0xA, 0x32C,	0xA, 0x23D,	0xA, 0x23F,	0xA, 0x32F,	0xA, 0x11C,	0xA, 0x384,	0xA, 0x31C,
			0xA, 0x17C,	0xA, 0x30A,	0xA, 0x2E0,	0xA, 0x276,	0xA, 0x250,	0xB, 0x3E3,	0xA, 0x396,	0xA, 0x18F,
			0xA, 0x204,	0xA, 0x206,	0xA, 0x230,	0xA, 0x265,	0xA, 0x212,	0xA, 0x23E,	0xB, 0x3AC,	0xB, 0x393,
			0xB, 0x3E1,	0xA, 0x1DE,	0xB, 0x3D6,	0xA, 0x31D,	0xB, 0x3E5,	0xB, 0x3E4,	0xA, 0x207,	0xB, 0x3C7,
			0xA, 0x277,	0xB, 0x3D4,	0x8, 0x0C0,	0xA, 0x162,	0xA, 0x3DA,	0xA, 0x124,	0xA, 0x1B4,	0xA, 0x264,
			0xA, 0x33D,	0xA, 0x1D1,	0xA, 0x1AF,	0xA, 0x39E,	0xA, 0x24F,	0xB, 0x373,	0xA, 0x249,	0xB, 0x372,
			0x9, 0x167,	0xA, 0x210,	0xA, 0x23A,	0xA, 0x1B8,	0xB, 0x3AF,	0xA, 0x18E,	0xA, 0x2EC,	0x7, 0x062,
			0x4, 0x00D
		};

		// UO packets may not exceed 64kb in length
		private const int BufferSize = 0x10000;

		// Optimal compression ratio is 2 / 8.
		private const int MinimalCodeLength = 2;

		// If our input exceeds this length, we cannot possibly compress it in 
		private const int EarlyOverflow = ( BufferSize * 8 ) / MinimalCodeLength;

		private static byte[] m_OutputBuffer = new byte[BufferSize];
		private static object m_SyncRoot = new object();

		public unsafe static void Compress( byte[] input, int length, out byte[] output, out int outputLength ) {
			if ( length >= EarlyOverflow ) {
				output = null;
				outputLength = length;
				return;
			}

			lock ( m_SyncRoot ) {
				int holdCount = 0;
				int holdValue = 0;

				int packCount = 0;
				int packValue = 0;

				int byteValue = 0;

				int inputLength = length;
				int inputIndex = 0;

				int outputCount = 0;

				fixed ( int* pTable = m_Table ) {
					fixed ( byte* pOutputBuffer = m_OutputBuffer ) {
						while ( inputIndex < inputLength ) {
							byteValue = input[inputIndex++] << 1;

							packCount = pTable[byteValue];
							packValue = pTable[byteValue | 1];

							holdValue <<= packCount;
							holdValue |= packValue;
							holdCount += packCount;

							if ( ( outputCount + ( holdCount / 8 ) ) >= BufferSize ) {
								output = null;
								outputLength = length;
								return;
							}

							while ( holdCount >= 8 ) {
								holdCount -= 8;

								pOutputBuffer[outputCount++] = ( byte ) ( holdValue >> holdCount );
							}
						}

						packCount = pTable[0x200];
						packValue = pTable[0x201];

						holdValue <<= packCount;
						holdValue |= packValue;
						holdCount += packCount;

						if ( ( outputCount + ( ( holdCount + 7 ) / 8 ) ) >= BufferSize ) {
							output = null;
							outputLength = length;
							return;
						}

						while ( holdCount >= 8 ) {
							holdCount -= 8;

							pOutputBuffer[outputCount++] = ( byte ) ( holdValue >> holdCount );
						}

						if ( holdCount > 0 ) {
							pOutputBuffer[outputCount++] = ( byte ) ( holdValue << ( 8 - holdCount ) );
						}
					}
				}

				output = m_OutputBuffer;
				outputLength = outputCount;
			}
		}

		public static readonly ICompressor Compressor;

		static Compression() {
			if ( Core.Is64Bit )
				Compressor = new Compressor64();
			else
				Compressor = new Compressor32();
		}

		public static ZLibError Pack( byte[] dest, ref int destLength, byte[] source, int sourceLength ) {
			return Compressor.Compress( dest, ref destLength, source, sourceLength );
		}

		public static ZLibError Pack( byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality ) {
			return Compressor.Compress( dest, ref destLength, source, sourceLength, quality );
		}

		public static ZLibError Unpack( byte[] dest, ref int destLength, byte[] source, int sourceLength ) {
			return Compressor.Decompress( dest, ref destLength, source, sourceLength );
		}
	}

	public interface ICompressor {
		string Version {
			get;
		}

		ZLibError Compress( byte[] dest, ref int destLength, byte[] source, int sourceLength );
		ZLibError Compress( byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality );

		ZLibError Decompress( byte[] dest, ref int destLength, byte[] source, int sourceLength );
	}

	public sealed class Compressor32 : ICompressor {
		[DllImport( "zlib32" )]
		private static extern string zlibVersion();

		[DllImport( "zlib32" )]
		private static extern ZLibError compress( byte[] dest, ref int destLength, byte[] source, int sourceLength );

		[DllImport( "zlib32" )]
		private static extern ZLibError compress2( byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality );

		[DllImport( "zlib32" )]
		private static extern ZLibError uncompress( byte[] dest, ref int destLen, byte[] source, int sourceLen );

		public Compressor32() {
		}

		public string Version {
			get {
				return zlibVersion();
			}
		}

		public ZLibError Compress( byte[] dest, ref int destLength, byte[] source, int sourceLength ) {
			return compress( dest, ref destLength, source, sourceLength );
		}

		public ZLibError Compress( byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality ) {
			return compress2( dest, ref destLength, source, sourceLength, quality );
		}

		public ZLibError Decompress( byte[] dest, ref int destLength, byte[] source, int sourceLength ) {
			return uncompress( dest, ref destLength, source, sourceLength );
		}
	}

	public sealed class Compressor64 : ICompressor {
		[DllImport( "zlib64" )]
		private static extern string zlibVersion();

		[DllImport( "zlib64" )]
		private static extern ZLibError compress( byte[] dest, ref int destLength, byte[] source, int sourceLength );

		[DllImport( "zlib64" )]
		private static extern ZLibError compress2( byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality );

		[DllImport( "zlib64" )]
		private static extern ZLibError uncompress( byte[] dest, ref int destLen, byte[] source, int sourceLen );

		public Compressor64() {
		}

		public string Version {
			get {
				return zlibVersion();
			}
		}

		public ZLibError Compress( byte[] dest, ref int destLength, byte[] source, int sourceLength ) {
			return compress( dest, ref destLength, source, sourceLength );
		}

		public ZLibError Compress( byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality ) {
			return compress2( dest, ref destLength, source, sourceLength, quality );
		}

		public ZLibError Decompress( byte[] dest, ref int destLength, byte[] source, int sourceLength ) {
			return uncompress( dest, ref destLength, source, sourceLength );
		}
	}

	public enum ZLibError : int {
		VersionError = -6,
		BufferError = -5,
		MemoryError = -4,
		DataError = -3,
		StreamError = -2,
		FileError = -1,

		Okay = 0,

		StreamEnd = 1,
		NeedDictionary = 2
	}

	public enum ZLibQuality : int {
		Default = -1,

		None = 0,

		Speed = 1,
		Size = 9
	}
}