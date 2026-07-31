using System;
using Xunit;

namespace Server.Tests;

/// <summary>
/// The interpolation buffer is rented by the handler ctor and returned by the closing Add, so every
/// hole is evaluated while it is live. A Reset()/Dispose() landing in that window used to leave the
/// next Append* spanning a null array: ArgumentNullException, parameter "array".
/// </summary>
// Sequential: building a list rents from STArrayPool, which is not thread-safe.
[Collection("Sequential Server Tests")]
public class ObjectPropertyListReentrancyTests
{
    // Stands in for a property getter that invalidates while its own tooltip is being built.
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

/// <summary>
/// The guard is per-list, so nested builds (a GetProperties override that reads another entity's
/// PropertyList) cannot unguard the outer one the way a single shared slot would.
/// </summary>
[Collection("Sequential Server Tests")]
public class ObjectPropertyListNestedBuildTests
{
    [Fact]
    public void NestedBuild_DoesNotUnguardTheOuterList()
    {
        var outer = new ObjectPropertyList(null);
        var inner = new ObjectPropertyList(null);

        outer.IsBuilding = true;
        inner.IsBuilding = true;   // another entity starts building, and finishes
        inner.IsBuilding = false;

        Assert.True(outer.IsBuilding);
    }

    [Fact]
    public void Reset_MidInterpolation_LeavesTheListUsable()
    {
        var opl = new ObjectPropertyList(null);

        opl.Add(1060776, $"{Reset(opl, "Knight")}\t{"Council of Mages"}");
        opl.Add(1042971, "still working");
        opl.Terminate();

        Assert.NotNull(opl.Buffer);
    }

    private static string Reset(ObjectPropertyList list, string value)
    {
        list.Reset();
        return value;
    }
}


/// <summary>
/// Invalidating from inside GetProperties is a defect in the getter, not a case to recover from:
/// DEBUG throws, RELEASE keeps a possibly stale tooltip without crashing or leaking.
/// </summary>
[Collection("Sequential Server Tests")]
public class PropertyListInvalidationDuringBuildTests
{
    private class SelfInvalidatingMobile : Mobile
    {
        public int Builds;

        public override void GetProperties(IPropertyList list)
        {
            Builds++;
            base.GetProperties(list);
            InvalidateProperties();
            list.Add(1060776, $"{"Knight"}\t{"Council of Mages"}");
        }
    }

    private static SelfInvalidatingMobile Place(int x)
    {
        var m = new SelfInvalidatingMobile();
        m.MoveToWorld(new Point3D(x, 1000, 0), Map.Felucca);
        return m;
    }

    [Fact]
    public void InvalidatingFromGetProperties_FailsLoudlyWithoutTearingDownTheBuild()
    {
        var wasEnabled = ObjectPropertyList.Enabled;
        ObjectPropertyList.Enabled = true;

        try
        {
            var m = Place(1000);

#if DEBUG
            Assert.Throws<InvalidOperationException>(() => _ = m.PropertyList);
#else
            Assert.Null(Record.Exception(() => _ = m.PropertyList));
#endif

            // Refused, not retried.
            Assert.Equal(1, m.Builds);
            m.Delete();
        }
        finally
        {
            ObjectPropertyList.Enabled = wasEnabled;
        }
    }
}
