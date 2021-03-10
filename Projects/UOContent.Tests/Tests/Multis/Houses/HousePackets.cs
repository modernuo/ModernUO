using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using Server.Multis;

namespace Server.Network
{
    public class BeginHouseCustomization : Packet
    {
        public BeginHouseCustomization(Serial house) : base(0xBF)
        {
            EnsureCapacity(17);

            Stream.Write((short)0x20);
            Stream.Write(house);
            Stream.Write((byte)0x04);
            Stream.Write((ushort)0x0000);
            Stream.Write((ushort)0xFFFF);
            Stream.Write((ushort)0xFFFF);
            Stream.Write((byte)0xFF);
        }
    }

    public class EndHouseCustomization : Packet
    {
        public EndHouseCustomization(Serial house) : base(0xBF)
        {
            EnsureCapacity(17);

            Stream.Write((short)0x20);
            Stream.Write(house);
            Stream.Write((byte)0x05);
            Stream.Write((ushort)0x0000);
            Stream.Write((ushort)0xFFFF);
            Stream.Write((ushort)0xFFFF);
            Stream.Write((byte)0xFF);
        }
    }

    public sealed class DesignStateGeneral : Packet
    {
        public DesignStateGeneral(Serial house, int revision) : base(0xBF)
        {
            EnsureCapacity(13);

            Stream.Write((short)0x1D);
            Stream.Write(house);
            Stream.Write(revision);
        }
    }

    public sealed class DesignStateDetailed : Packet
    {
        public const int MaxItemsPerStairBuffer = 750;

        private readonly bool[] m_PlaneUsed = new bool[9];
        private readonly byte[] m_PrimBuffer = new byte[4];

