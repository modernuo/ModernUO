using Server.Engines.Quests.Necro;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SummonedPaladinMoongateTests
{
    [Fact]
    public void MoongateIsNotSavedBecauseItsDeleteTimerIsNot()
    {
        var gate = new SummonedPaladinMoongate();

        try
        {
            Assert.True(gate.SkipSerialization);
        }
        finally
        {
            gate.Delete();
        }
    }
}
