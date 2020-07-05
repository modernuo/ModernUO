using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Network;

namespace Benchmarks
{
  [RyuJitX64Job]
  [SimpleJob(RuntimeMoniker.NetCoreApp31)]
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
        new BuyItemState("Name A", 0x1009, 0x2009, 1009, 1, 10, 1024)
      };
    }

    [Benchmark]
    public int TestPacketConstruction()
    {
      Span<byte> packet = new VendorBuyContent(m_States).Compile(false, out var length);
      return length;
    }

    [Benchmark]
    public int TestInlineConstruction()
    {
      Serial serial1 = 0x1;
      Serial serial2 = 0x1000;
      var buyStates = m_States;

      Span<byte> data = stackalloc byte[5 + buyStates.Count * 19];

      int pos = 0;

      ((byte)0x3C).CopyTo(ref pos, data); // Packet ID
      ((ushort)data.Length).CopyTo(ref pos, data); // Length
      ((ushort)buyStates.Count).CopyTo(ref pos, data); // Count

      for (int i = buyStates.Count - 1; i >= 0; i--)
      {
        BuyItemState buyState = buyStates[i];

        buyState.MySerial.CopyTo(ref pos, data);
        ((ushort)buyState.ItemID).CopyTo(ref pos, data);
        ((byte)0).CopyTo(ref pos, data); // ItemID Offset
        ((ushort)buyState.Amount).CopyTo(ref pos, data);
        ((ushort)(i + 1)).CopyTo(ref pos, data); // X
        ((ushort)0x1).CopyTo(ref pos, data); // Y
        buyState.ContainerSerial.CopyTo(ref pos, data);
        ((ushort)buyState.Hue).CopyTo(ref pos, data);
      }

      return data.Length;
    }

    [Benchmark]
    public int TestSpanWriterConstruction()
    {
      Serial serial1 = 0x1;
      Serial serial2 = 0x1000;
      var buyStates = m_States;

      int length = 5 + buyStates.Count * 19;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);

      w.Write((byte)0x3C); // Packet ID
      w.Write((ushort)length); // Length
      w.Write((ushort)buyStates.Count); // Length

      for (int i = buyStates.Count - 1; i >= 0; i--)
      {
        BuyItemState buyState = buyStates[i];
        w.Write(buyState.MySerial);
        w.Write((ushort)buyState.ItemID);
        w.Write((byte)0);
        w.Write((ushort)(i + 1)); // X
        w.Write((ushort)0x1); // Y
        w.Write(buyState.ContainerSerial);
        w.Write((ushort)buyState.Hue);
      }

      return w.Pos;
    }
  }
}
