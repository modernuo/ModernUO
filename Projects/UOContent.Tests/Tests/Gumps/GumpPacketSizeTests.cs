using System;
using System.Buffers.Binary;
using Server.Gumps;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests.Gumps;

[Collection("Sequential UOContent Tests")]
public class GumpPacketSizeTests
{
    // Random CJK text is close to incompressible, so this many characters cannot pack into one packet.
    private const int OversizedChars = 48000;
    private const int StringCount = 12;

    [Fact]
    public void OversizedDynamicGump_IsNotSent()
    {
        AssertNotSent(new StringsDynamicGump(RandomStrings(1)));
    }

    [Fact]
    public void OversizedLegacyGump_IsNotSent()
    {
        AssertNotSent(new StringsLegacyGump(RandomStrings(2)));
    }

    [Fact]
    public void OversizedDynamicLayout_IsNotSent()
    {
        AssertNotSent(new LayoutDynamicGump());
    }

    [Fact]
    public void OversizedStaticGump_IsNeverSent()
    {
        // The second send takes the cached path, which must not replay a layout whose strings never fit.
        AssertNotSent(new StringsStaticGump());
        AssertNotSent(new StringsStaticGump());
    }

    [Fact]
    public void LargeCompressibleGump_IsSentWithItsLength()
    {
        var strings = new string[StringCount];
        Array.Fill(strings, new string('a', OversizedChars / StringCount));

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendGump(new StringsDynamicGump(strings));

        var sent = ns.SendBuffer.GetReadSpan();
        Assert.Equal(0xDD, sent[0]);
        Assert.Equal(sent.Length, BinaryPrimitives.ReadUInt16BigEndian(sent[1..]));
        Assert.True(ns.HasGump<StringsDynamicGump>());
    }

    private static void AssertNotSent<T>(T gump) where T : BaseGump
    {
        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendGump(gump);

        Assert.Equal(0, ns.SendBuffer.GetReadSpan().Length);
        Assert.False(ns.HasGump<T>());
    }

    private static string[] RandomStrings(int seed)
    {
        var random = new System.Random(seed);
        var strings = new string[StringCount];
        var chars = new char[OversizedChars / StringCount];

        for (var i = 0; i < strings.Length; i++)
        {
            for (var j = 0; j < chars.Length; j++)
            {
                chars[j] = (char)(0x4E00 + random.Next(0x5000));
            }

            strings[i] = new string(chars);
        }

        return strings;
    }

    private sealed class StringsDynamicGump : DynamicGump
    {
        private readonly string[] _strings;

        public StringsDynamicGump(string[] strings) : base(0, 0) => _strings = strings;

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            for (var i = 0; i < _strings.Length; i++)
            {
                builder.AddHtml(10, 10 + i * 20, 200, 20, _strings[i]);
            }
        }
    }

    private sealed class StringsLegacyGump : Gump
    {
        public StringsLegacyGump(string[] strings) : base(0, 0)
        {
            AddPage(0);

            for (var i = 0; i < strings.Length; i++)
            {
                AddHtml(10, 10 + i * 20, 200, 20, strings[i]);
            }
        }
    }

    private sealed class LayoutDynamicGump : DynamicGump
    {
        public LayoutDynamicGump() : base(0, 0)
        {
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            var random = new System.Random(3);
            builder.AddPage();

            // Random coordinates and art keep the layout text from compressing below the packet limit.
            for (var i = 0; i < 15000; i++)
            {
                builder.AddItem(random.Next(10000), random.Next(10000), random.Next(0x10000), random.Next(3000));
            }
        }
    }

    private sealed class StringsStaticGump : StaticGump<StringsStaticGump>
    {
        private static readonly string[] _strings = RandomStrings(4);

        public StringsStaticGump() : base(0, 0)
        {
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();

            for (var i = 0; i < _strings.Length; i++)
            {
                builder.AddHtml(10, 10 + i * 20, 200, 20, _strings[i]);
            }
        }
    }
}
