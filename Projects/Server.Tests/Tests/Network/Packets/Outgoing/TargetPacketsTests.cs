using Server.Network;
using Server.Targeting;
using Xunit;

namespace Server.Tests.Network
{
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

    public class TargetPacketsTests
    {
        [Fact]
        public void TestMultiTargetReqHS()
        {
            var multiID = 0x1024;
            var p = new Point3D(1000, 100, 10);
            MultiTarget t = new TestMultiTarget(multiID, p);

            var expected = new MultiTargetReqHS(t).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges |= ProtocolChanges.HighSeas;
            ns.SendMultiTargetReq(t);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestMultiTargetReq()
        {
            var multiID = 0x1024;
            var p = new Point3D(1000, 100, 10);
            MultiTarget t = new TestMultiTarget(multiID, p);

            var expected = new MultiTargetReq(t).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMultiTargetReq(t);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestCancelTarget()
        {
            var expected = new CancelTarget().Compile();
            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendCancelTarget();

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestTargetReq()
        {
            var t = new TestTarget(10, true, TargetFlags.Beneficial);
            var expected = new TargetReq(t).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendTargetReq(t);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }
    }
}
