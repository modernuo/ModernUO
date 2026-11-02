using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class NetStateExpansionTests
{
    [Theory]
    [InlineData("5.0.9.1", ClientFlags.None, Expansion.ML)]
    [InlineData("4.0.11c", ClientFlags.None, Expansion.None)]
    [InlineData("4.0.11c", ClientFlags.Felucca | ClientFlags.Trammel | ClientFlags.Ilshenar | ClientFlags.Malas, Expansion.AOS)]
    [InlineData("7.0.5.0", ClientFlags.None, Expansion.ML)]
    [InlineData("7.0.5.0", ClientFlags.TerMur, Expansion.SA)]
    [InlineData("7.0.20.0", ClientFlags.None, Expansion.HS)]
    [InlineData("7.0.110.0", ClientFlags.None, Expansion.EJ)]
    public void ResolvesExpansionFromVersionAndFlags(string version, ClientFlags flags, Expansion expected)
    {
        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.Version = new ClientVersion(version);
        ns.Flags = flags;

        Assert.Equal(expected, (Expansion)ns.ExpansionInfo.Id);
    }

    [Fact]
    public void VersionlessClientWithoutFlags_ResolvesToNone()
    {
        using var ns = PacketTestUtilities.CreateTestNetState();

        Assert.Equal(Expansion.None, (Expansion)ns.ExpansionInfo.Id);
    }

    [Fact]
    public void LateVersion_ReResolvesCachedExpansion()
    {
        using var ns = PacketTestUtilities.CreateTestNetState();
        Assert.Equal(Expansion.None, (Expansion)ns.ExpansionInfo.Id);

        ns.Version = new ClientVersion("7.0.20.0");
        Assert.Equal(Expansion.HS, (Expansion)ns.ExpansionInfo.Id);
    }

    [Fact]
    public void LateFlags_ReResolvesCachedExpansion()
    {
        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.Version = new ClientVersion("4.0.11c");
        Assert.Equal(Expansion.None, (Expansion)ns.ExpansionInfo.Id);

        ns.Flags = ClientFlags.Tokuno;
        Assert.Equal(Expansion.SE, (Expansion)ns.ExpansionInfo.Id);
    }
}
