using System;
using System.IO.Compression;
using System.Text;

namespace Server.Tests.Network.Packets
{
  public class GumpUtilities
  {
    public static readonly byte[] NoMoveBuffer = Encoding.ASCII.GetBytes("{ nomove }");
    public static readonly byte[] NoCloseBuffer = Encoding.ASCII.GetBytes("{ noclose }");
    public static readonly byte[] NoDisposeBuffer = Encoding.ASCII.GetBytes("{ nodispose }");
    public static readonly byte[] NoResizeBuffer = Encoding.ASCII.GetBytes("{ noresize }");

    public const string NoMove = "{ nomove }";
    public const string NoClose = "{ noclose }";
    public const string NoDispose = "{ nodispose }";
    public const string NoResize = "{ noresize }";

    public static int WritePacked(ReadOnlySpan<byte> source, Span<byte> dest)
    {
      int length = source.Length;

      if (length == 0)
      {
        dest.Slice(0, 4).Clear();
        return 4;
      }

      ulong packLength = (ulong)dest.Length - 8;

      ZlibError ce = Zlib.Pack(dest.Slice(8), ref packLength, source, ZlibQuality.Default);
      if (ce != ZlibError.Okay) Console.WriteLine("ZLib error: {0} (#{1})", ce, (int)ce);

      ((int)(4 + packLength)).CopyTo(dest.Slice(0, 4));
      length.CopyTo(dest.Slice(4, 4));

      return (int)(8 + packLength);
    }
  }
}