        public DesignStateDetailed(uint serial, int revision, int xMin, int yMin, int xMax, int yMax, MultiTileEntry[] tiles)
            : base(0xD8)
        {
            EnsureCapacity(17 + tiles.Length * 5);

            Write((byte)0x03); // Compression Type
            Write((byte)0x00); // Unknown
            Write(serial);
            Write(revision);
            Write((short)tiles.Length);
            Write((short)0); // Buffer length : reserved
            Write((byte)0);  // Plane count : reserved

            var totalLength = 1; // includes plane count

            var width = xMax - xMin + 1;
            var height = yMax - yMin + 1;

            var planeBuffers = new byte[9][];

            for (var i = 0; i < planeBuffers.Length; ++i)
            {
                planeBuffers[i] = ArrayPool<byte>.Shared.Rent(0x400);
            }

            var stairBuffers = new byte[6][];

            for (var i = 0; i < stairBuffers.Length; ++i)
            {
                stairBuffers[i] = ArrayPool<byte>.Shared.Rent(MaxItemsPerStairBuffer * 5);
            }

            Clear(planeBuffers[0], width * height * 2);

            for (var i = 0; i < 4; ++i)
            {
                Clear(planeBuffers[1 + i], (width - 1) * (height - 2) * 2);
                Clear(planeBuffers[5 + i], width * (height - 1) * 2);
            }

            var totalStairsUsed = 0;

            for (var i = 0; i < tiles.Length; ++i)
            {
                var mte = tiles[i];
                var x = mte.OffsetX - xMin;
                var y = mte.OffsetY - yMin;
                int z = mte.OffsetZ;
                var floor = TileData.ItemTable[mte.ItemId & TileData.MaxItemValue].Height <= 0;
                int plane, size;

                switch (z)
                {
                    case 0:
                        plane = 0;
                        break;
                    case 7:
                        plane = 1;
                        break;
                    case 27:
                        plane = 2;
                        break;
                    case 47:
                        plane = 3;
                        break;
                    case 67:
                        plane = 4;
                        break;
                    default:
                        {
                            var stairBufferIndex = totalStairsUsed / MaxItemsPerStairBuffer;
                            var stairBuffer = stairBuffers[stairBufferIndex];

                            var byteIndex = totalStairsUsed % MaxItemsPerStairBuffer * 5;

                            stairBuffer[byteIndex++] = (byte)(mte.ItemId >> 8);
                            stairBuffer[byteIndex++] = (byte)mte.ItemId;

                            stairBuffer[byteIndex++] = (byte)mte.OffsetX;
                            stairBuffer[byteIndex++] = (byte)mte.OffsetY;
                            stairBuffer[byteIndex] = (byte)mte.OffsetZ;

                            ++totalStairsUsed;

                            continue;
                        }
                }

                if (plane == 0)
                {
                    size = height;
                }
                else if (floor)
                {
                    size = height - 2;
                    x -= 1;
                    y -= 1;
                }
                else
                {
                    size = height - 1;
                    plane += 4;
                }

                var index = (x * size + y) * 2;

                if (x < 0 || y < 0 || y >= size || index + 1 >= 0x400)
                {
                    var stairBufferIndex = totalStairsUsed / MaxItemsPerStairBuffer;
                    var stairBuffer = stairBuffers[stairBufferIndex];

                    var byteIndex = totalStairsUsed % MaxItemsPerStairBuffer * 5;

                    stairBuffer[byteIndex++] = (byte)(mte.ItemId >> 8);
                    stairBuffer[byteIndex++] = (byte)mte.ItemId;

                    stairBuffer[byteIndex++] = (byte)mte.OffsetX;
                    stairBuffer[byteIndex++] = (byte)mte.OffsetY;
                    stairBuffer[byteIndex] = (byte)mte.OffsetZ;

                    ++totalStairsUsed;
                }
                else
                {
                    m_PlaneUsed[plane] = true;
                    planeBuffers[plane][index] = (byte)(mte.ItemId >> 8);
                    planeBuffers[plane][index + 1] = (byte)mte.ItemId;
                }
            }

            var planeCount = 0;

            var deflatedBuffer = ArrayPool<byte>.Shared.Rent(0x2000);

            for (var i = 0; i < planeBuffers.Length; ++i)
            {
                if (!m_PlaneUsed[i])
                {
                    ArrayPool<byte>.Shared.Return(planeBuffers[i]);
                    continue;
                }

                ++planeCount;

                int size = i switch
                {
                    0   => width * height * 2,
                    < 5 => (width - 1) * (height - 2) * 2,
                    _   => width * (height - 1) * 2
                };

                var inflatedBuffer = planeBuffers[i];

                var deflatedLength = deflatedBuffer.Length;
                var ce = Zlib.Pack(
                    deflatedBuffer,
                    ref deflatedLength,
                    inflatedBuffer,
                    size,
                    ZlibQuality.Default
                );

                if (ce != ZlibError.Okay)
                {
                    Console.WriteLine("ZLib error: {0} (#{1})", ce, (int)ce);
                    deflatedLength = 0;
                    size = 0;
                }

                Write((byte)(0x20 | i));
                Write((byte)size);
                Write((byte)deflatedLength);
                Write((byte)(((size >> 4) & 0xF0) | ((deflatedLength >> 8) & 0xF)));
                Write(deflatedBuffer, 0, deflatedLength);

                totalLength += 4 + deflatedLength;
                ArrayPool<byte>.Shared.Return(inflatedBuffer);
            }

            var totalStairBuffersUsed = (totalStairsUsed + (MaxItemsPerStairBuffer - 1)) / MaxItemsPerStairBuffer;

            for (var i = 0; i < totalStairBuffersUsed; ++i)
            {
                ++planeCount;

                var count = Math.Min(MaxItemsPerStairBuffer, totalStairsUsed - i * MaxItemsPerStairBuffer);

                var size = count * 5;

                var inflatedBuffer = stairBuffers[i];

                var deflatedLength = deflatedBuffer.Length;
                var ce = Zlib.Pack(
                    deflatedBuffer,
                    ref deflatedLength,
                    inflatedBuffer,
                    size,
                    ZlibQuality.Default
                );

                if (ce != ZlibError.Okay)
                {
                    Console.WriteLine("ZLib error: {0} (#{1})", ce, (int)ce);
                    deflatedLength = 0;
                    size = 0;
                }

                Write((byte)(9 + i));
                Write((byte)size);
                Write((byte)deflatedLength);
                Write((byte)(((size >> 4) & 0xF0) | ((deflatedLength >> 8) & 0xF)));
                Write(deflatedBuffer, 0, deflatedLength);

                totalLength += 4 + deflatedLength;
            }

            for (var i = 0; i < stairBuffers.Length; ++i)
            {
                ArrayPool<byte>.Shared.Return(stairBuffers[i]);
            }

            ArrayPool<byte>.Shared.Return(deflatedBuffer);

            Stream.Seek(15, SeekOrigin.Begin);

            Write((short)totalLength); // Buffer length
            Write((byte)planeCount);   // Plane count
        }

        public void Write(int value)
        {
            m_PrimBuffer[0] = (byte)(value >> 24);
            m_PrimBuffer[1] = (byte)(value >> 16);
            m_PrimBuffer[2] = (byte)(value >> 8);
            m_PrimBuffer[3] = (byte)value;

            Stream.UnderlyingStream.Write(m_PrimBuffer, 0, 4);
        }

        public void Write(uint value)
        {
            m_PrimBuffer[0] = (byte)(value >> 24);
            m_PrimBuffer[1] = (byte)(value >> 16);
            m_PrimBuffer[2] = (byte)(value >> 8);
            m_PrimBuffer[3] = (byte)value;

            Stream.UnderlyingStream.Write(m_PrimBuffer, 0, 4);
        }

        public void Write(short value)
        {
            m_PrimBuffer[0] = (byte)(value >> 8);
            m_PrimBuffer[1] = (byte)value;

            Stream.UnderlyingStream.Write(m_PrimBuffer, 0, 2);
        }

        public void Write(byte value)
        {
            Stream.UnderlyingStream.WriteByte(value);
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            Stream.UnderlyingStream.Write(buffer, offset, size);
        }

        public static void Clear(byte[] buffer, int size)
        {
            for (var i = 0; i < size; ++i)
            {
                buffer[i] = 0;
            }
        }
    }
}
