using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests.Gumps;

// TextDefinition is both [PropertyObject] and parsable. The props gump checks [PropertyObject]
// first, so without an explicit branch a TextDefinition row drills into a read-only Number/String
// gump (or, when the value is null, silently redraws the same page) instead of opening an editor.
// It broke exactly that way once already: #1217 added [PropertyObject] to TextDefinition and
// hijacked the routing that #765 had left working. These tests pin the routing down.
[Collection("Sequential UOContent Tests")]
public class PropsGumpTextDefinitionTests : IDisposable
{
    // Mirrors PropertiesGump.MaxEntriesPerPage, which is private.
    private const int MaxEntriesPerPage = 15;

    private readonly List<Item> _items = [];
    private readonly List<Mobile> _mobiles = [];
    private readonly List<NetState> _states = [];

    public void Dispose()
    {
        for (var i = 0; i < _mobiles.Count; i++)
        {
            _mobiles[i].NetState = null;
            _mobiles[i].Delete();
        }

        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Delete();
        }

        for (var i = 0; i < _states.Count; i++)
        {
            _states[i].Dispose();
        }

        _mobiles.Clear();
        _items.Clear();
        _states.Clear();
    }

    private (Mobile From, NetState State) CreateStaff()
    {
        var ns = PacketTestUtilities.CreateTestNetState();
        _states.Add(ns);

        var from = new Mobile { AccessLevel = AccessLevel.GameMaster };
        from.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        _mobiles.Add(from);

        from.NetState = ns;
        ns.Mobile = from;

        return (from, ns);
    }

    private SkillTeleporter CreateTeleporter(TextDefinition message)
    {
        var tp = new SkillTeleporter { Message = message };
        tp.MoveToWorld(new Point3D(1001, 1000, 0), Map.Felucca);
        _items.Add(tp);
        return tp;
    }

    // Opens the props gump on whichever page holds `propName` and presses that row's gold '>'.
    private static void PressPropertyButton(Mobile from, NetState ns, object o, string propName)
    {
        var probe = new TestPropsGump(from, o);
        var list = probe.List;

        var index = -1;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is PropertyInfo p && p.Name == propName)
            {
                index = i;
                break;
            }
        }

        Assert.True(index >= 0, $"{o.GetType().Name} has no visible '{propName}' property row.");

        var page = index / MaxEntriesPerPage;
        var buttonId = index - page * MaxEntriesPerPage + 3;

        var gump = new TestPropsGump(from, o, list, page);
        gump.OnResponse(ns, EmptyInfo(buttonId));
    }

    private static RelayInfo EmptyInfo(int buttonId) =>
        new(buttonId, ReadOnlySpan<int>.Empty, ReadOnlySpan<ushort>.Empty, ReadOnlySpan<Range>.Empty, ReadOnlySpan<byte>.Empty);

    private static RelayInfo TextInfo(int buttonId, int entryId, string text)
    {
        var block = Encoding.BigEndianUnicode.GetBytes(text);
        var ids = new[] { (ushort)entryId };
        var ranges = new[] { new Range(0, block.Length) };

        return new RelayInfo(buttonId, ReadOnlySpan<int>.Empty, ids, ranges, block);
    }

    [Fact]
    public void PressingSetOnAPopulatedTextDefinitionOpensTheEditor()
    {
        var (from, ns) = CreateStaff();
        var tp = CreateTeleporter(TextDefinition.Of(1060847));

        PressPropertyButton(from, ns, tp, nameof(SkillTeleporter.Message));

        Assert.NotNull(ns.FindGump<SetGump>());
    }

    // The null case is the one that looks most broken in-game: the PropertyObject branch has no
    // object to drill into, so it re-sends the same page and the button appears to do nothing.
    [Fact]
    public void PressingSetOnANullTextDefinitionOpensTheEditor()
    {
        var (from, ns) = CreateStaff();
        var tp = CreateTeleporter(null);

        PressPropertyButton(from, ns, tp, nameof(SkillTeleporter.Message));

        Assert.NotNull(ns.FindGump<SetGump>());
    }

    // Numeric input always wins and becomes a cliloc -- the same rule TextDefinition.Parse gives
    // the [set command -- so "0" yields cliloc 0 (an empty definition), never the string "0".
    [Theory]
    [InlineData("1060847", 1060847, null)]
    [InlineData("0", 0, null)]
    [InlineData("Hail, traveller.", 0, "Hail, traveller.")]
    public void EditorRoundTripsClilocAndString(string entered, int expectedNumber, string expectedString)
    {
        var (from, ns) = CreateStaff();
        var tp = CreateTeleporter(null);

        PressPropertyButton(from, ns, tp, nameof(SkillTeleporter.Message));

        var setGump = ns.FindGump<SetGump>();
        Assert.NotNull(setGump);

        setGump.OnResponse(ns, TextInfo(1, 0, entered));

        Assert.NotNull(tp.Message);
        Assert.Equal(expectedNumber, tp.Message.Number);
        Assert.Equal(expectedString, tp.Message.String);
    }

    // A string that looks like a cliloc has to survive a trip through the editor untouched. The
    // edit box is seeded from GetValue(), so that is the text a GM sees and presses OK on.
    [Fact]
    public void EditorPreservesAStringThatLooksLikeACliloc()
    {
        var (from, ns) = CreateStaff();
        var tp = CreateTeleporter(TextDefinition.Of("1060847"));

        PressPropertyButton(from, ns, tp, nameof(SkillTeleporter.Message));

        var setGump = ns.FindGump<SetGump>();
        Assert.NotNull(setGump);

        setGump.OnResponse(ns, TextInfo(1, 0, tp.Message.GetValue()));

        Assert.NotNull(tp.Message);
        Assert.Equal(0, tp.Message.Number);
        Assert.Equal("1060847", tp.Message.String);
    }

    [Fact]
    public void EditorNullButtonClearsTheValue()
    {
        var (from, ns) = CreateStaff();
        var tp = CreateTeleporter(TextDefinition.Of("Hail, traveller."));

        PressPropertyButton(from, ns, tp, nameof(SkillTeleporter.Message));

        var setGump = ns.FindGump<SetGump>();
        Assert.NotNull(setGump);

        setGump.OnResponse(ns, EmptyInfo(2));

        Assert.Null(tp.Message);
    }

    // Exposes the protected list/page plumbing so a test can aim at a specific property row.
    private sealed class TestPropsGump : PropertiesGump
    {
        public TestPropsGump(Mobile m, object o) : base(m, o)
        {
        }

        public TestPropsGump(Mobile m, object o, List<object> list, int page) : base(m, o, null, list, page)
        {
        }

        public List<object> List => m_List;
    }
}
