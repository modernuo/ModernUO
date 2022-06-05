using Xunit;

namespace Server.Tests;

public class LocalizationEntryTests
{
    [Fact]
    public void TestClilocAsParameter()
    {
        Localization.Add("enu", 500002, "This tests ~1_NUMBER~ as parameters.");
        Localization.Add("enu", 500003, "clilocs");

        string numericFormatter = Localization.Format(500002, "enu", $"{500003:#}");
        string stringParam = Localization.Format(500002, "enu", $"{"#500003"}");

        Assert.Equal("This tests clilocs as parameters.", numericFormatter);
        Assert.Equal("This tests clilocs as parameters.", stringParam);
    }
}
