using Xunit;

namespace Server.Tests.Utility;

public class TryParseTests
{
    [Theory]
    [InlineData("True", null, true)]
    [InlineData("False", null, false)]
    [InlineData("Alakazam", "Not a valid boolean string.", true)]
    public void TestTryParseBool(string value, string returned, bool parsedAs)
    {
        var actualReturned = Server.Types.TryParse(typeof(bool), value, out var constructed);
        Assert.Equal(returned, actualReturned);

        if (returned == null)
        {
            Assert.Equal(parsedAs, constructed);
        }
    }
}
