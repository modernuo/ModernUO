using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// Who a guild's leader may put out and who it may take in, and how often.
///
/// <para>
/// <b>Patrick's order of 11.09.2026.</b> A guild could lose members and had no way to gain one: bots were
/// dealt in at birth, walked out of a guild that was getting nowhere, and nothing on the shard could ever
/// offer any of them a place again. On the evening this was written that had run to its conclusion — all
/// five guilds standing at one member each, seventy bots outside them, and the hall that would have made
/// the guilds worth staying in unraisable because raising one takes a levy on members nobody had.
/// </para>
///
/// <para>
/// <b>The power is the leader's, not the code's, and that is the point rather than an implementation
/// detail.</b> The five makers are the shard's thinking bots; they already charge their bands with a trade
/// to gather at and a trade to make at, through <see cref="BotCharter"/>, and this is the same kind of
/// power over the same band. What this file does is the part a model must not be asked to do: keep the
/// clock, rank the candidates, and refuse the moves that would turn a roster into a revolving door.
/// </para>
///
/// <para>
/// <b>One change per guild per <see cref="EveryMs"/>, and it may be both halves of one thought.</b> A
/// leader that could hire on every beat would empty the pool in a minute and a guild that could expel on
/// every beat would be a queue rather than a band. Expelling one and taking one in is a single act of
/// judgement, so it costs one turn of the clock, not two.
/// </para>
///
/// <para>
/// <b>What "productive" is measured by, and what it is not.</b> This shard keeps no per-bot ledger of
/// earnings — the ledger it has is per-place and per-trade — so the honest answer to "who is worth taking"
/// is <see cref="BotMobile.Progress"/>: the share of what its own class set out to learn that it has
/// actually reached. It is the population's own answer to whether a bot is becoming something, it cannot be
/// gamed by a bot that happens to be standing on a pile of gold, and it is the number the dashboard already
/// shows. The purse breaks ties, because a bot that has kept some is a bot whose work has been going
/// somewhere. If a per-bot earnings ledger is ever kept, this method is the one place that has to change.
/// </para>
/// </summary>
public static class BotRoster
{
    /// <summary>Whether leaders may change their rosters at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// How long a leader waits between changes to its roster.
    ///
    /// Half an hour, by order. It is deliberately the same figure as <see cref="BotGuilds.Patience"/> —
    /// the time a member gives a new guild before it starts asking what it is getting — so that a guild
    /// filling itself and a guild bleeding members run on the same clock and the numbers can be read
    /// against each other.
    /// </summary>
    public static int EveryMs { get; set; } = 1800000;

    /// <summary>
    /// How many candidates a leader is shown.
    ///
    /// Five. A model handed seventy names picks one out of the first few whatever the rest say, so the list
    /// is cut here, by rank, rather than at the far end by the model's attention.
    /// </summary>
    public static int Shortlist { get; set; } = 5;

    /// <summary>
    /// How full a guild has to be before a leader stops being shown anybody at all.
    ///
    /// Nought, meaning never: a guild at <see cref="BotGuilds.Most"/> is refused by
    /// <see cref="BotGuilds.Recruit"/> anyway, and a leader that can see a better bot than one of its own
    /// should be able to expel and take in on the same turn.
    /// </summary>
    public static int StopAt { get; set; }

    /// <summary>When each guild last changed its roster.</summary>
    private static readonly Dictionary<string, long> _changed = [];

    /// <summary>Turns of the clock a leader has used.</summary>
    public static long Changes { get; private set; }

    /// <summary>Times a leader was asked for a change and the clock said no.</summary>
    public static long TooSoon { get; private set; }

    /// <summary>Times a leader named somebody who was not a candidate by the time it was asked for.</summary>
    public static long Gone { get; private set; }

    /// <summary>Whether this guild's leader may change the roster now.</summary>
    public static bool Free(Guild guild)
    {
        if (!Running || guild == null)
        {
            return false;
        }

        return !_changed.TryGetValue(guild.Name, out var when) || Core.TickCount - (when + EveryMs) >= 0;
    }

    /// <summary>How long until it may, in minutes, for the line a leader is shown.</summary>
    public static int Waits(Guild guild)
    {
        if (guild == null || !_changed.TryGetValue(guild.Name, out var when))
        {
            return 0;
        }

        var left = EveryMs - (int)(Core.TickCount - when);

        return left <= 0 ? 0 : left / 60000;
    }

