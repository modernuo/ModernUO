using System.Collections.Generic;
using System.Globalization;
using Server;
using Server.Mobiles;
using Server.Mobiles.AI.BaseAI;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class DebugMessageTimingTests
{
    public static IEnumerable<object[]> DeadlineCases()
    {
        (long Now, long Deadline, bool Due)[] clocks =
        [
            (99, 100, false), (100, 100, true), (101, 100, true),
            (-101, -100, false), (-100, -100, true), (-99, -100, true),
            (long.MaxValue - 10, long.MinValue + 10, false),
            (long.MinValue + 10, long.MaxValue - 10, true)
        ];

        foreach (var (now, deadline, due) in clocks)
        {
            foreach (var withProvider in new[] { false, true })
            {
                foreach (var debug in new[] { false, true })
                {
                    yield return [now, deadline, due, withProvider, debug];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(DeadlineCases))]
    public void Handler_EmitsOnlyWhenDebugEnabledAndDue(
        long now, long deadline, bool due, bool withProvider, bool debug)
    {
        var previousTick = Core.TickCount;
        var pet = new PetTestStub();
        var ai = pet.AIObject;
        pet.Debug = debug;
        ai.NextDebugMessage = deadline;
        Core._tickCount = now;
        var handler = withProvider
            ? new DebugInterpolatedStringHandler(6, 1, CultureInfo.InvariantCulture, ai)
            : new DebugInterpolatedStringHandler(6, 1, ai);

        try
        {
            handler.AppendLiteral("value ");
            handler.AppendFormatted(42);
            Assert.Equal(debug && due ? "value 42" : "", handler.Text.ToString());

            ai.DebugSayFormatted(ref handler, 50);
            Assert.Equal(debug && due ? unchecked(now + 50) : deadline, ai.NextDebugMessage);
            Assert.True(handler.Text.IsEmpty);
        }
        finally
        {
            handler.Clear();
            Core._tickCount = previousTick;
            pet.Delete();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Handler_RearmedCooldown_WaitsAcrossWrap(bool withProvider)
    {
        var previousTick = Core.TickCount;
        var pet = new PetTestStub { Debug = true };
        var ai = pet.AIObject;

        try
        {
            Core._tickCount = long.MaxValue - 10;
            ai.NextDebugMessage = Core.TickCount;
            Emit(ai, withProvider);
            var deadline = ai.NextDebugMessage;
            Assert.Equal(long.MinValue + 39, deadline);

            Core._tickCount = long.MaxValue - 9;
            Emit(ai, withProvider);
            Assert.Equal(deadline, ai.NextDebugMessage);

            Core._tickCount = deadline - 1;
            Emit(ai, withProvider);
            Assert.Equal(deadline, ai.NextDebugMessage);

            Core._tickCount = deadline;
            Emit(ai, withProvider);
            Assert.Equal(deadline + 50, ai.NextDebugMessage);
        }
        finally
        {
            Core._tickCount = previousTick;
            pet.Delete();
        }
    }

    [Fact]
    public void InterpolatedCall_UsesHandlerAcrossWrap()
    {
        var previousTick = Core.TickCount;
        var pet = new PetTestStub { Debug = true };
        var ai = pet.AIObject;

        try
        {
            Core._tickCount = long.MaxValue - 10;
            ai.NextDebugMessage = long.MinValue + 10;
            var value = 42;
            ai.DebugSayFormatted($"value {value}", 50);
            Assert.Equal(long.MinValue + 10, ai.NextDebugMessage);

            Core._tickCount = long.MinValue + 10;
            ai.DebugSayFormatted($"value {value}", 50);
            Assert.Equal(long.MinValue + 60, ai.NextDebugMessage);
        }
        finally
        {
            Core._tickCount = previousTick;
            pet.Delete();
        }
    }

    private static void Emit(BaseAI ai, bool withProvider)
    {
        var handler = withProvider
            ? new DebugInterpolatedStringHandler(5, 0, CultureInfo.InvariantCulture, ai)
            : new DebugInterpolatedStringHandler(5, 0, ai);
        try
        {
            handler.AppendLiteral("debug");
            ai.DebugSayFormatted(ref handler, 50);
        }
        finally
        {
            handler.Clear();
        }
    }
}
