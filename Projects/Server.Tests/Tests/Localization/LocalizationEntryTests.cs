using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
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

    [Fact]
    public void RemovingTheLastFallbackEntry_KeepsLaterAddsVisible()
    {
        Localization.Clear();

        try
        {
            Localization.Add("enu", 500004, "first");
            Assert.True(Localization.Remove("enu", 500004));

            Localization.Add("enu", 500005, "second");

            Assert.False(Localization.TryGetLocalization(500004, out _));
            Assert.True(Localization.TryGetLocalization(500005, out var entry));
            Assert.Equal("second", entry.Text);
        }
        finally
        {
            // Lets the next lookup reload the real tables from disk.
            Localization.Clear();
        }
    }
}
