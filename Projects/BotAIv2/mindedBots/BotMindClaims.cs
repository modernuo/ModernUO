using System;
using System.Collections.Generic;

namespace Server.BotAI.Mind;

/// <summary>
/// Which mind has said it will fill which row of the board, so the others leave it alone.
///
/// <para>
/// <b>Patrick's order of 15.09.2026: the thinking crafters discuss the market between them and share out who takes
/// which order.</b> They could already hear each other (<see cref="BotMindTalk"/>) and see what each other held
/// (the fellows block), and still four of them read one board and went to one bench, because "I will make the
/// bustier arms" was never a fact anybody else could read — it was a sentence in a reason field. A claim is that
/// sentence made into a fact: the row's own label, the name that took it, and when.
/// </para>
///
/// <para>
/// <b>A claim is a note, not a lock.</b> Nothing on the shard refuses the work to anybody else — the crafts fill
/// the board in their own order and the auction weighs as it always did. What changes is what the other minds are
/// told: a row with a name against it is somebody's, and the prompt says so. It lapses on its own clock, because a
/// mind that has died or been switched off must not hold a row for ever.
/// </para>
/// </summary>
public static class BotMindClaims
{
    /// <summary>How long a claim stands without being made again. A quarter of an hour, the charter's own clock.</summary>
    public static int HoldsMs { get; set; } = 900000;

    private static readonly Dictionary<string, (string Who, long Tick)> _claims = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Claims made this session.</summary>
    public static long Made { get; private set; }

    /// <summary>Claims that named a row somebody else was already holding.</summary>
    public static long Contested { get; private set; }

    /// <summary>Puts this mind's name against the row. The row is the board's own label for it.</summary>
    public static bool Claim(string who, string row)
    {
        if (string.IsNullOrWhiteSpace(who) || string.IsNullOrWhiteSpace(row))
        {
            return false;
        }

        var (holder, _) = Holder(row);

        if (holder != null && !string.Equals(holder, who, StringComparison.OrdinalIgnoreCase))
        {
            Contested++;
        }

        _claims[row.Trim()] = (who, Core.TickCount);
        Made++;

        return true;
    }

    /// <summary>Who holds this row and how many seconds ago they said so, or null and nought.</summary>
    public static (string Who, int SecondsAgo) Holder(string row)
    {
        if (string.IsNullOrWhiteSpace(row) || !_claims.TryGetValue(row.Trim(), out var claim))
        {
            return (null, 0);
        }

        var since = Core.TickCount - claim.Tick;

        if (since >= HoldsMs)
        {
            _claims.Remove(row.Trim());

            return (null, 0);
        }

        return (claim.Who, (int)(since / 1000));
    }

    /// <summary>The row this mind holds, if any, and how long ago it took it.</summary>
    public static (string Row, int SecondsAgo) Of(string who)
    {
        foreach (var (row, claim) in _claims)
        {
            if (!string.Equals(claim.Who, who, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var since = Core.TickCount - claim.Tick;

            if (since < HoldsMs)
            {
                return (row, (int)(since / 1000));
            }
        }

        return (null, 0);
    }

    /// <summary>Rows held right now.</summary>
    public static int Standing()
    {
        var n = 0;

        foreach (var (_, claim) in _claims)
        {
            if (Core.TickCount - claim.Tick < HoldsMs)
            {
                n++;
            }
        }

        return n;
    }

    public static string Describe() =>
        $"{Made} orders on the board claimed by a mind ({Contested} over somebody else's claim), {Standing()} claims standing";

    public static void Forget()
    {
        _claims.Clear();
        Made = 0;
        Contested = 0;
    }
}
