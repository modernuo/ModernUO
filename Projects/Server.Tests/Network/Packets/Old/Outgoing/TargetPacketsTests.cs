using System;
using System.Buffers;
using Server.Network;
using Server.Targeting;
using Xunit;

namespace Server.Tests.Network.Packets
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
            int multiID = 0x1024;
            Point3D p = new Point3D(1000, 100, 10);
            MultiTarget t = new TestMultiTarget(multiID, p);

            Span<byte> data = new MultiTargetReqHS(t).Compile();

            Span<byte> expectedData = stackalloc byte[30];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x99); // Packet ID
            expectedData.Write(ref pos, t.AllowGround);
            expectedData.Write(ref pos, t.TargetID);

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, 0);
      expectedData.Write(ref pos, (ushort)0);
      expectedData.Write(ref pos, (ushort)0);
      expectedData.Write(ref pos, (byte)0);
      expectedData.Write(ref pos, (byte)0);
      expectedData.Write(ref pos, (ushort)0);
#else
            pos += 12;
#endif

            expectedData.Write(ref pos, (short)t.MultiID);
            expectedData.Write(ref pos, (ushort)t.Offset.X);
            expectedData.Write(ref pos, (ushort)t.Offset.Y);
            expectedData.Write(ref pos, (short)t.Offset.Z);

#if NO_LOCAL_INIT
      // Hue (4 bytes)
      expectedData.Write(ref pos, 0);
#endif

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMultiTargetReq()
        {
            int multiID = 0x1024;
            Point3D p = new Point3D(1000, 100, 10);
            MultiTarget t = new TestMultiTarget(multiID, p);

            Span<byte> data = new MultiTargetReq(t).Compile();

            Span<byte> expectedData = stackalloc byte[26];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x99); // Packet ID
            expectedData.Write(ref pos, t.AllowGround);
            expectedData.Write(ref pos, t.TargetID);

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, 0);
      expectedData.Write(ref pos, (ushort)0);
      expectedData.Write(ref pos, (ushort)0);
      expectedData.Write(ref pos, (byte)0);
      expectedData.Write(ref pos, (byte)0);
      expectedData.Write(ref pos, (ushort)0);
#else
            pos += 12;
#endif

            expectedData.Write(ref pos, (short)t.MultiID);
            expectedData.Write(ref pos, (ushort)t.Offset.X);
            expectedData.Write(ref pos, (ushort)t.Offset.Y);
            expectedData.Write(ref pos, (short)t.Offset.Z);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestCancelTarget()
        {
            Span<byte> data = new CancelTarget().Compile();

            Span<byte> expectedData = stackalloc byte[19];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x6C); // Packet ID
            expectedData.Write(ref pos, (byte)0);
            expectedData.Write(ref pos, 0);
            expectedData.Write(ref pos, (byte)3); // Beneficial / Harmful

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, 0);
      expectedData.Write(ref pos, (ushort)0);
      expectedData.Write(ref pos, (ushort)0);
      expectedData.Write(ref pos, (byte)0);
      expectedData.Write(ref pos, (byte)0);
      expectedData.Write(ref pos, (ushort)0);
#endif

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestTargetReq()
        {
            var t = new TestTarget(10, true, TargetFlags.Beneficial);
            Span<byte> data = new TargetReq(t).Compile();

            Span<byte> expectedData = stackalloc byte[19];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x6C); // Packet ID
            expectedData.Write(ref pos, t.AllowGround);
            expectedData.Write(ref pos, t.TargetID);
            expectedData.Write(ref pos, (byte)t.Flags);

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, 0);
      expectedData.Write(ref pos, (ushort)0);
      expectedData.Write(ref pos, (ushort)0);
      expectedData.Write(ref pos, (byte)0);
      expectedData.Write(ref pos, (byte)0);
      expectedData.Write(ref pos, (ushort)0);
#endif

            AssertThat.Equal(data, expectedData);
        }
    }
}
