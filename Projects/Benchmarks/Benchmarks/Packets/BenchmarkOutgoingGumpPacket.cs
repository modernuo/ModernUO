using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Collections;
using Server.Gumps;
using Server.Network;
using Server.Tests.Network;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class OutgoingGumpPacketBenchmarks
    {
        private static readonly byte[] _layoutBuffer = GC.AllocateUninitializedArray<byte>(0x20000);
        private static readonly byte[] _stringsBuffer = GC.AllocateUninitializedArray<byte>(0x20000);

        public static void CreateDisplayGump(Gump gump, out int switches, out int entries)
        {
            switches = 0;
            entries = 0;

            const bool packed = false;

            var layoutWriter = new SpanWriter(_layoutBuffer);

            if (!gump.Draggable)
            {
                layoutWriter.Write(Gump.NoMove);
            }

            if (!gump.Closable)
            {
                layoutWriter.Write(Gump.NoClose);
            }

            if (!gump.Disposable)
            {
                layoutWriter.Write(Gump.NoDispose);
            }

            if (!gump.Resizable)
            {
                layoutWriter.Write(Gump.NoResize);
            }

            var stringsList = new OrderedHashSet<string>(32);

            foreach (var entry in gump.Entries)
            {
                entry.AppendTo(ref layoutWriter, stringsList, ref entries, ref switches);
            }

            var stringsWriter = new SpanWriter(_stringsBuffer);

            foreach (var str in stringsList)
            {
                var s = str ?? "";
                stringsWriter.Write((ushort)s.Length);
                stringsWriter.WriteBigUni(s);
            }

            int maxLength;
            if (packed)
            {
                var worstLayoutLength = Zlib.MaxPackSize(layoutWriter.BytesWritten);
                var worstStringsLength = Zlib.MaxPackSize(stringsWriter.BytesWritten);
                maxLength = 40 + worstLayoutLength + worstStringsLength;
            }
            else
            {
                maxLength = 23 + layoutWriter.BytesWritten + stringsWriter.BytesWritten;
            }

            var writer = new SpanWriter(maxLength);
            writer.Write((byte)(packed ? 0xDD : 0xB0)); // Packet ID
            writer.Seek(2, SeekOrigin.Current);

            writer.Write(gump.Serial);
            writer.Write(gump.TypeID);
            writer.Write(gump.X);
            writer.Write(gump.Y);

            if (packed)
            {
                layoutWriter.Write((byte)0); // Layout text terminator
                OutgoingGumpPackets.WritePacked(layoutWriter.Span, ref writer);

                writer.Write(stringsList.Count);
                OutgoingGumpPackets.WritePacked(stringsWriter.Span, ref writer);
            }
            else
            {
                writer.Write((ushort)layoutWriter.BytesWritten);
                writer.Write(layoutWriter.Span);

                writer.Write((ushort)stringsList.Count);
                writer.Write(stringsWriter.Span);
            }

            writer.WritePacketLength();

            layoutWriter.Dispose();  // Just in case
            stringsWriter.Dispose(); // Just in case
        }

        public class NameChangeDeedGump : Gump
        {
            public NameChangeDeedGump() : base(50, 50)
            {
                Closable = false;
                Draggable = false;
                Resizable = false;

                AddPage(0);

                AddBlackAlpha(10, 120, 250, 85);
                AddHtml(10, 125, 250, 20, Color(Center("Name Change Deed"), 0xFFFFFF));

                AddLabel(73, 15, 1152, "");
                AddLabel(20, 150, 0x480, "New Name:");
                AddTextField(100, 150, 150, 20, 0);

                AddButtonLabeled(75, 180, 1, "Submit");
            }

            public void AddBlackAlpha(int x, int y, int width, int height)
            {
                AddImageTiled(x, y, width, height, 2624);
                AddAlphaRegion(x, y, width, height);
            }

            public void AddTextField(int x, int y, int width, int height, int index)
            {
                AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
                AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
            }

            public static string Center(string text) => $"<CENTER>{text}</CENTER>";

            public static string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

            public void AddButtonLabeled(int x, int y, int buttonID, string text)
            {
                AddButton(x, y - 1, 4005, 4007, buttonID);
                AddHtml(x + 35, y, 240, 20, Color(text, 0xFFFFFF));
            }
        }

        private static Gump _gump;

        [GlobalSetup]
        public void Setup()
        {
            _gump = new NameChangeDeedGump();
        }

        [Benchmark]
        public void TestNewStack()
        {
            CreateDisplayGump(_gump, out var _, out var _);
        }

        [Benchmark]
        public void TestOldStack()
        {
            _gump.Compile().Compile(false, out var _);
        }
    }
}
