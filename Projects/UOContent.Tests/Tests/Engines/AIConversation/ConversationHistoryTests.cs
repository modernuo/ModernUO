using Server.Engines.AIConversation;
using Xunit;

namespace Server.Tests.Engines.AIConversation;

public class ConversationHistoryTests
{
    [Fact]
    public void Add_KeepsTurnsInOrder()
    {
        var history = new ConversationHistory(20);

        history.Add(ChatRole.User, "hello");
        history.Add(ChatRole.Assistant, "well met");

        Assert.Equal(2, history.Count);
        Assert.Equal(new ChatTurn(ChatRole.User, "hello"), history.Turns[0]);
        Assert.Equal(new ChatTurn(ChatRole.Assistant, "well met"), history.Turns[1]);
    }

    [Fact]
    public void Add_DropsOldestTurnsBeyondLimit()
    {
        var history = new ConversationHistory(4);

        for (var i = 0; i < 10; i++)
        {
            history.Add(ChatRole.User, $"question {i}");
            history.Add(ChatRole.Assistant, $"answer {i}");
        }

        Assert.Equal(4, history.Count);
        Assert.Equal("question 8", history.Turns[0].Text);
        Assert.Equal("answer 9", history.Turns[^1].Text);
    }

    [Fact]
    public void Trim_EnsuresFirstTurnIsUser()
    {
        var history = new ConversationHistory(3);

        history.Add(ChatRole.User, "q1");
        history.Add(ChatRole.Assistant, "a1");
        history.Add(ChatRole.User, "q2");
        history.Add(ChatRole.Assistant, "a2");

        // Capacity 3 would leave [a1, q2, a2]; the head realigns to q2.
        Assert.Equal(2, history.Count);
        Assert.Equal(ChatRole.User, history.Turns[0].Role);
        Assert.Equal("q2", history.Turns[0].Text);
    }

    [Fact]
    public void ToArray_ReturnsIndependentSnapshot()
    {
        var history = new ConversationHistory(10);
        history.Add(ChatRole.User, "hello");

        var snapshot = history.ToArray();
        history.Add(ChatRole.Assistant, "well met");

        Assert.Single(snapshot);
        Assert.Equal(2, history.Count);
    }
}