    /// <summary>
    /// Everybody this guild could take, best first.
    ///
    /// <para>
    /// Walks the population rather than the world, which is the only list of bots this assembly keeps and
    /// is a few dozen entries long. Called when a leader is being written a state block — every couple of
    /// minutes, for five bots — and not on anybody's beat.
    /// </para>
    /// </summary>
    public static List<BotMobile> Candidates(Guild guild, int most)
    {
        List<BotMobile> found = [];

        if (!Running || guild == null || guild.Disbanded)
        {
            return found;
        }

        var roll = BotPopulation.Bots;

        for (var i = 0; i < roll.Count; i++)
        {
            if (roll[i] is not BotMobile { Deleted: false, Alive: true } bot)
            {
                continue;
            }

            if (bot.Guild != null || BotGuilds.Outside(bot) || BotGuilds.Spurned(bot, guild))
            {
                continue;
            }

            found.Add(bot);
        }

        found.Sort(static (a, b) => Promise(b).CompareTo(Promise(a)));

        if (most > 0 && found.Count > most)
        {
            found.RemoveRange(most, found.Count - most);
        }

        return found;
    }

    /// <summary>
    /// This guild's own members, worst first — which is the order a leader deciding who to put out wants
    /// them in, and the reverse of the one above on purpose so that both lists are read by the same number.
    /// </summary>
    public static List<BotMobile> Weakest(Guild guild, int most)
    {
        List<BotMobile> found = [];

        if (!Running || guild?.Members == null)
        {
            return found;
        }

        for (var i = 0; i < guild.Members.Count; i++)
        {
            // The head is left off rather than listed and refused: a leader shown a name it cannot act on
            // will eventually name it, and every such answer is a turn of the clock spent on nothing. It was
            // the maker until 21.09.2026 — see BotGuilds.Head.
            if (guild.Members[i] is BotMobile { Deleted: false } member && !BotGuilds.IsHead(member))
            {
                found.Add(member);
            }
        }

        found.Sort(static (a, b) => Promise(a).CompareTo(Promise(b)));

        if (most > 0 && found.Count > most)
        {
            found.RemoveRange(most, found.Count - most);
        }

        return found;
    }

    /// <summary>
    /// What one bot promises a guild, as one number. See the note at the top for what it is made of and
    /// what it deliberately is not.
    /// </summary>
    public static double Promise(BotMobile bot)
    {
        if (bot is not { Deleted: false })
        {
            return 0.0;
        }

        // Trade first and money second, at a hundredth of the weight: a full purse should separate two bots
        // of equal skill and should never outrank one that has learned its trade.
        return bot.Progress + Math.Min(1.0, (bot.Backpack?.GetAmount(typeof(Gold)) ?? 0) / 5000.0) * 0.01;
    }

    /// <summary>
    /// Puts the leader's judgement into effect: at most one out and at most one in, on one turn of the
    /// clock, and only if at least one of them actually happened.
    /// </summary>
    /// <returns>What was done, in words, or null if nothing was.</returns>
    public static string Change(Guild guild, BotMobile putOut, BotMobile takeIn, string why)
    {
        if (!Running || guild == null || (putOut == null && takeIn == null))
        {
            return null;
        }

        if (!Free(guild))
        {
            TooSoon++;

            return null;
        }

        // Asked again at the moment of acting, because the list the leader answered from was written when
        // it was asked and a model's answer arrives seconds later — by which time a candidate can have been
        // taken by another guild, or died.
        var put = putOut != null && ReferenceEquals(putOut.Guild, guild) && BotGuilds.Expel(guild, putOut, why);
        var took = takeIn is { Deleted: false, Alive: true, Guild: null } && BotGuilds.Recruit(guild, takeIn, why);

        if (!put && !took)
        {
            Gone++;

            return null;
        }

        _changed[guild.Name] = Core.TickCount;
        Changes++;

        if (put && took)
        {
            return $"put {putOut.Name} out and took {takeIn.Name} on";
        }

        return put ? $"put {putOut.Name} out" : $"took {takeIn.Name} on";
    }

    public static string Describe() =>
        !Running
            ? "no leader may change its roster"
            : $"{Changes} rosters changed by their leaders ({BotGuilds.Hired} taken on, {BotGuilds.Expelled} put out), "
            + $"one change per guild per {EveryMs / 60000} minutes: {TooSoon} asks came too soon, {Gone} named somebody "
            + $"who was gone by the time it was asked for, {BotGuilds.Crowded} found the guild full, "
            + $"{BotGuilds.HeldElsewhere} named somebody already in a guild, {BotGuilds.Spared} tried to put out their own maker";

    public static void Forget()
    {
        _changed.Clear();
        Changes = 0;
        TooSoon = 0;
        Gone = 0;
    }
}
