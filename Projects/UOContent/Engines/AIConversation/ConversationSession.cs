using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.AIConversation;

/// <summary>
/// One player's active conversation with a persona NPC. Only ever touched on
/// the game thread.
/// </summary>
public class ConversationSession
{
    public ConversationSession(Mobile player, BaseCreature npc, int maxHistoryTurns)
    {
        Player = player;
        Npc = npc;
        History = new ConversationHistory(maxHistoryTurns);
        LastActivity = Core.Now;
    }

    public Mobile Player { get; }
    public BaseCreature Npc { get; }
    public ConversationHistory History { get; }

    /// <summary>Timestamps of recent API requests, for the per-minute cap.</summary>
    public Queue<DateTime> RecentRequests { get; } = new();

    public DateTime LastActivity { get; set; }
    public DateTime LastRequest { get; set; }

    /// <summary>An API request is in flight; further speech is ignored.</summary>
    public bool Busy { get; set; }

    /// <summary>Set when the session ends so late replies are dropped.</summary>
    public bool Ended { get; set; }
}
