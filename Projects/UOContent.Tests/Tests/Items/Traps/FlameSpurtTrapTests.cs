using System.Reflection;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class FlameSpurtTrapTests
{
    [Fact]
    public void Delete_StopsTheRepeatingTimer()
    {
        var trap = new FlameSpurtTrap();
        trap.StartTimer();

        var field = typeof(FlameSpurtTrap).GetField("_timerToken", BindingFlags.Instance | BindingFlags.NonPublic);
        bool Running() => ((TimerExecutionToken)field!.GetValue(trap)!).Running;

        Assert.True(Running());
        trap.Delete();
        Assert.False(Running());
    }
}
