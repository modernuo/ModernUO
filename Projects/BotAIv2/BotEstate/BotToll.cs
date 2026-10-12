using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>How a guild charges strangers for hunting its land.</summary>
public enum BotTollWay
{
    /// <summary>It does not: the band's own ground, or nobody to hold it.</summary>
    None,

    /// <summary>A share of every stranger's hunting coin on its land, taken whether or not anybody says so.</summary>
    Posted,

    /// <summary>A larger share, but only of a stranger one of its members has walked up to and told.</summary>
    Patrolled
}

/// <summary>
/// The toll on hunting a guild's land: which way each guild charges, who has been told, and what a stranger pays.
///
/// <para>
/// <b>Patrick's order of 26.09.2026, the first of the night:</b> "guilds may demand payment for hunting on their
/// territories — either by setting a percentage that is collected, or by actively patrolling, pointing out that the land
/// belongs to the guild and that the hunter's income will be taxed." Both are here, and a guild uses one or the other:
/// a guild with <see cref="PatrolFighters"/> fighters in the world patrols (<see cref="BotTollman"/> walks up to a stranger
/// hunting its land and says so, and a stranger told pays <see cref="PatrolRate"/>); a smaller guild posts its toll
/// (<see cref="PostedRate"/> of every stranger's hunting coin, told or not).
/// </para>
///
/// <para>
/// <b>Who pays.</b> A stranger: a bot of another guild or of none. Not the guild's own (they pay their dues into the same
/// chest at the old tithe, <see cref="BotChest.Rate"/>), not an ally, not an enemy — a guild at war settles with blood,
/// not coin — and not The Shadow, which pays nobody. Coin only, only coin still in the pack, only from work that braves
/// the ground (<see cref="BotChest.Tithe"/>), and a company's shares too (<c>BotSpoils</c>).
/// </para>
///
/// <para>
/// <b>What it does to the hunter is a price, not a wall.</b> The appraisal weighs hunting on tolled land at what is left
/// after the toll (<see cref="Factor"/>), outside the fifth root so it bites as a toll does, and never below
/// <c>1 − PatrolRate</c>. A hunter told once goes elsewhere if elsewhere pays better, and pays if it stays. Every toll paid
/// sours the payer's guild a little on the taker's and warms the taker's on the payer's (<see cref="Grudge"/>,
/// <see cref="Thanks"/>): a toll is how a quarrel over ground starts, and a quarrel is how the war the land costs starts.
/// </para>
/// </summary>
public static class BotToll
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotToll));

    /// <summary>Whether tolls are charged at all. Off, strangers pay nothing and are told nothing.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>A posting guild's share of a stranger's hunting coin. A tenth.</summary>
    public static double PostedRate { get; set; } = 0.10;

    /// <summary>A patrolling guild's share of a stranger's hunting coin, once the stranger has been told. A fifth.</summary>
    public static double PatrolRate { get; set; } = 0.20;

    /// <summary>How long a stranger stays told. An hour.</summary>
    public static int WarnedMs { get; set; } = 3600000;

    /// <summary>Fighters a guild needs in the world to patrol its land rather than post a toll.</summary>
    public static int PatrolFighters { get; set; } = 4;

    /// <summary>How often each guild's way is looked at, and the strangers on its land listed for its wardens.</summary>
    public static int LookMs { get; set; } = 10000;

    /// <summary>How often the ways are chosen again. Five minutes.</summary>
    public static int ChooseMs { get; set; } = 300000;

    /// <summary>How much the payer's guild sours on the taker's for every toll paid.</summary>
    public static double Grudge { get; set; } = 0.3;

    /// <summary>How much the taker's guild warms to the payer's for every toll paid.</summary>
    public static double Thanks { get; set; } = 0.2;

    /// <summary>Tolls paid, and what they came to, by way.</summary>
    public static long PostedPaid { get; private set; }

    public static long PostedGold { get; private set; }

    public static long PatrolPaid { get; private set; }

    public static long PatrolGold { get; private set; }

    /// <summary>Strangers told by a warden.</summary>
    public static long Told { get; private set; }

    /// <summary>Coin taken on another guild's land that owed nothing: an ally, an enemy, the band, or a stranger not told.</summary>
    public static long Exempt { get; private set; }

    public static long Untold { get; private set; }

    /// <summary>Times a guild changed the way it charges.</summary>
    public static long Changes { get; private set; }

    private static readonly Dictionary<string, BotTollWay> _ways = new(StringComparer.Ordinal);

    private static readonly Dictionary<(Serial Hunter, string Guild), long> _told = [];

    private static readonly List<(Serial Hunter, string Guild)> _lapsed = [];

    /// <summary>Strangers hunting each patrolling guild's land and not yet told, refreshed every <see cref="LookMs"/>.</summary>
    private static readonly Dictionary<string, List<BotMobile>> _strangers = new(StringComparer.Ordinal);

    private static long _lookedTick;

    private static long _choseTick;

    private static bool _looked;

    private static bool _chose;

    /// <summary>How this guild charges strangers. Posted until its way has been chosen.</summary>
    public static BotTollWay Way(string guild) =>
        guild == null ? BotTollWay.None : _ways.TryGetValue(guild, out var way) ? way : BotTollWay.Posted;

    /// <summary>Whether this hunter has been told by a warden of this guild within the hour.</summary>
    public static bool Warned(Mobile hunter, string guild) =>
        hunter != null && guild != null && _told.TryGetValue((hunter.Serial, guild), out var until) && Core.TickCount - until < 0;

    /// <summary>A warden has told this hunter. See <see cref="BotWard"/>.</summary>
    public static void Warn(Mobile hunter, string guild)
    {
        if (hunter == null || guild == null)
        {
            return;
        }

        _told[(hunter.Serial, guild)] = Core.TickCount + WarnedMs;
        Told++;

        if (_strangers.TryGetValue(guild, out var list))
        {
            list.Remove(hunter as BotMobile);
        }
    }

    /// <summary>
    /// The share of this payer's hunting coin the holder of the ground takes, or nought. Cheap enough for the appraisal:
    /// two dictionary lookups and the engine's ally list. The holder's own members are not asked here — their dues are
    /// <see cref="BotChest.Rate"/>, in the chest's own arithmetic.
    /// </summary>
    public static double Rate(string holder, Mobile payer)
    {
        if (!Running || holder == null || payer == null)
        {
            return 0.0;
        }

        // A neutral trader passes every guild's land toll-free (01.10.2026, build 385): a toll is the start of a quarrel between
        // guilds, and Patrick's traders take part in none. See BotTrading.Neutral.
        if (BotTrading.Neutral(payer))
        {
            return 0.0;
        }

        var theirs = payer.Guild?.Name;

        if (theirs == holder)
        {
            return 0.0;
        }

        if (theirs != null)
        {
            // An enemy settles with blood and a war's plunder, not a toll; an ally is a guest; the band pays nobody.
            if (BotWar.Of(holder, theirs) != null || BotUnderworld.Band(payer.Guild as Guild))
            {
                return 0.0;
            }

            if (BotGuilds.Named(holder) is { } held && BotGuilds.Named(theirs) is { } other && held.IsAlly(other))
            {
                return 0.0;
            }
        }

        return Way(holder) switch
        {
            BotTollWay.Posted => PostedRate,
            BotTollWay.Patrolled => Warned(payer, holder) ? PatrolRate : 0.0,
            _ => 0.0
        };
    }

    /// <summary>
    /// What hunting here is worth to this bot after the toll: one on open ground, one on its own land, and never less than
    /// <c>1 − PatrolRate</c>. For the appraisal, and only for work that braves the ground.
    /// </summary>
    public static double Factor(Mobile body, Map map, BotDeed deed)
    {
        if (!Running || deed == null || !deed.Braves || map == null || map == Map.Internal)
        {
            return 1.0;
        }

        var holder = BotLand.Holder(map, deed.Where);

        if (holder == null)
        {
            return 1.0;
        }

        return 1.0 - Math.Clamp(Rate(holder, body), 0.0, Math.Max(PostedRate, PatrolRate));
    }

    /// <summary>A toll was paid: counted, and the two guilds' opinions moved. Called from <see cref="BotChest.Tithe"/>.</summary>
    public static void Paid(string holder, Mobile payer, int gold)
    {
        if (Way(holder) == BotTollWay.Patrolled)
        {
            PatrolPaid++;
            PatrolGold += gold;
        }
        else
        {
            PostedPaid++;
            PostedGold += gold;
        }

        var theirs = payer?.Guild?.Name;

        if (theirs != null && theirs != holder)
        {
            BotRegard.Move(theirs, holder, -Grudge, "a toll paid on their land");
            BotRegard.Move(holder, theirs, Thanks, "a toll paid on our land");
        }
    }

    /// <summary>Coin taken on another guild's ground that owed nothing. Counted apart from the tolls, so a toll nobody pays is visible.</summary>
    public static void Owed(string holder, Mobile payer)
    {
        if (Way(holder) == BotTollWay.Patrolled && payer?.Guild?.Name != holder && Rate(holder, payer) <= 0.0
            && payer is not null && !Exempted(holder, payer))
        {
            Untold++;
        }
        else
        {
            Exempt++;
        }
    }

    private static bool Exempted(string holder, Mobile payer)
    {
        // Counted where the coin was taken toll-free, once a payment (01.10.2026, build 385). See BotTrading.Waive and TollWaived.
        if (BotTrading.Neutral(payer))
        {
            BotTrading.Waive();

            return true;
        }

        var theirs = payer.Guild?.Name;

        return theirs != null && (BotWar.Of(holder, theirs) != null || BotUnderworld.Band(payer.Guild as Guild)
            || BotGuilds.Named(holder) is { } held && BotGuilds.Named(theirs) is { } other && held.IsAlly(other));
    }

    /// <summary>The strangers a warden of this guild may be sent to, nearest first from where it stands. Empty unless the guild patrols.</summary>
    public static BotMobile Nearest(string guild, Mobile from)
    {
        if (guild == null || from == null || !_strangers.TryGetValue(guild, out var list) || list.Count == 0)
        {
            return null;
        }

        BotMobile best = null;
        var bestRange = double.MaxValue;

        for (var i = 0; i < list.Count; i++)
        {
            var one = list[i];

            // No warden is sent to a neutral trader (01.10.2026, build 385): it owes no toll. See BotTrading.Neutral.
            if (one is not { Deleted: false, Alive: true } || one.Map != from.Map || Warned(one, guild) || BotTrading.Neutral(one))
            {
                continue;
            }

            // <b>Still on the land now.</b> The list is made every LookMs, and a stranger who had walked off the land since was
            // offered, taken and let go on every beat, the ward ending "had left the land before it was told" the moment it
            // began: Talia of The Blade took and finished it five times in the one second at 13:40:24 on 26.09.2026.
            if (!string.Equals(BotLand.Holder(one.Map, one.Location), guild, System.StringComparison.Ordinal))
            {
                continue;
            }

            var range = from.GetDistanceToSqrt(one);

            if (range < bestRange)
            {
                bestRange = range;
                best = one;
            }
        }

        return best;
    }

    /// <summary>
    /// The clock: every <see cref="ChooseMs"/> each guild's way is chosen again from how many of it can walk its land, and
    /// every <see cref="LookMs"/> the strangers hunting each patrolling guild's land are listed for its wardens. From
    /// <c>BotBeat</c>, with its own throttle.
    /// </summary>
    public static void Look()
    {
        if (!Running)
        {
            return;
        }

        var now = Core.TickCount;

        if (!_chose || now - _choseTick >= ChooseMs)
        {
            _chose = true;
            _choseTick = now;
            Choose();
        }

        if (_looked && now - _lookedTick < LookMs)
        {
            return;
        }

        _looked = true;
        _lookedTick = now;

        foreach (var list in _strangers.Values)
        {
            list.Clear();
        }

        _lapsed.Clear();

        foreach (var (key, until) in _told)
        {
            if (now - until >= 0)
            {
                _lapsed.Add(key);
            }
        }

        for (var i = 0; i < _lapsed.Count; i++)
        {
            _told.Remove(_lapsed[i]);
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false, Alive: true } || bot.Map == null || bot.Map == Map.Internal)
            {
                continue;
            }

            // Hunting, by what it is doing: work that braves the ground. A stranger walking through is not a hunter.
            if (bot.Resolve?.Deed is not { Braves: true })
            {
                continue;
            }

            var holder = BotLand.Holder(bot.Map, bot.Location);

            if (holder == null || Way(holder) != BotTollWay.Patrolled || bot.Guild?.Name == holder || Warned(bot, holder))
            {
                continue;
            }

            if (bot.Guild != null && Exempted(holder, bot))
            {
                continue;
            }

            if (!_strangers.TryGetValue(holder, out var strangers))
            {
                _strangers[holder] = strangers = [];
            }

            strangers.Add(bot);
        }
    }

    /// <summary>Each guild's way, from how many of it can walk its land. The band is never asked.</summary>
    private static void Choose()
    {
        foreach (var guild in BotGuilds.Standing)
        {
            if (guild == null || guild.Disbanded)
            {
                continue;
            }

            var way = BotUnderworld.Band(guild) ? BotTollWay.None : Fighters(guild) >= PatrolFighters ? BotTollWay.Patrolled : BotTollWay.Posted;
            var had = Way(guild.Name);

            _ways[guild.Name] = way;

            if (had != way)
            {
                Changes++;

                logger.Information(
                    "{Guild} now {Way} for hunting its land",
                    guild.Name,
                    way switch
                    {
                        BotTollWay.Patrolled => $"patrols it and charges strangers it has told {PatrolRate:P0}",
                        BotTollWay.Posted => $"posts a toll of {PostedRate:P0} on strangers",
                        _ => "charges nothing"
                    }
                );
            }
        }
    }

    /// <summary>Members in the world who fight.</summary>
    private static int Fighters(Guild guild)
    {
        var count = 0;
        var members = guild.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i] is BotMobile { Deleted: false, Alive: true } bot && bot.Map != Map.Internal
                && bot.Class is { } klass && klass.Role != BotRole.Producer)
            {
                count++;
            }
        }

        return count;
    }

    public static string Describe()
    {
        if (!Running)
        {
            return "no tolls on hunting";
        }

        using var sb = ValueStringBuilder.Create(256);

        sb.Append($"tolls on hunting: {PostedPaid} posted tolls of {PostedGold}gp at {PostedRate:P0}, {PatrolPaid} of {PatrolGold}gp from strangers told at {PatrolRate:P0}, ");
        sb.Append($"{Told} strangers told by wardens, {Untold} hunts on patrolled land by strangers nobody had told, {Exempt} owing nothing (allies, enemies, the band); ways: ");

        var first = true;

        foreach (var (guild, way) in _ways)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            first = false;
            sb.Append($"{guild} {way}");
        }

        if (first)
        {
            sb.Append("not chosen yet");
        }

        return sb.ToString();
    }

    public static void Forget()
    {
        _ways.Clear();
        _told.Clear();
        _strangers.Clear();
        _chose = false;
        _looked = false;
        PostedPaid = 0;
        PostedGold = 0;
        PatrolPaid = 0;
        PatrolGold = 0;
        Told = 0;
        Exempt = 0;
        Untold = 0;
        Changes = 0;
    }
}
