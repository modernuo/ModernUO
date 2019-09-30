using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Server.Buffers;
using Server.Multis;

namespace Server.Network
{
  public static class HouseFoundationPackets
  {
    public static void SendBeginHouseCustomization(NetState ns, Serial house)
    {
      if (ns == null)
        return;

      SpanWriter writer = new SpanWriter(stackalloc byte[17]);
      writer.Write((byte)0xBF); // Packet ID
      writer.Write((ushort)17); // Dynamic Length

      writer.Write((short)0x20); // House Customization
      writer.Write(house);
      writer.Write((byte)0x04); // Start customization
      writer.Position += 2;
      writer.Write(0xFFFFFFFF);
      writer.Write((byte)0xFF);

      ns.Send(writer.Span);
    }

    public static void SendEndHouseCustomization(NetState ns, Serial house)
    {
      if (ns == null)
        return;

      SpanWriter writer = new SpanWriter(stackalloc byte[17]);
      writer.Write((byte)0xBF); // Packet ID
      writer.Write((ushort)17); // Dynamic Length

      writer.Write((short)0x20); // House Customization
      writer.Write(house);
      writer.Write((byte)0x05); // End customization
      writer.Position += 2;
      writer.Write(0xFFFFFFFF);
      writer.Write((byte)0xFF);

      ns.Send(writer.Span);
    }

    public static void SendDesignStateGeneral(NetState ns, Serial house, int revision)
    {
      if (ns == null)
        return;

      SpanWriter writer = new SpanWriter(stackalloc byte[13]);
      writer.Write((byte)0xBF); // Packet ID
      writer.Write((ushort)13); // Dynamic Length

      writer.Write((short)0x1D); // Design
      writer.Write(house);
      writer.Write(revision);

      ns.Send(writer.Span);
    }

    private const int MaxItemsPerStairBuffer = 750;

    private static ConcurrentQueue<SendQueueEntry> m_SendQueue;
    private static AutoResetEvent m_Sync;

    static HouseFoundationPackets()
    {
      m_SendQueue = new ConcurrentQueue<SendQueueEntry>();
      m_Sync = new AutoResetEvent(false);
      Task.Run(ProcessDesignStates);
    }

    public static void ProcessDesignStates()
    {
      while (!Core.Closing)
      {
        m_Sync.WaitOne();

        int count = m_SendQueue.Count;

        while (count > 0 && m_SendQueue.TryDequeue(out SendQueueEntry sqe))
          try
          {
            byte[] packet = sqe.m_DesignState.Packet;

            if (packet == null)
            {
              packet = CreateDesignDetailsPacket(sqe.m_Serial, sqe.m_Revision, sqe.m_xMin, sqe.m_yMin, sqe.m_xMax,
                sqe.m_yMax, sqe.m_Tiles);
              sqe.m_DesignState.Packet = packet;
              return;
            }

            sqe.m_NetState.Send(packet);
          }
          catch (Exception e)
          {
            Console.WriteLine(e);

            try
            {
              using StreamWriter op = new StreamWriter("dsd_exceptions.txt", true);
              op.WriteLine(e);
            }
            catch
            {
              // ignored
            }
          }
          finally
          {
            count = m_SendQueue.Count;
          }
      }
    }

    public static byte[] CreateDesignDetailsPacket(Serial serial, int revision, int xMin, int yMin, int xMax, int yMax,
        MultiTileEntry[] tiles)
    {
      ArrayBufferWriter<byte> bufferWriter = new ArrayBufferWriter<byte>(0x10000);

      SpanWriter headWriter = new SpanWriter(stackalloc byte[18]);
      headWriter.Write((byte)0xD8); // Packet ID
      headWriter.Position += 2; // Dynamic Length

      headWriter.Write((byte)0x03); // Compression Type
      headWriter.Position++;
      headWriter.Write(serial);
      headWriter.Write(revision);
      headWriter.Write((short)tiles.Length);
      headWriter.Position += 3; // Buffer/Plane count

      int bufferLength = 1; // includes plane count

      int width = xMax - xMin + 1;
      int height = yMax - yMin + 1;

      Span<bool> planeUsed = stackalloc bool[9];
      Span<byte> planeBuffers = stackalloc byte[9216];
      Span<byte> stairBuffers = stackalloc byte[MaxItemsPerStairBuffer * 30];

      int totalStairsUsed = 0;

      for (int i = 0; i < tiles.Length; ++i)
      {
        MultiTileEntry mte = tiles[i];
        int x = mte.m_OffsetX - xMin;
        int y = mte.m_OffsetY - yMin;
        int z = mte.m_OffsetZ;
        bool floor = TileData.ItemTable[mte.m_ItemID & TileData.MaxItemValue].Height <= 0;
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
            int stairBufferIndex = totalStairsUsed / MaxItemsPerStairBuffer;
            Span<byte> stairBuffer = stairBuffers.Slice(stairBufferIndex, MaxItemsPerStairBuffer);

            int byteIndex = totalStairsUsed % MaxItemsPerStairBuffer * 5;

            stairBuffer[byteIndex++] = (byte)(mte.m_ItemID >> 8);
            stairBuffer[byteIndex++] = (byte)mte.m_ItemID;

            stairBuffer[byteIndex++] = (byte)mte.m_OffsetX;
            stairBuffer[byteIndex++] = (byte)mte.m_OffsetY;
            stairBuffer[byteIndex] = (byte)mte.m_OffsetZ;

            ++totalStairsUsed;

            continue;
          }
        }

        if (plane == 0)
          size = height;
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

        int index = (x * size + y) * 2;

