using Server;
using Server.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PacketThrottlesTests
{
    [Fact]
    public void GetThrottle_RejectsPacketId256InsteadOfOverrunningTheTable()
    {
        var from = new Mobile((Serial)0x1);
        from.DefaultMobileInit();

        var exception = Record.Exception(() =>
            PacketThrottles.GetThrottle(new CommandEventArgs(from, "GetThrottle", "256", ["256"])));

        Assert.Null(exception);
    }
}
