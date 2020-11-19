using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Network;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class BenchmarkPacketConstruction
    {
        public List<BuyItemState> m_States;

        [GlobalSetup]
        public void SetUp()
        {
            m_States = new List<BuyItemState>
            {
                new BuyItemState("Name 1", 0x1000, 0x2000, 1000, 1, 10, 1024),
                new BuyItemState("Name 2", 0x1001, 0x2001, 1001, 1, 10, 1024),
                new BuyItemState("Name 3", 0x1002, 0x2002, 1002, 1, 10, 1024),
                new BuyItemState("Name 4", 0x1003, 0x2003, 1003, 1, 10, 1024),
                new BuyItemState("Name 5", 0x1004, 0x2004, 1004, 1, 10, 1024),
                new BuyItemState("Name 6", 0x1005, 0x2005, 1005, 1, 10, 1024),
                new BuyItemState("Name 7", 0x1006, 0x2006, 1006, 1, 10, 1024),
                new BuyItemState("Name 8", 0x1007, 0x2007, 1007, 1, 10, 1024),
                new BuyItemState("Name 9", 0x1008, 0x2008, 1008, 1, 10, 1024),
                new BuyItemState("Name A", 0x1009, 0x2009, 1009, 1, 10, 1024),
                new BuyItemState("Name 10", 0x1000, 0x2000, 1000, 1, 10, 1024),
                new BuyItemState("Name 20", 0x1001, 0x2001, 1001, 1, 10, 1024),
                new BuyItemState("Name 30", 0x1002, 0x2002, 1002, 1, 10, 1024),
                new BuyItemState("Name 40", 0x1003, 0x2003, 1003, 1, 10, 1024),
                new BuyItemState("Name 50", 0x1004, 0x2004, 1004, 1, 10, 1024),
                new BuyItemState("Name 60", 0x1005, 0x2005, 1005, 1, 10, 1024),
                new BuyItemState("Name 70", 0x1006, 0x2006, 1006, 1, 10, 1024),
                new BuyItemState("Name 80", 0x1007, 0x2007, 1007, 1, 10, 1024),
                new BuyItemState("Name 90", 0x1008, 0x2008, 1008, 1, 10, 1024),
                new BuyItemState("Name A0", 0x1009, 0x2009, 1009, 1, 10, 1024),
                new BuyItemState("Name 11", 0x1000, 0x2000, 1000, 1, 10, 1024),
                new BuyItemState("Name 21", 0x1001, 0x2001, 1001, 1, 10, 1024),
                new BuyItemState("Name 31", 0x1002, 0x2002, 1002, 1, 10, 1024),
                new BuyItemState("Name 41", 0x1003, 0x2003, 1003, 1, 10, 1024),
                new BuyItemState("Name 51", 0x1004, 0x2004, 1004, 1, 10, 1024),
                new BuyItemState("Name 61", 0x1005, 0x2005, 1005, 1, 10, 1024),
                new BuyItemState("Name 71", 0x1006, 0x2006, 1006, 1, 10, 1024),
                new BuyItemState("Name 81", 0x1007, 0x2007, 1007, 1, 10, 1024),
                new BuyItemState("Name 91", 0x1008, 0x2008, 1008, 1, 10, 1024),
                new BuyItemState("Name A1", 0x1009, 0x2009, 1009, 1, 10, 1024)
            };
        }

        [Benchmark]
        public Packet TestPacketConstruction()
        {
            Packet p = new VendorBuyContent(m_States);
            p.Compile(false, out _);
            return p;
        }

        [Benchmark]
        public int TestInlineRefConstruction()
        {
            var buyStates = m_States;

            int length = 5 + buyStates.Count * 19;

            Span<byte> data = stackalloc byte[length];

            int pos = 0;
            data.Write(ref pos, (byte)0x3C);
            data.Write(ref pos, (ushort)length);
            data.Write(ref pos, (ushort)buyStates.Count);

            for (int i = buyStates.Count - 1; i >= 0; i--)
            {
                BuyItemState buyState = buyStates[i];

                data.Write(ref pos, buyState.MySerial);
                data.Write(ref pos, (ushort)buyState.ItemID);
                data.Write(ref pos, (byte)0);
                data.Write(ref pos, (ushort)buyState.Amount);
                data.Write(ref pos, (ushort)(i + 1));
                data.Write(ref pos, (ushort)0x1);
                data.Write(ref pos, buyState.ContainerSerial);
                data.Write(ref pos, (ushort)buyState.Hue);
            }

            return data.Length;
        }

        [Benchmark]
        public int TestInlineConstruction()
        {
            var buyStates = m_States;

            Span<byte> data = stackalloc byte[5 + buyStates.Count * 19];
            data[0] = 0x3C;
            BinaryPrimitives.WriteUInt16BigEndian(data.Slice(1, 2), (ushort)data.Length);
            BinaryPrimitives.WriteUInt16BigEndian(data.Slice(3, 2), (ushort)buyStates.Count);
            int pos = 4;


            for (int i = buyStates.Count - 1; i >= 0; i--)
            {
                BuyItemState buyState = buyStates[i];
                BinaryPrimitives.WriteUInt32BigEndian(data.Slice(pos, 4), buyState.MySerial);
                pos += 4;
                BinaryPrimitives.WriteUInt16BigEndian(data.Slice(pos, 2), (ushort)buyState.ItemID);
                pos += 2;
                data[pos++] = 0;
                BinaryPrimitives.WriteUInt16BigEndian(data.Slice(pos, 2), (ushort)buyState.Amount);
                pos += 2;
                BinaryPrimitives.WriteUInt16BigEndian(data.Slice(pos, 2), (ushort)(i + 1));
                pos += 2;
                BinaryPrimitives.WriteUInt16BigEndian(data.Slice(pos, 2), 0x1);
                pos += 2;
                BinaryPrimitives.WriteUInt32BigEndian(data.Slice(pos, 4), buyState.ContainerSerial);
                pos += 4;
                BinaryPrimitives.WriteUInt16BigEndian(data.Slice(pos, 2), (ushort)(buyState.Hue));
                pos += 2;
            }

            return data.Length;
        }

        [Benchmark]
        public int TestSpanWriterConstruction()
        {
            var buyStates = m_States;

            int length = 5 + buyStates.Count * 19;

            SpanWriter w = new SpanWriter(stackalloc byte[length]);

            w.Write((byte)0x3C);              // Packet ID
            w.Write((ushort)length);          // Length
            w.Write((ushort)buyStates.Count); // Length

            for (int i = buyStates.Count - 1; i >= 0; i--)
            {
                BuyItemState buyState = buyStates[i];
                w.Write(buyState.MySerial);
                w.Write((ushort)buyState.ItemID);
                w.Write((byte)0);
                w.Write((ushort)(i + 1)); // X
                w.Write((ushort)0x1);     // Y
                w.Write(buyState.ContainerSerial);
                w.Write((ushort)buyState.Hue);
            }

            return w.Pos;
        }
    }
}
