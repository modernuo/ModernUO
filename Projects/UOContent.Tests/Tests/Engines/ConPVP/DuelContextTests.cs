using System.Collections.Generic;
using Server;
using Server.Engines.ConPVP;
using Xunit;

namespace Server.Engines.ConPVP.Tests;

[Collection("Sequential UOContent Tests")]
public class DuelContextTests
{
    [Fact]
    public void TestCountdownReachesZero()
    {
        Timer.Init(0);

        var initiator = new Mobile((Serial)0x1024u);
        initiator.DefaultMobileInit();

        try
        {
            var context = new DuelContext(initiator, RulesetLayout.Root);
            var counts = new List<int>();

            context.StartCountdown(3, counts.Add);

            for (var tick = 8L; tick <= 5000; tick += 8)
            {
                Timer.Slice(tick);

                if (tick == 896)
                {
                    // One step per second: nothing but the immediate first step before the first second.
                    Assert.Equal(new[] { 3 }, counts);
                }
            }

            Assert.Equal(new[] { 3, 2, 1, 0 }, counts);
        }
        finally
        {
            initiator.Delete();
            Timer.Init(0);
        }
    }
}
