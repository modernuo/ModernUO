using System;
using Server.Saves;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AutoSaveTests
{
    [Fact]
    public void ResetAutoSave_RejectsZeroDelay()
    {
        var delay = AutoSave.Delay;
        var warning = AutoSave.Warning;

        try
        {
            AutoSave.ResetAutoSave(TimeSpan.Zero, TimeSpan.FromSeconds(30));

            Assert.Equal(delay, AutoSave.Delay);
            Assert.Equal(warning, AutoSave.Warning);
        }
        finally
        {
            AutoSave.ResetAutoSave(delay, warning);
        }
    }
}
