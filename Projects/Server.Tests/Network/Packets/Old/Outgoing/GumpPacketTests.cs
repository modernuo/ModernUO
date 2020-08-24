using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Server.Gumps;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class GumpPacketTests
    {
        [Fact]
        public void TestCloseGump()
        {
            var typeId = 100;
            var buttonId = 10;

            Span<byte> data = new CloseGump(typeId, buttonId).Compile();

            Span<byte> expectedData = stackalloc byte[13];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xBF); // Packet ID
            expectedData.Write(ref pos, (ushort)0xD); // Length
            expectedData.Write(ref pos, (ushort)0x4); // Close Gump
            expectedData.Write(ref pos, typeId);
            expectedData.Write(ref pos, buttonId);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestDisplaySignGump()
        {
            Serial gumpSerial = 0x1000;
            var gumpId = 100;
            var unknownString = "This is an unknown string";
            var caption = "This is a caption";

            Span<byte> data = new DisplaySignGump(gumpSerial, gumpId, unknownString, caption).Compile();

            Span<byte> expectedData = stackalloc byte[15 + unknownString.Length + caption.Length];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x8B);
            expectedData.Write(ref pos, (ushort)expectedData.Length);
            expectedData.Write(ref pos, gumpSerial);
            expectedData.Write(ref pos, (ushort)gumpId);
            expectedData.Write(ref pos, (ushort)(unknownString.Length + 1));
            expectedData.WriteAsciiNull(ref pos, unknownString);
            expectedData.Write(ref pos, (ushort)(caption.Length + 1));
            expectedData.WriteAsciiNull(ref pos, caption);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestFastGumpPacket()
        {
            NetState ns = new NetState(new AccountPacketTests.TestConnectionContext
            {
                RemoteEndPoint = IPEndPoint.Parse("127.0.0.1"),
            });

            var gump = new ResurrectGump(2);

            Span<byte> data = gump.Compile(ns).Compile();

            Span<byte> expectedData = stackalloc byte[0x1000];

            int pos = 0;

            expectedData[pos++] = 0xB0; // Packet ID
            pos += 2; // Length

            expectedData.Write(ref pos, gump.Serial);
            expectedData.Write(ref pos, gump.TypeID);
            expectedData.Write(ref pos, gump.X);
            expectedData.Write(ref pos, gump.Y);
            pos += 2; // Layout Length

            int layoutLength = 0;

            if (!gump.Draggable)
            {
                expectedData.Write(ref pos, GumpUtilities.NoMoveBuffer);
                layoutLength += GumpUtilities.NoMove.Length;
            }

            if (!gump.Closable)
            {
                expectedData.Write(ref pos, GumpUtilities.NoCloseBuffer);
                layoutLength += GumpUtilities.NoClose.Length;
            }

            if (!gump.Disposable)
            {
                expectedData.Write(ref pos, GumpUtilities.NoDisposeBuffer);
                layoutLength += GumpUtilities.NoDispose.Length;
            }

            if (!gump.Resizable)
            {
                expectedData.Write(ref pos, GumpUtilities.NoResizeBuffer);
                layoutLength += GumpUtilities.NoResize.Length;
            }

            foreach (var entry in gump.Entries)
            {
                var str = entry.Compile(ns);
                expectedData.WriteAscii(ref pos, str);
                layoutLength += str.Length; // ASCII so 1:1
            }

            expectedData.Slice(19, 2).Write((ushort)layoutLength);
            expectedData.Write(ref pos, (ushort)gump.Strings.Count);

            for (var i = 0; i < gump.Strings.Count; ++i)
                expectedData.WriteBigUni(ref pos, gump.Strings[i] ?? "");

            expectedData.Slice(1, 2).Write((ushort)pos); // Length

            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestPackedGumpPacket()
        {
            NetState ns = new NetState(new AccountPacketTests.TestConnectionContext
            {
                RemoteEndPoint = IPEndPoint.Parse("127.0.0.1"),
            })
            {
                ProtocolChanges = ProtocolChanges.Unpack
            };

            var gump = new ResurrectGump(2);

            Span<byte> data = gump.Compile(ns).Compile();

            Span<byte> expectedData = stackalloc byte[0x1000];

            int pos = 0;

            expectedData[pos++] = 0xDD; // Packet ID
            pos += 2; // Length

            expectedData.Write(ref pos, gump.Serial);
            expectedData.Write(ref pos, gump.TypeID);
            expectedData.Write(ref pos, gump.X);
            expectedData.Write(ref pos, gump.Y);

            var layoutList = new List<string>();
            int bufferLength = 1; // Null terminated

            if (!gump.Draggable)
            {
                layoutList.Add(GumpUtilities.NoMove);
                bufferLength += GumpUtilities.NoMove.Length;
            }

            if (!gump.Closable)
            {
                layoutList.Add(GumpUtilities.NoClose);
                bufferLength += GumpUtilities.NoClose.Length;
            }

            if (!gump.Disposable)
            {
                layoutList.Add(GumpUtilities.NoDispose);
                bufferLength += GumpUtilities.NoDispose.Length;
            }

            if (!gump.Resizable)
            {
                layoutList.Add(GumpUtilities.NoResize);
                bufferLength += GumpUtilities.NoResize.Length;
            }

            foreach (var entry in gump.Entries)
            {
                var str = entry.Compile(ns);
                bufferLength += str.Length;
                layoutList.Add(str);
            }

            IMemoryOwner<byte> memOwner = SlabMemoryPool.Shared.Rent(bufferLength);

            Span<byte> buffer = memOwner.Memory.Span;
            int bufferPos = 0;

            foreach (var layout in layoutList)
                buffer.WriteAscii(ref bufferPos, layout);

#if NO_LOCAL_INIT
      buffer.Write(ref bufferPos, (byte)0); // Layout terminator
#else
            bufferPos++;
#endif

            expectedData.WritePacked(ref pos, buffer.Slice(0, bufferPos));
            memOwner.Dispose();

            expectedData.Write(ref pos, gump.Strings.Count);
            bufferLength = gump.Strings.Sum(str => 2 + str.Length * 2);
            memOwner = SlabMemoryPool.Shared.Rent(bufferLength);
            buffer = memOwner.Memory.Span;
            bufferPos = 0;

            foreach (var str in gump.Strings)
                buffer.WriteBigUni(ref bufferPos, str);

            expectedData.WritePacked(ref pos, buffer.Slice(0, bufferPos));

            // Length
            expectedData.Slice(1, 2).Write((ushort)pos);
            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }
    }

    public class ResurrectGump : Gump
    {
        public ResurrectGump(int msg) : base(100, 0)
        {
            AddPage(0);

            AddBackground(0, 0, 400, 350, 2600);

            AddHtmlLocalized(0, 20, 400, 35, 1011022); // <center>Resurrection</center>

            /* It is possible for you to be resurrected here by this healer. Do you wish to try?<br>
             * CONTINUE - You chose to try to come back to life now.<br>
             * CANCEL - You prefer to remain a ghost for now.
             */
            AddHtmlLocalized(50, 55, 300, 140, 1011023 + msg, true, true);

            AddButton(200, 227, 4005, 4007, 0);
            AddHtmlLocalized(235, 230, 110, 35, 1011012); // CANCEL

            AddButton(65, 227, 4005, 4007, 1);
            AddHtmlLocalized(100, 230, 110, 35, 1011011); // CONTINUE
        }
    }
}
