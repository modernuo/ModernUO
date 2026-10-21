using System.Collections.Generic;

namespace Server.Engines.AIConversation;

/// <summary>
/// Bounded per-session message history. Oldest turns are dropped first, and
/// the head is re-aligned so the first message sent to the API is always a
/// user turn (the Messages API rejects histories that open with "assistant").
/// </summary>
public class ConversationHistory
{
    private readonly List<ChatTurn> _turns = new();
    private readonly int _maxTurns;

    public ConversationHistory(int maxTurns) => _maxTurns = maxTurns;

    public IReadOnlyList<ChatTurn> Turns => _turns;

    public int Count => _turns.Count;

    public void Add(ChatRole role, string text)
    {
        _turns.Add(new ChatTurn(role, text));
        Trim();
    }

    private void Trim()
    {
        if (_turns.Count > _maxTurns)
        {
            _turns.RemoveRange(0, _turns.Count - _maxTurns);
        }

        var firstUser = 0;

        while (firstUser < _turns.Count && _turns[firstUser].Role != ChatRole.User)
        {
            firstUser++;
        }

        if (firstUser > 0)
        {
            _turns.RemoveRange(0, firstUser);
        }
    }

    /// <summary>Snapshot for handing to a background request.</summary>
    public ChatTurn[] ToArray() => _turns.ToArray();
}
