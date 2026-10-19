using Server.Network;
using Server.Targeting;
using Xunit;

namespace Server.Tests.Network;

public class TestMultiTarget : MultiTarget
{
    public TestMultiTarget(
        int multiID,
        Point3D offset,
        int range = 10,
        bool allowGround = true,
        TargetFlags flags = TargetFlags.None
    ) : base(multiID, offset, range, allowGround, flags)
    {
    }
}

public class TestTarget : Target
{
    public TestTarget(
        int range,
        bool allowGround,
        TargetFlags flags
    ) : base(range, allowGround, flags)
    {
    }
}

[Collection("Sequential Server Tests")]
public class TargetPacketsTests
{
    [Fact]
    public void TestMultiTargetReqHS()
    {
        var multiID = 0x1024;
        var p = new Point3D(1000, 100, 10);
        MultiTarget t = new TestMultiTarget(multiID, p);

        PacketAssert.Sends(new MultiTargetReqHS(t), ns => ns.SendMultiTargetReq(t), ProtocolChanges.HighSeas);
    }

    [Fact]
    public void TestMultiTargetReq()
    {
        var multiID = 0x1024;
        var p = new Point3D(1000, 100, 10);
        MultiTarget t = new TestMultiTarget(multiID, p);

        PacketAssert.Sends(new MultiTargetReq(t), ns => ns.SendMultiTargetReq(t));
    }

    [Fact]
    public void TestCancelTarget()
    {
        PacketAssert.Sends(new CancelTarget(), ns => ns.SendCancelTarget());
    }

    [Fact]
    public void TestTargetReq()
    {
        var t = new TestTarget(10, true, TargetFlags.Beneficial);

        PacketAssert.Sends(new TargetReq(t), ns => ns.SendTargetReq(t));
    }
}
