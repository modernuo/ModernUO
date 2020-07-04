using System;
using System.Buffers.Binary;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;

namespace Benchmarks
{
  [RyuJitX64Job]
  [SimpleJob(RuntimeMoniker.NetCoreApp31)]
  public class BenchmarkPacketConstruction
  {
    [Benchmark]
    public int TestSliceConstruction()
    {
      Serial serial1 = 0x1;
      Serial serial2 = 0x1000;

      Span<byte> data = stackalloc byte[]
      {
        0x01, // Packet ID
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00
      };

      BinaryPrimitives.WriteUInt32BigEndian(data.Slice(1, 4), serial1);
      BinaryPrimitives.WriteUInt32BigEndian(data.Slice(5, 4), serial2);

      return data.Length;
    }

    [Benchmark]
    public int TestNoInlineConstruction()
    {
      Serial serial1 = 0x1;
      Serial serial2 = 0x1000;

      Span<byte> data = stackalloc byte[25];

      data[0] = 0x01; // Packet ID
      BinaryPrimitives.WriteUInt32BigEndian(data.Slice(1, 4), serial1);
      BinaryPrimitives.WriteUInt32BigEndian(data.Slice(5, 4), serial2);
      BinaryPrimitives.WriteUInt64BigEndian(data.Slice(9, 8), 0x0);
      BinaryPrimitives.WriteUInt64BigEndian(data.Slice(17, 8), 0x0);

      return data.Length;
    }
  }
}
