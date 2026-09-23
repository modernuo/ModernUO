using System;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class DetectHiddenTimerTests
{
    private sealed class DetectingCreature : PetTestStub
    {
        public override bool PlayerRangeSensitive => false;
    }

    private sealed class DetectingAI : BaseAI
    {
        public int Detections { get; private set; }

        public DetectingAI(BaseCreature creature) : base(creature)
        {
        }

        public override bool Think() => true;

        public override void DetectHidden() => Detections++;
    }

    [SkippableTheory]
    [InlineData(0, 108000, 132000)]
    [InlineData(1, 108000, 132000)]
    [InlineData(100, 108000, 132000)]
    [InlineData(250, 108000, 132000)]
    [InlineData(251, 107100, 130900)]
    [InlineData(1000, 27000, 33000)]
    [InlineData(30000, 900, 1100)]
    public void DetectHidden_SchedulesBoundedCooldown_AndWaitsBeforeRetry(
        int intelligence, long minDelay, long maxDelay)
    {
        Skip.If(!Server.Tests.TestServerInitializer.TileDataLoaded, "Requires UO client map data.");
        Core._tickCount = 0;
        Timer.Init(0);
        var creature = new DetectingCreature();
        DetectingAI ai = null;

        try
        {
            creature.Int = Math.Max(1, intelligence);
            if (intelligence == 0)
            {
                creature.AddStatMod(new StatMod(StatType.Int, "DetectHiddenZeroInt", -1, TimeSpan.Zero));
            }

            Assert.Equal(intelligence, creature.Int);
            creature.Skills.DetectHidden.Base = 100;
            creature.MoveToWorld(new Point3D(1500, 1600, Map.Felucca.GetAverageZ(1500, 1600)), Map.Felucca);
            creature.AIObject.AITimer.Stop();
            ai = new DetectingAI(creature);
            ai.AITimer.Activate();

            AdvanceUntil(() => ai.Detections > 0, 1024);
            Assert.Equal(1, ai.Detections);
            Assert.InRange(ai._nextDetectHidden - Core.TickCount, minDelay, maxDelay);
            Assert.True(ai.AITimer.Running);

            var deadline = ai._nextDetectHidden;
            while (Core.TickCount + 8 < deadline)
            {
                Core._tickCount += 8;
                Timer.Slice(Core.TickCount);
            }

            Assert.Equal(1, ai.Detections);
            AdvanceUntil(() => ai.Detections > 1, 1024);
            Assert.Equal(2, ai.Detections);
            Assert.InRange(ai._nextDetectHidden - Core.TickCount, minDelay, maxDelay);
        }
        finally
        {
            ai?.AITimer.Stop();
            creature.Delete();
        }
    }

    private static void AdvanceUntil(Func<bool> condition, int milliseconds)
    {
        for (var elapsed = 0; !condition() && elapsed < milliseconds; elapsed += 8)
        {
            Core._tickCount += 8;
            Timer.Slice(Core.TickCount);
        }

        Assert.True(condition(), "The AI timer did not reach the expected detection.");
    }
}
