using Xunit;

namespace Server.Tests;

// Enums travel as ints: the routing types are internal, and a public test method cannot take them.
public class ShutdownRoutingTests
{
    [Theory]
    [InlineData((int)LoopLifecycle.NotStarted, 0, false)]
    [InlineData((int)LoopLifecycle.Running, 0, true)]
    [InlineData((int)LoopLifecycle.ShuttingDown, 0, true)]
    [InlineData((int)LoopLifecycle.Running, 1, false)]
    [InlineData((int)LoopLifecycle.ShuttingDown, 2, false)]
    public void OnlyTheFirstTerminateIsInterceptedOnceTheLoopRuns(int lifecycle, int priorSignals, bool expected) =>
        Assert.Equal(expected, ShutdownRouting.ShouldInterceptTerminate((LoopLifecycle)lifecycle, priorSignals));

    [Theory]
    [InlineData((int)LoopLifecycle.NotStarted, (int)ProcessExitRoute.RunHere)]
    [InlineData((int)LoopLifecycle.Running, (int)ProcessExitRoute.WaitForLoop)]
    [InlineData((int)LoopLifecycle.ShuttingDown, (int)ProcessExitRoute.WaitForLoop)]
    public void ProcessExitWaitsForAStartedLoop(int lifecycle, int expected) =>
        Assert.Equal((ProcessExitRoute)expected, ShutdownRouting.RouteProcessExit((LoopLifecycle)lifecycle));

    [Theory]
    [InlineData(true, (int)LoopLifecycle.NotStarted, (int)CrashRoute.RunHere)]
    [InlineData(true, (int)LoopLifecycle.Running, (int)CrashRoute.RunHere)]
    [InlineData(true, (int)LoopLifecycle.ShuttingDown, (int)CrashRoute.RunHere)]
    [InlineData(false, (int)LoopLifecycle.NotStarted, (int)CrashRoute.OffLoop)]
    [InlineData(false, (int)LoopLifecycle.Running, (int)CrashRoute.HandOffToLoop)]
    [InlineData(false, (int)LoopLifecycle.ShuttingDown, (int)CrashRoute.OffLoop)]
    public void CrashIsHandedOffOnlyToARunningLoop(bool onLoopThread, int lifecycle, int expected) =>
        Assert.Equal((CrashRoute)expected, ShutdownRouting.RouteCrash(onLoopThread, (LoopLifecycle)lifecycle));
}