        if (x < 0 || y < 0 || y >= size || index + 1 >= 0x400)
        {
          int stairBufferIndex = totalStairsUsed / MaxItemsPerStairBuffer;
          Span<byte> stairBuffer = stairBuffers.Slice(stairBufferIndex, MaxItemsPerStairBuffer);

          int byteIndex = totalStairsUsed % MaxItemsPerStairBuffer * 5;

          stairBuffer[byteIndex++] = (byte)(mte.m_ItemID >> 8);
          stairBuffer[byteIndex++] = (byte)mte.m_ItemID;

          stairBuffer[byteIndex++] = (byte)mte.m_OffsetX;
          stairBuffer[byteIndex++] = (byte)mte.m_OffsetY;
          stairBuffer[byteIndex] = (byte)mte.m_OffsetZ;

          ++totalStairsUsed;
        }
        else
        {
          planeUsed[plane] = true;
          planeBuffers[plane * 0x400 + index] = (byte)(mte.m_ItemID >> 8);
          planeBuffers[plane * 0x400 + index + 1] = (byte)mte.m_ItemID;
        }
      }

      int planeCount = 0;
      int bound = (int)Compression.Compressor.CompressBound(0x400);

      for (int i = 0; i < 9; ++i)
      {
        if (!planeUsed[i])
          continue;

        ++planeCount;

        int size;

        if (i == 0)
          size = width * height * 2;
        else if (i < 5)
          size = (width - 1) * (height - 2) * 2;
        else
          size = width * (height - 1) * 2;

        Span<byte> inflatedBuffer = planeBuffers.Slice(i * 0x400, 0x400);
        Span<byte> deflatedBuffer = bufferWriter.GetSpan(bound + 4);

        ulong deflatedLength = (ulong)bound;
        ZLibError ce = Compression.Pack(deflatedBuffer.Slice(4), ref deflatedLength, inflatedBuffer, (ulong)size, ZLibQuality.Default);

        if (ce != ZLibError.Okay)
        {
          Console.WriteLine("ZLib error: {0} (#{1})", ce, (int)ce);
          size = 0;
        }

        int length = (int)deflatedLength;

        deflatedBuffer[0] = (byte)(0x20 | i);
        deflatedBuffer[1] = (byte)size;
        deflatedBuffer[2] = (byte)length;
        deflatedBuffer[3] = (byte)(((size >> 4) & 0xF0) | ((length >> 8) & 0xF));

        bufferLength += 4 + length;
        bufferWriter.Advance(length + 4);
      }

      int totalStairBuffersUsed = (totalStairsUsed + (MaxItemsPerStairBuffer - 1)) / MaxItemsPerStairBuffer;
      bound = (int)Compression.Compressor.CompressBound(MaxItemsPerStairBuffer);

      for (int i = 0; i < totalStairBuffersUsed; ++i)
      {
        ++planeCount;

        int count = totalStairsUsed - i * MaxItemsPerStairBuffer;

        if (count > MaxItemsPerStairBuffer)
          count = MaxItemsPerStairBuffer;

        int size = count * 5;

        Span<byte> inflatedBuffer = stairBuffers.Slice(i * MaxItemsPerStairBuffer, MaxItemsPerStairBuffer);
        Span<byte> deflatedBuffer = bufferWriter.GetSpan(bound + 4);

        ulong deflatedLength = (ulong)bound;
        ZLibError ce = Compression.Pack(deflatedBuffer.Slice(4), ref deflatedLength, inflatedBuffer, (ulong)size, ZLibQuality.Default);

        if (ce != ZLibError.Okay)
        {
          Console.WriteLine("ZLib error: {0} (#{1})", ce, (int)ce);
          deflatedLength = 0;
          size = 0;
        }

        int length = (int)deflatedLength;

        deflatedBuffer[0] = (byte)(9 + i);
        deflatedBuffer[1] = (byte)size;
        deflatedBuffer[2] = (byte)length;
        deflatedBuffer[3] = (byte)(((size >> 4) & 0xF0) | ((length >> 8) & 0xF));

        bufferLength += 4 + length;
        bufferWriter.Advance(length + 4);
      }

      headWriter.Position = 15;

      headWriter.Write((short)bufferLength); // Buffer length
      headWriter.Write((byte)planeCount); // Plane count

      headWriter.Position = 1;
      int written = headWriter.WrittenCount + bufferWriter.WrittenCount;
      headWriter.Write((ushort)written);

      byte[] packet = new byte[written];
      headWriter.CopyTo(packet);
      bufferWriter.WrittenSpan.CopyTo(packet.AsSpan(headWriter.WrittenCount));

      return packet;
    }

    private class SendQueueEntry
    {
      public NetState m_NetState;
      public DesignState m_DesignState;
      public int m_Revision;
      public uint m_Serial;
      public MultiTileEntry[] m_Tiles;
      public int m_xMin, m_yMin, m_xMax, m_yMax;

      public SendQueueEntry(NetState ns, HouseFoundation foundation, DesignState state)
      {
        m_NetState = ns;
        m_Serial = foundation.Serial;
        m_Revision = state.Revision;
        m_DesignState = state;

        MultiComponentList mcl = state.Components;

        m_xMin = mcl.Min.X;
        m_yMin = mcl.Min.Y;
        m_xMax = mcl.Max.X;
        m_yMax = mcl.Max.Y;

        m_Tiles = mcl.List;
      }
    }

    public static void SendDesignDetails(NetState ns, HouseFoundation house, DesignState state)
    {
      m_SendQueue.Enqueue(new SendQueueEntry(ns, house, state));
      m_Sync.Set();
    }
  }
}
