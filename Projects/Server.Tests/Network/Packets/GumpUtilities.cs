using System;
using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace Server.Tests.Network.Packets
{
    public static class GumpUtilities
    {
        public static readonly byte[] NoMoveBuffer = Encoding.ASCII.GetBytes("{ nomove }");
        public static readonly byte[] NoCloseBuffer = Encoding.ASCII.GetBytes("{ noclose }");
        public static readonly byte[] NoDisposeBuffer = Encoding.ASCII.GetBytes("{ nodispose }");
        public static readonly byte[] NoResizeBuffer = Encoding.ASCII.GetBytes("{ noresize }");

        public const string NoMove = "{ nomove }";
        public const string NoClose = "{ noclose }";
        public const string NoDispose = "{ nodispose }";
        public const string NoResize = "{ noresize }";

        public static void WritePacked(this Span<byte> dest, ref int pos, ReadOnlySpan<byte> source)
        {
            int length = source.Length;

            if (length == 0)
            {
#if NO_LOCAL_INIT
        dest.Write(ref pos, 0);
#else
                pos += 4;
#endif
                return;
            }

            ulong packLength = (ulong)dest.Length - 8;

            ZlibError ce = Zlib.Pack(dest.Slice(pos + 8), ref packLength, source, ZlibQuality.Default);
            if (ce != ZlibError.Okay) Console.WriteLine("ZLib error: {0} (#{1})", ce, (int)ce);

            dest.Write(ref pos, (int)(4 + packLength));
            dest.Write(ref pos, length);
            pos += (int)packLength;
        }
    }
}
