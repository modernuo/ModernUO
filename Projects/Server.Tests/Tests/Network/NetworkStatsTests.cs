using System.Net.Sockets;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class NetworkStatsTests
{
    private static byte[] Pattern(int length, int seed)
    {
        var data = new byte[length];
        new System.Random(seed).NextBytes(data);
        return data;
    }

    [Fact]
    public void MaintainSendBuffers_RecordsTheRefusalsItResets()
    {
        var previous = NetState._availableMemoryBytes;
        NetState ns = null;
        Socket client = null;

        try
        {
            ns = PacketTestUtilities.CreateTestNetState(out client);
            ns._protocolState = NetState.ProtocolState.GameServer_AwaitingGameServerLogin;
            ns.Account = new MockAccount();
            var baseSize = ns._socket.SendBuffer.PhysicalSize;

            NetState.MaintainSendBuffers(); // drain whatever earlier tests refused
            NetState._availableMemoryBytes = 1; // any working set is above 80% of one byte

            for (var i = 0; i < 6; i++)
            {
                ns.Send(Pattern(baseSize / 4, i));
            }

            // The sweep refreshes _availableMemoryBytes itself, so the refusal must already be counted
            NetState.MaintainSendBuffers();
            var stats = NetState.GetNetworkStats();

            Assert.True(stats.LastSweep.Ran);
            Assert.True(stats.LastSweep.CeilingRefusals >= 1);

            // Counters are per sweep, not cumulative
            NetState.MaintainSendBuffers();
            Assert.Equal(0, NetState.GetNetworkStats().LastSweep.CeilingRefusals);
        }
        finally
        {
            NetState._availableMemoryBytes = previous;
            ns?.Dispose();
            client?.Close();
        }
    }

    [Fact]
    public void GetNetworkStats_CountsAuthenticatedConnectionsSeparately()
    {
        var ns = PacketTestUtilities.CreateTestNetState(out var client);

        try
        {
            var before = NetState.GetNetworkStats();
            ns._protocolState = NetState.ProtocolState.GameServer_AwaitingGameServerLogin;
            ns.Account = new MockAccount();
            var after = NetState.GetNetworkStats();

            Assert.Equal(before.Connected, after.Connected);
            Assert.Equal(before.Authenticated + 1, after.Authenticated);
            Assert.True(after.Connected >= after.Authenticated);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }
}
