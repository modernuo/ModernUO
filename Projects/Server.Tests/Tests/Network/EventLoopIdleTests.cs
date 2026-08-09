using Xunit;

namespace Server.Tests;

/// <summary>
/// The event loop only sleeps when every queue it drains is empty. These drains are deliberately
/// bounded -- ExecuteTasks stops at its per-frame cap -- so leftover work is normal and must keep
/// the loop awake. Getting this wrong strands queued work for the length of a sleep.
/// </summary>
[Collection("Sequential Server Tests")]
public class EventLoopIdleTests
{
    [Fact]
    public void FreshContextIsEmpty()
    {
        var context = new EventLoopContext();

        Assert.True(context.IsEmpty);
    }

    [Fact]
    public void PostedWorkMakesContextNonEmpty()
    {
        var context = new EventLoopContext();

        context.Post(() => { });

        Assert.False(context.IsEmpty);
    }

    [Fact]
    public void PriorityWorkMakesContextNonEmpty()
    {
        var context = new EventLoopContext();

        context.Post(() => { }, EventLoopContext.Priority.High);

        Assert.False(context.IsEmpty);
    }

    [Fact]
    public void ContextIsEmptyAgainOnceDrained()
    {
        var context = new EventLoopContext();
        context.Post(() => { });

        context.ExecuteTasks();

        Assert.True(context.IsEmpty);
    }

    [Fact]
    public void WorkBeyondThePerFrameCapKeepsContextNonEmpty()
    {
        // The cap is what makes IsEmpty necessary: a single ExecuteTasks pass cannot be assumed
        // to have drained everything, so the loop must not treat "I just ran tasks" as "idle".
        const int perFrameCap = 128;
        var context = new EventLoopContext(perFrameCap);

        for (var i = 0; i < perFrameCap + 10; i++)
        {
            context.Post(() => { });
        }

        context.ExecuteTasks();

        Assert.False(context.IsEmpty);
    }
}
