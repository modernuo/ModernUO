using System;
using System.Buffers;
using System.IO;
using Server.Gumps;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests.Gumps;

[Collection("Sequential UOContent Tests")]
public class TestLayoutGumps
{
    [Fact]
    public void TestDynamicGumpPacket()
    {
        var legacyGump = new LegacyTestGump("Test");
        var legacyPacketData = legacyGump.Compile().Compile();

        var staticGump = new DynamicTestGump("Test");
        var buffer = GC.AllocateUninitializedArray<byte>(512);
        var writer = new SpanWriter(buffer);
        staticGump.Compile(ref writer);

        AssertThat.Equal(writer.Span, legacyPacketData);
    }

    [Fact]
    public void TestStaticLayoutGumpPacket()
    {
        var expectedLayout =
            "{ page 0 }{ resizepic 10 10 9260 265 140 }{ tilepic 205 40 4 }{ tilepic 227 40 5 }{ tilepic 180 78 3246 }{ tilepic 195 90 3245 }{ tilepic 218 95 3248 }{ htmlgump 30 30 150 75 1 0 0 }{ htmlgump 30 70 150 25 00002 1 0 }{ button 40 105 2074 2075 1 0 1 }{ button 110 105 2073 2072 1 0 2 }\0"u8;

        string[] strings =
        [
            "<div align=center>Wilt thou sanctify the resurrection of:</div>",
            "<CENTER>Test</CENTER>"
        ];

        InternalTestStaticGump(expectedLayout, new StaticLayoutTestGump("Test"), strings);
    }

    [Fact]
    public void TestStaticGumpPacket()
    {
        var expectedLayout =
            "{ page 0 }{ resizepic 10 10 9260 265 140 }{ tilepic 205 40 4 }{ tilepic 227 40 5 }{ tilepic 180 78 3246 }{ tilepic 195 90 3245 }{ tilepic 218 95 3248 }{ htmlgump 30 30 150 75 1 0 0 }{ htmlgump 30 70 150 25 2 1 0 }{ button 40 105 2074 2075 1 0 1 }{ button 110 105 2073 2072 1 0 2 }\0"u8;

        string[] strings =
        [
            "<div align=center>Wilt thou sanctify the resurrection of:</div>",
            "<CENTER>Test</CENTER>"
        ];

        InternalTestStaticGump(expectedLayout, new StaticTestGump(), strings);
    }

    [Fact]
    public void TestStaticGumpIsCached()
    {
        var gump = new CachedGump();
        var buffer = GC.AllocateUninitializedArray<byte>(512);
        var writer = new SpanWriter(buffer);
        gump.Compile(ref writer);

        var packet = writer.Span.ToArray();

        // Reset the writer
        writer.Seek(0, SeekOrigin.Begin);

        // Second call should not call BuildLayout
        gump.Compile(ref writer);

        AssertThat.Equal(writer.Span, packet);
    }

    private static void InternalTestStaticGump<T>(ReadOnlySpan<byte> expectedLayout, StaticGump<T> staticGump, string[] strings)
        where T : StaticGump<T>
    {
        // Expected layout
        var expectedBuffer = GC.AllocateUninitializedArray<byte>(512);
        var expectedBufferWriter = new SpanWriter(expectedBuffer);
        OutgoingGumpPackets.WritePacked(expectedLayout, ref expectedBufferWriter);
        var layoutLength = expectedBufferWriter.BytesWritten;

        var buffer = GC.AllocateUninitializedArray<byte>(512);
        var writer = new SpanWriter(buffer);
        staticGump.Compile(ref writer);

        // Assert layout is exactly what we are expecting
        AssertThat.Equal(writer.Span.Slice(19, layoutLength), expectedBufferWriter.Span);

        // Assert strings count
        AssertThat.Equal(writer.Span.Slice(19 + layoutLength, 4), stackalloc byte[] { 0, 0, 0, 3 });

        var expectedStringsBuffer = GC.AllocateUninitializedArray<byte>(512);
        var expectedStringsWriter = new SpanWriter(expectedStringsBuffer);

        // Empty string
        expectedStringsWriter.Write((ushort)0);

        // loop through the strings, write them to the strings writer
        foreach (var str in strings)
        {
            expectedStringsWriter.Write((ushort)str.Length);
            expectedStringsWriter.WriteBigUni(str);
        }

        // Reset buffer
        expectedBufferWriter.Seek(0, SeekOrigin.Begin);

        OutgoingGumpPackets.WritePacked(expectedStringsWriter.Span, ref expectedBufferWriter);

        // Assert strings are exactly what we are expecting
        AssertThat.Equal(writer.Span[(19 + layoutLength + 4)..], expectedBufferWriter.Span);
    }

    private class CachedGump : StaticGump<CachedGump>
    {
        private bool _isCachedLayout;

        public CachedGump() : base(50, 50)
        {
            Serial = (Serial)0x124;
            TypeID = 0x5346;
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            Assert.False(_isCachedLayout);
            _isCachedLayout = true;

            builder.AddPage();

            builder.AddHtml(30, 30, 150, 75, "Some text");
        }

        protected override void BuildStrings(ref GumpStringsBuilder builder)
        {
            Assert.Fail("BuildStrings should not be called when the layout is cached.");
        }
    }
}
