using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// How often a bot reconsiders what it is short of.
///
/// <para>
/// <b>Once a minute, by Patrick's order of 05.09.2026, and the numbers it is against are in the shard's own
/// summary.</b> The <c>Needs:</c> line reads its counters as questions asked, and at 00:42 they stood at
/// 142,845 asked about a feather, a log or a hide; 35,022 asked about worn gear; 2,178 asked about metal —
/// over a population of fifty-three, in under two hours. <c>BotStores.Keep</c> runs on the bot's own beat,
/// five times a second per bot; the two proposers are asked every time the auction reviews. None of those
/// questions has an answer that can change in a fifth of a second: whether a fletcher is short of feathers
/// is a fact about a pack that a walk to a wood takes minutes to alter.
/// </para>
///
/// <para>
/// <b>A clock per bot <em>and per question</em>, not one clock for all of them.</b> A single stamp would have
/// the three starve each other — whichever asked first would spend the minute, and the other two would be
/// answered once every three. They are different questions about different halves of a trade and each is
/// entitled to its own minute.
/// </para>
///
/// <para>
/// This is a throttle on <em>asking</em> and never on doing. Nothing here refuses an order, cancels one or
/// changes what a bot decides — the answer a minute later is the same answer, arrived at once instead of
/// three hundred times. The cost of being a minute late to notice a shortage is a minute; the cost of asking
/// three hundred times a minute is the population's whole decision budget, which is what the summary's
/// denominators have been reporting all along without anybody reading them as a complaint.
/// </para>
/// </summary>
public static class BotNeeds
{
    /// <summary>How long between one bot's reviews of one of its needs.</summary>
    public static int EveryMs { get; set; } = 60000;

    /// <summary>The most stamps kept before the lapsed ones are swept. Two per bot per question is plenty.</summary>
    public static int MostStamps { get; set; } = 4096;

    /// <summary>Questions answered rather than passed over. For the summary.</summary>
    public static long Asked { get; private set; }

    /// <summary>Questions passed over because the same bot asked the same one inside the minute.</summary>
    public static long Passed { get; private set; }

    private static readonly Dictionary<(Serial Who, string What), long> _asked = new();

    /// <summary>
    /// Whether this bot may reconsider this need now, stamping it when it may.
    ///
    /// <para>
    /// Asked once and acted on: a caller that asks twice has spent its minute on the first of them. Stamps
    /// are compared by subtraction against a real tick, never against a nought default — see
    /// <c>BotStipend</c> for the host whose counter starts enormous and can wrap negative.
    /// </para>
    /// </summary>
    public static bool Due(Mobile body, string what)
    {
        if (body == null || string.IsNullOrEmpty(what))
        {
            return true;
        }

        var now = Core.TickCount;
        var key = (body.Serial, what);

        if (_asked.TryGetValue(key, out var last) && now - last < EveryMs)
        {
            Passed++;

            return false;
        }

        if (_asked.Count > MostStamps)
        {
            Sweep(now);
        }

        _asked[key] = now;
        Asked++;

        return true;
    }

    private static void Sweep(long now)
    {
        List<(Serial, string)> lapsed = [];

        foreach (var (key, last) in _asked)
        {
            if (now - last >= EveryMs)
            {
                lapsed.Add(key);
            }
        }

        for (var i = 0; i < lapsed.Count; i++)
        {
            _asked.Remove(lapsed[i]);
        }
    }

    public static string Describe() =>
        Asked + Passed == 0
            ? "nobody has looked at what they are short of yet"
            : $"needs reviewed {Asked} times, {Passed} asks inside the {EveryMs / 1000}s between one bot's reviews";

    /// <summary>Forgotten with the world, like every store in this assembly that is keyed by serial.</summary>
    public static void Forget()
    {
        _asked.Clear();
        Asked = 0;
        Passed = 0;
    }
}
