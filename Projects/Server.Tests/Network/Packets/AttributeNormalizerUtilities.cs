using System;
using Server.Network;

namespace Server.Tests.Network.Packets
{
  public static class AttributeNormalizerUtilities
  {
    public static void Write(int cur, int max, bool normalize, Span<byte> data)
    {
      if (normalize && AttributeNormalizer.Enabled && max != 0)
      {
        int maximum = AttributeNormalizer.Maximum;

        ((ushort)maximum).CopyTo(data.Slice(0, 2));
        ((ushort)(cur * maximum / max)).CopyTo(data.Slice(2, 4));
        return;
      }

      ((ushort)max).CopyTo(data.Slice(0, 2));
      ((ushort)cur).CopyTo(data.Slice(2, 4));
    }

    public static void WriteReverse(int cur, int max, bool normalize, Span<byte> data)
    {
      if (normalize && AttributeNormalizer.Enabled && max != 0)
      {
        int maximum = AttributeNormalizer.Maximum;

        ((ushort)(cur * maximum / max)).CopyTo(data.Slice(0, 2));
        ((ushort)maximum).CopyTo(data.Slice(2, 4));
        return;
      }

      ((ushort)cur).CopyTo(data.Slice(0, 2));
      ((ushort)max).CopyTo(data.Slice(2, 4));
    }
  }
}
