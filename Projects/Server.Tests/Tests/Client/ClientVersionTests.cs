using Xunit;

namespace Server.Tests.Network;

public class ClientVersionTests
{
    [Theory]
    [InlineData("7.0.33.1", "67.0.33", false, 0)]
    [InlineData("7.0.45.65", "67.0.33", false, 1)]
    [InlineData("6.0.0.0", "67.0.33", false, -1)]
    [InlineData("7.0.33.1", "7.0.33.1", true, 0)]
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
    [InlineData(7, 0, 33, 1, 7, 0, 33, 1, true, 0)]
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

    [Theory]
    [InlineData("4.0.11c", 4, 0, 11, 3)]
    [InlineData("4.0.11", 4, 0, 11, 0)]
    [InlineData("5.0.0a", 5, 0, 0, 1)]
    [InlineData("5.0.2b", 5, 0, 2, 2)]
    [InlineData("5.0.6e", 5, 0, 6, 5)]
    [InlineData("5.0.7.0", 5, 0, 7, 0)]
    [InlineData("5.0.9.1", 5, 0, 9, 1)]
    [InlineData("4.0.11c uotd", 4, 0, 11, 3)]
    public void TestLetterPatchStringCtor(string version, int major, int minor, int revision, int patch)
    {
        var parsed = new ClientVersion(version);
        var built = new ClientVersion(major, minor, revision, patch);

        Assert.Equal(major, parsed.Major);
        Assert.Equal(minor, parsed.Minor);
        Assert.Equal(revision, parsed.Revision);
        Assert.Equal(patch, parsed.Patch);
        Assert.Equal(version.Split(' ')[0], built.ToString());
    }

    [Theory]
    [InlineData("4.0.11b", "4.0.11c", -1)]
    [InlineData("4.0.11c", "4.0.11", 1)]
    [InlineData("4.0.11c", "5.0.0a", -1)]
    [InlineData("5.0.2b", "5.0.2a", 1)]
    [InlineData("5.0.6e", "5.0.7.0", -1)]
    public void TestLetterPatchOrdering(string a, string b, int comparison)
    {
        Assert.Equal(comparison, new ClientVersion(a).CompareTo(new ClientVersion(b)));
    }

    [Theory]
    [InlineData("4.0.11c", 4, 0, 11, 3)]
    [InlineData("5.0.2b", 5, 0, 2, 2)]
    [InlineData("7.0.33.1", 7, 0, 33, 1)]
    public void TestStringAndIntCtorsAreEqual(string version, int major, int minor, int revision, int patch)
    {
        var parsed = new ClientVersion(version);
        var built = new ClientVersion(major, minor, revision, patch);

        Assert.Equal(ClientType.Classic, parsed.Type);
        Assert.Equal(built, parsed);
        Assert.Equal(built.GetHashCode(), parsed.GetHashCode());
    }

    [Theory]
    [InlineData("4.0.11c uotd", ClientType.UOTD)]
    [InlineData("66.55.38", ClientType.KR)]
    [InlineData("67.0.33", ClientType.SA)]
    public void TestStringCtorKeepsOtherTypes(string version, ClientType type)
    {
        Assert.Equal(type, new ClientVersion(version).Type);
    }
}
