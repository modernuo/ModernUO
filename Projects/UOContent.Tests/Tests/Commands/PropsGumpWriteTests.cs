using System;
using System.Collections.Generic;
using System.Text;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests.Commands;

[Collection("Sequential UOContent Tests")]
public class PropsGumpWriteTests : IDisposable
{
    private readonly List<Mobile> _mobiles = [];
    private readonly List<NetState> _states = [];

    public void Dispose()
    {
        for (var i = 0; i < _mobiles.Count; i++)
        {
            _mobiles[i].NetState = null;
            _mobiles[i].Delete();
        }

        for (var i = 0; i < _states.Count; i++)
        {
            _states[i].Dispose();
        }

        _mobiles.Clear();
        _states.Clear();
    }

    private (Mobile From, NetState State) CreateEditor(AccessLevel level)
    {
        var ns = PacketTestUtilities.CreateTestNetState();
        _states.Add(ns);

        var from = new Mobile { AccessLevel = level };
        from.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        _mobiles.Add(from);

        from.NetState = ns;
        ns.Mobile = from;

        return (from, ns);
    }

    private Mobile CreateTarget(AccessLevel level)
    {
        var m = new Mobile { Name = "Original", AccessLevel = level };
        m.MoveToWorld(new Point3D(1001, 1000, 0), Map.Felucca);
        _mobiles.Add(m);
        return m;
    }

    private static RelayInfo NameInfo(string text)
    {
        var block = Encoding.BigEndianUnicode.GetBytes(text);
        var ids = new[] { (ushort)0 };
        var ranges = new[] { new Range(0, block.Length) };

        return new RelayInfo(1, ReadOnlySpan<int>.Empty, ids, ranges, block);
    }

    private static void SubmitName(Mobile from, NetState ns, Mobile target, string text)
    {
        var prop = typeof(Mobile).GetProperty(nameof(Mobile.Name));
        var gump = new PropertiesGump(from, target);
        var setGump = new SetGump(prop, from, target, gump);

        setGump.OnResponse(ns, NameInfo(text));
    }

    [Fact]
    public void SetGump_HigherPrivilegedMobile_IsRefused()
    {
        var (from, ns) = CreateEditor(AccessLevel.Administrator);
        var target = CreateTarget(AccessLevel.Developer);

        SubmitName(from, ns, target, "Changed");

        Assert.Equal("Original", target.Name);
    }

    [Fact]
    public void SetGump_AllowedWrite_StillWorks()
    {
        var (from, ns) = CreateEditor(AccessLevel.Administrator);
        var target = CreateTarget(AccessLevel.Player);

        SubmitName(from, ns, target, "Changed");

        Assert.Equal("Changed", target.Name);
    }

    [Fact]
    public void PropsGump_EditBelowReadLevel_IsIgnored()
    {
        var (from, ns) = CreateEditor(AccessLevel.GameMaster);
        var subject = new ReadRestricted();
        var prop = typeof(ReadRestricted).GetProperty(nameof(ReadRestricted.Secret));

        var gump = new PropertiesGump(from, subject, null, [prop], 0);
        var readsBefore = subject.Reads;
        gump.OnResponse(ns, new RelayInfo(3, ReadOnlySpan<int>.Empty, ReadOnlySpan<ushort>.Empty, ReadOnlySpan<Range>.Empty, ReadOnlySpan<byte>.Empty));

        Assert.Null(ns.FindGump<SetGump>());
        Assert.Equal(readsBefore, subject.Reads);
    }

    private class ReadRestricted
    {
        public int Reads { get; private set; }

        [CommandProperty(AccessLevel.Administrator, AccessLevel.GameMaster)]
        public string Secret
        {
            get
            {
                Reads++;
                return "x";
            }
            set { }
        }
    }
}
