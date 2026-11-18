using System;
using System.Reflection;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class StatModTests
{
    [Fact]
    public void AfterDeserialization_RestartsTimerWithRemainingDuration()
    {
        var mod = new StatMod(StatType.Str, "test", 10, TimeSpan.FromMinutes(10));
        mod.Remove();

        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(StatMod).GetField("_added", flags)!.SetValue(mod, Core.Now - TimeSpan.FromMinutes(9));
        typeof(StatMod).GetMethod("AfterDeserialization", flags)!.Invoke(mod, null);
        var token = (TimerExecutionToken)typeof(StatMod).GetField("_timerToken", flags)!.GetValue(mod)!;

        Assert.True(token.Next - Core.Now < TimeSpan.FromMinutes(2));
        token.Cancel();
    }
}
