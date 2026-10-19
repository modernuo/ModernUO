using System;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class LocalizationEntryTests : IDisposable
{
    private const int FormatNumber = 500002;
    private const int ParameterNumber = 500003;

    public void Dispose()
    {
        Localization.Remove("enu", FormatNumber);
        Localization.Remove("enu", ParameterNumber);
    }

    [Fact]
    public void TestClilocAsParameter()
    {
        Localization.Add("enu", FormatNumber, "This tests ~1_NUMBER~ as parameters.");
        Localization.Add("enu", ParameterNumber, "clilocs");

        string numericFormatter = Localization.Format(FormatNumber, "enu", $"{ParameterNumber:#}");
        string stringParam = Localization.Format(FormatNumber, "enu", $"{"#500003"}");

        Assert.Equal("This tests clilocs as parameters.", numericFormatter);
        Assert.Equal("This tests clilocs as parameters.", stringParam);
    }
}
