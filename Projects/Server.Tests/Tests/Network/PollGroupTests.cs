using System;
using System.Runtime.InteropServices;
using System.Threading;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

public class PollGroupTests
{
    [Fact]
    public void TestPollGroup()
    {
        // var group = new KQueuePollGroup();
        var nss = new NetState[2048];
        var handles = new IntPtr[2048];
        for (var i = 0; i < nss.Length; i++)
        {
            nss[i] = PacketTestUtilities.CreateTestNetState();
            handles[i] = (IntPtr)nss[i].Handle;
        }

        GC.AddMemoryPressure(10000000000);
        GC.Collect();
        GC.RemoveMemoryPressure(10000000000);
        GC.Collect();

        Thread.Sleep(1000);

        for (var i = 0; i < nss.Length; i++)
        {
            Assert.Equal(nss[i].Handle, (GCHandle)handles[i]);
        }

        // group.Dispose();
    }
}
