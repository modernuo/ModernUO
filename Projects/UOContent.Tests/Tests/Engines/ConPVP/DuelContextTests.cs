using System.Collections.Generic;
using Server.Engines.ConPVP;
using Server.Tests;
using Xunit;

namespace Server.Engines.ConPVP.Tests;

[Collection("Sequential UOContent Tests")]
public class DuelContextTests
{
    [Fact]
    public void TestCountdownReachesZero()
    {
        using var scope = new TimerWheelScope();

        var initiator = new Mobile(World.NewMobile);
        initiator.DefaultMobileInit();

        try
        {
            var context = new DuelContext(initiator, RulesetLayout.Root);
            List<int> counts = [];

            context.StartCountdown(3, counts.Add);

            TimerWheelScope.RunFor(900);
            Assert.Empty(counts);

            TimerWheelScope.RunFor(4100);
            Assert.Equal([3, 2, 1, 0], counts);
        }
        finally
        {
            initiator.Delete();
        }
    }
}
