using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Tests")]
public class ClientVersionTests
{
    [Theory]
    [InlineData("7.0.33.1", "67.0.33", false, 0)]
    [InlineData("7.0.45.65", "67.0.33", false, 1)]
    [InlineData("6.0.0.0", "67.0.33", false, -1)]
    public void TestCCAndECStringCtor(string ccVersion, string ecVersion, bool equal, int comparison)
    {
        var cc = new ClientVersion(ccVersion);
        var ec = new ClientVersion(ecVersion);

        Assert.Equal(equal, cc == ec);
        Assert.Equal(comparison, cc.CompareTo(ec));
    }

    [Theory]
    [InlineData(7, 0, 33, 1, 67, 0, 33, 0, false, 0)]
    [InlineData(7, 0, 45, 65, 67, 0, 33, 0, false, 1)]
    [InlineData(6, 0, 0, 0, 67, 0, 33, 0, false, -1)]
    public void TestCCAndECCtor(
        int ccMaj, int ccMin, int ccRev, int ccPatch, int ecMaj, int ecMin, int ecRev, int ecPatch,
        bool equal, int comparison
    )
    {
        var cc = new ClientVersion(ccMaj, ccMin, ccRev, ccPatch);
        var ec = new ClientVersion(ecMaj, ecMin, ecRev, ecPatch);

        Assert.Equal(equal, cc == ec);
        Assert.Equal(comparison, cc.CompareTo(ec));
    }
}
