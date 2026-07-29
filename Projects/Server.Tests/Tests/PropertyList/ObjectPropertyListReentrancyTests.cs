using System;
using Xunit;

namespace Server.Tests;

/// <summary>
/// The interpolation scratch buffer is rented by the compiler-generated handler ctor
/// (InitializeInterpolation) and returned by the closing Add. Reset()/Dispose() also return it, so
/// if either runs while a hole is still being evaluated the buffer is nulled underneath the
/// in-flight handler and the next Append* spans a null array:
///
///   System.ArgumentNullException: Value cannot be null. (Parameter 'array')
///     at Server.ObjectPropertyList.AppendStringDirect(String value)
///
/// This is reachable in practice. Mobile.InvalidateProperties rebuilds in place:
///
///   m_PropertyList.Reset();
///   InitializePropertyList(m_PropertyList);   // <- GetProperties runs with m_PropertyList set
///
/// so any property getter with an InvalidateProperties side effect that is read from inside
/// GetProperties re-enters, calls Reset() on the list being written, and kills the whole tooltip
/// build. Factions PlayerState.Rank is exactly such a getter: its getter calls Invalidate(), and
/// m_InvalidateRank defaults to true, so the first tooltip build for a faction member on the
/// faction facet after a load trips it.
/// </summary>
public class ObjectPropertyListReentrancyTests
{
    // Stands in for a property getter that invalidates the entity while its tooltip is being built.
    private static string ResettingHole(ObjectPropertyList list, string value)
    {
        list.Reset();
        return value;
    }

    private static string DisposingHole(ObjectPropertyList list, string value)
    {
        list.Dispose();
        return value;
    }

    [Fact]
    public void InterpolatedAdd_ResetMidHole_DoesNotThrow()
    {
        var opl = new ObjectPropertyList(null);

        // Mirrors PlayerMobile.GetProperties:
        //   list.Add(1060776, $"{pl.Rank.Title}\t{faction.Definition.PropName}");
        var ex = Record.Exception(
            () => opl.Add(1060776, $"{ResettingHole(opl, "Knight")}\t{"Council of Mages"}")
        );

        Assert.Null(ex);
    }

    [Fact]
    public void InterpolatedAdd_DisposeMidHole_DoesNotThrow()
    {
        var opl = new ObjectPropertyList(null);

        var ex = Record.Exception(
            () => opl.Add(1060776, $"{DisposingHole(opl, "Knight")}\t{"Council of Mages"}")
        );

        Assert.Null(ex);
    }
}
