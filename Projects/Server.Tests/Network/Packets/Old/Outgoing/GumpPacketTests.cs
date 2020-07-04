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

      Span<byte> expectedData = stackalloc byte[]
      {
        0xBF, // Packet ID
        0x00, 0xD, // Length
        0x00, 0x04, // Close
        0x00, 0x00, 0x00, 0x00, // Type Id
        0x00, 0x00, 0x00, 0x00, // Button Id
      };

      typeId.CopyTo(expectedData.Slice(5, 4));
      buttonId.CopyTo(expectedData.Slice(9, 4));

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
      ((byte)0x8B).CopyTo(ref pos, expectedData);
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData);
      gumpSerial.CopyTo(ref pos, expectedData);
      ((ushort)gumpId).CopyTo(ref pos, expectedData);
      unknownString.CopyASCIINullTo(ref pos, expectedData);
      caption.CopyASCIINullTo(ref pos, expectedData);

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

      gump.Serial.CopyTo(ref pos, expectedData);
      gump.TypeID.CopyTo(ref pos, expectedData);
      gump.X.CopyTo(ref pos, expectedData);
      gump.Y.CopyTo(ref pos, expectedData);
      pos += 2; // Layout Length

      int layoutLength = 0;

      if (!gump.Draggable)
      {
        GumpUtilities.NoMoveBuffer.CopyTo(ref pos, expectedData);
        layoutLength += GumpUtilities.NoMove.Length;
      }

      if (!gump.Closable)
      {
        GumpUtilities.NoCloseBuffer.CopyTo(ref pos, expectedData);
        layoutLength += GumpUtilities.NoClose.Length;
      }

      if (!gump.Disposable)
      {
        GumpUtilities.NoDisposeBuffer.CopyTo(ref pos, expectedData);
        layoutLength += GumpUtilities.NoDispose.Length;
      }

      if (!gump.Resizable)
      {
        GumpUtilities.NoResizeBuffer.CopyTo(ref pos, expectedData);
        layoutLength += GumpUtilities.NoResize.Length;
      }

      foreach (var entry in gump.Entries)
      {
        var str = entry.Compile(ns);
        str.CopyRawASCIITo(ref pos, expectedData);
        layoutLength += str.Length; // ASCII so 1:1
      }

      ((ushort)layoutLength).CopyTo(expectedData.Slice(19, 2));
      ((ushort)gump.Strings.Count).CopyTo(ref pos, expectedData);

      for (var i = 0; i < gump.Strings.Count; ++i)
        (gump.Strings[i] ?? "").CopyUnicodeBigEndianTo(ref pos, expectedData);

      ((ushort)pos).CopyTo(expectedData.Slice(1, 2));

      expectedData = expectedData.Slice(0, pos);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestPackedGumpPacket()
    {
      NetState ns = new NetState(new AccountPacketTests.TestConnectionContext
      {
        RemoteEndPoint = IPEndPoint.Parse("127.0.0.1"),
      });

      ns.ProtocolChanges = ProtocolChanges.Unpack;

      var gump = new ResurrectGump(2);

      Span<byte> data = gump.Compile(ns).Compile();

      Span<byte> expectedData = stackalloc byte[0x1000];

      int pos = 0;

      expectedData[pos++] = 0xDD; // Packet ID
      pos += 2; // Length

      gump.Serial.CopyTo(ref pos, expectedData);
      gump.TypeID.CopyTo(ref pos, expectedData);
      gump.X.CopyTo(ref pos, expectedData);
      gump.Y.CopyTo(ref pos, expectedData);

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
        layout.CopyRawASCIITo(ref bufferPos, buffer);

      pos += GumpUtilities.WritePacked(buffer.Slice(0, bufferPos + 1), expectedData.Slice(pos));
      memOwner.Dispose();

      gump.Strings.Count.CopyTo(ref pos, expectedData);
      bufferLength = gump.Strings.Sum(str => 2 + str.Length * 2);
      memOwner = SlabMemoryPool.Shared.Rent(bufferLength);
      buffer = memOwner.Memory.Span;
      bufferPos = 0;

      foreach (var str in gump.Strings)
        str.CopyUnicodeBigEndianTo(ref bufferPos, buffer);

      pos += GumpUtilities.WritePacked(buffer.Slice(0, bufferPos), expectedData.Slice(pos));

      // Length
      ((ushort)pos).CopyTo(expectedData.Slice(1, 2));
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
