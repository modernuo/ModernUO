using System;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Who should move house, and why: first sight, a guild seated abroad, a poor living, or dying where it lives.
///
/// <para>
/// <b>Once a minute, over the whole population, and nothing here walks or searches.</b> It reads a record, a purse, a
/// share of a trade and a list of deaths per bot — a hundred bots is a few hundred field reads — and at most it marks a
/// bot with a town it may move to. The move itself is work the bot takes when it has nothing better (<see cref="BotMover"/>,
/// <see cref="BotResettle"/>), and the residence changes when the bot takes it, never here: an offer is not an errand
/// (DECISIONS C9).
/// </para>
///
/// <list type="bullet">
/// <item><b>First sight.</b> A bot with no record — every bot on the first boot of this build — is filed where home was
/// until now (its guild's seat, else the population's home) and then chooses among the fit towns it can walk to
/// (<see cref="BotSettle.Choose"/>); a choice other than where it lives is marked as a move. This is the choice Patrick's
/// order is about, made once per bot.</item>
/// <item><b>A guild seated abroad.</b> Its members live by it, at once, as they always did (<c>BotSeat.Home</c>): the
/// guild chose, and the walk home or the next rising takes them there.</item>
/// <item><b>A poor living.</b> A bot judged over <see cref="StretchHours"/> in one town that neither earned
/// <see cref="PoorGold"/> nor learned <see cref="PoorProgress"/> of its trade there.</item>
/// <item><b>Dying where it lives.</b> <see cref="DeathsToMove"/> deaths within <see cref="DeathHours"/> inside its own
/// leash of home.</item>
/// </list>
///
/// <para>
/// <b>A reason to move is a chance, not an order</b> (<see cref="MoveOdds"/>), and never within
/// <see cref="MoveEveryHours"/> of the last settling: a bad hour in one town must not empty it into the next, which is the
/// herding DECISIONS C11 records four times.
/// </para>
/// </summary>
public static class BotRelocate
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRelocate));

    /// <summary>Why a move was marked.</summary>
    public enum Cause
    {
        Settling,
        Poor,
        Deaths
    }

    /// <summary>How often the population is looked over. A minute: every rule here is about hours.</summary>
    public static int LookEveryMs { get; set; } = 60000;

    /// <summary>The stretch a bot's living is judged over, in hours. Two: a novice's trade moves a hundredth in less, and a grown bot's purse shows a day's work.</summary>
    public static double StretchHours { get; set; } = 2.0;

    /// <summary>
    /// Gold a bot must gain over the stretch — pack, bank and what it has on the board (<c>BotYield.Standing</c>) — not to be
    /// earning poorly. A hundred and fifty: the middling purse of the population read 66gp at 18:58 on 29.09.2026 and 145gp
    /// twenty minutes before, so a bot that put less than that by in two hours is living below the middle.
    /// </summary>
    public static int PoorGold { get; set; } = 150;

    /// <summary>
    /// The share of its trade (<c>BotMobile.Progress</c>) a bot must gain over the stretch not to be earning poorly, whatever
    /// its purse did. A hundredth: a novice learning its trade is doing well by it wherever it lives, and a grown bot, whose
    /// share no longer moves, is judged by its purse alone.
    /// </summary>
    public static double PoorProgress { get; set; } = 0.01;

    /// <summary>Deaths within the bot's leash of home that make it think of moving. Three — the count a killing field is declared at (BotBarred.KillingDeaths).</summary>
    public static int DeathsToMove { get; set; } = 3;

    /// <summary>How far back those deaths are counted, in hours. Two.</summary>
    public static double DeathHours { get; set; } = 2.0;

    /// <summary>The least time between settling in a town and moving on, in hours. Four: "at most once per few hours", Patrick's order.</summary>
    public static double MoveEveryHours { get; set; } = 4.0;

    /// <summary>The chance a bot with a reason to move is marked to move at this look. A half: see the class.</summary>
    public static double MoveOdds { get; set; } = 0.5;

    /// <summary>How long a marked move stays on offer before it is let go, in minutes. An hour: a bot that has had something better to do for an hour is not moving.</summary>
    public static double PendingMinutes { get; set; } = 60.0;

    /// <summary>How old a newborn of the boot may be and still be moved into its town when the towns are read, in minutes. Ten.</summary>
    public static double NewbornMinutes { get; set; } = 10.0;

    public static long Filed { get; private set; }

    public static long FirstChoices { get; private set; }

    /// <summary>First choices of the town the bot was already standing in, filed there without a walk.</summary>
    public static long StoodIn { get; private set; }

    public static long Judged { get; private set; }

    public static long Poor { get; private set; }

    public static long Deadly { get; private set; }

    /// <summary>Bots found living in a town not fit to live in, at the end of a stretch, and asked to choose again.</summary>
    public static long Misplaced { get; private set; }

    /// <summary>Bots with a reason to move that the draw kept where they are. See <see cref="MoveOdds"/>.</summary>
    public static long Stayed { get; private set; }

    /// <summary>Bots with a reason to move and no town to go to: every road too strong for them, or nowhere fit.</summary>
    public static long Nowhere { get; private set; }

    /// <summary>Choices, first or later, in which at least one town was passed over for a road too strong for the bot.</summary>
    public static long Outmatched { get; private set; }

    public static long Marked { get; private set; }

    public static long MarkedSettling { get; private set; }

    public static long MarkedPoor { get; private set; }

    public static long MarkedDeaths { get; private set; }

    /// <summary>Marked moves let go after <see cref="PendingMinutes"/> untaken, or because the town was barred meanwhile.</summary>
    public static long Expired { get; private set; }

    /// <summary>Bots that moved to their guild's seat abroad, or home for a class that stays home, at once.</summary>
    public static long Followed { get; private set; }

    /// <summary>Newborns of the boot moved into their towns once the towns were read, and those found too late or gone.</summary>
    public static long MovedIn { get; private set; }

    public static long Late { get; private set; }

    private static long _lookedTick;

    private static bool _said;

    /// <summary>The module's clock: the towns judged once, the boot's newborns moved in once, and the population looked over every minute.</summary>
    internal static void Tick()
    {
        var map = BotPopulation.Home;

        if (!BotResidence.Active || map == null || map == Map.Internal)
        {
            return;
        }

        if (!BotSettle.Reckoned)
        {
            if (!BotSettle.Reckon(map))
            {
                return;
            }

            MoveIn();
            _lookedTick = Core.TickCount - LookEveryMs;
        }

        var now = Core.TickCount;

        if (now - _lookedTick < LookEveryMs)
        {
            return;
        }

        _lookedTick = now;

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && bot.Map == map)
            {
                Judge(bot, now);
            }
        }

        BotResidence.Refresh();
    }

    /// <summary>
    /// The classes whose work is the whole population's and is done at home, so they live in the home town whatever the
    /// choice would say. The Baron — his stroll is "the town at home" (BotStroll, 16.09.2026) and his harrow is mustered from
    /// home — and the Captain, whose drill field is fixed in Britain (<c>BotSchool.Ground</c>, 1479,1629). And the Trader
    /// (01.10.2026, build 385): Britain is the market (Patrick, 30.09: "all trade lives in Britain"), its shop opens by Britain's
    /// bank, and its hauls go out from there to every other town; a trader settled in Yew would keep its shop in Yew.
    /// </summary>
    public static string[] StayHome { get; set; } = ["Baron", "Captain", "Trader"];

    /// <summary>Whether this bot's class lives at home. See <see cref="StayHome"/>.</summary>
    public static bool Pinned(BotMobile bot)
    {
        var name = bot?.Class?.Name;

        if (name == null || StayHome == null)
        {
            return false;
        }

        for (var i = 0; i < StayHome.Length; i++)
        {
            if (string.Equals(StayHome[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The town a bot must live in, or null when it chooses: the home town for a class that stays home
    /// (<see cref="StayHome"/>); else the town its guild is seated by, when that is not the home town. Null for The Shadow.
    /// </summary>
    public static BotTowns.Town Forced(BotMobile bot)
    {
        if (bot == null || BotUnderworld.Member(bot))
        {
            return null;
        }

        if (Pinned(bot))
        {
            return BotSettle.HomeTown;
        }

        if (bot.Guild is not Guild guild)
        {
            return null;
        }

        // <b>A guild with a house lives in the house's town, Britain included</b> (29.09.2026, evening): a house bought in
        // Britain is the guild's home, so its members live there even if they had chosen another town, as the members of a
        // guild housed abroad live abroad. See BotGuildHouses.
        // Not a town that has put the guild out (merged as build 332): the exile outranks the house. See BotBurgh.
        if (BotGuildHouses.TownOf(guild) is { } housed && !BotBurgh.Bars(guild, housed))
        {
            return housed;
        }

        var seat = BotSeat.Of(guild);

        if (seat == Point3D.Zero)
        {
            return null;
        }

        var town = BotTowns.Nearest(seat);

        // A town that has put the guild out is not forced on its members, even while the seat still stands there (29.09.2026):
        // they choose elsewhere until the guild is seated by another town. See BotBurgh.
        return town == null || ReferenceEquals(town, BotSettle.HomeTown) || BotBurgh.Bars(guild, town) ? null : town;
    }

    private static void Judge(BotMobile bot, long now)
    {
        var rec = BotResidence.Adopt(bot);
        var forced = Forced(bot);

        if (rec == null || BotResidence.TownOf(rec) == null)
        {
            FirstSight(bot, forced, now);

            return;
        }

        var town = BotResidence.TownOf(rec);

        if (forced != null)
        {
            if (!ReferenceEquals(forced, town))
            {
                Follow(bot, rec, town, forced);
            }

            return;
        }

        // The Shadow lives at its hideout whatever its record says (BotResidence.Home), so it is never moved.
        if (BotUnderworld.Member(bot))
        {
            return;
        }

        // A gap of more than two looks is a rest out of the world or a stretch dead: the stretch starts again from here, so
        // hours spent resting are not read as hours of earning nothing.
        if (!rec.Watched || now - rec.SeenTick > 2L * LookEveryMs)
        {
            Restart(bot, rec, now);
            rec.Watched = true;
        }

        rec.SeenTick = now;

        if (rec.Pending != null)
        {
            Pending(bot, rec, now);

            return;
        }

        if (!bot.Alive || bot.Tired)
        {
            return;
        }

        var deathsMs = (long)(DeathHours * 3600000);

        for (var d = rec.Deaths.Count - 1; d >= 0; d--)
        {
            if (now - rec.Deaths[d] > deathsMs)
            {
                rec.Deaths.RemoveAt(d);
            }
        }

        var cause = Cause.Settling;
        string why = null;

        // A bot whose guild the town has put out moves now, without the draw or the clock, as from a town not fit to live in
        // (29.09.2026). See BotBurgh.
        if (bot.Guild is Guild guild && BotBurgh.Bars(guild, town))
        {
            why = $"{guild.Name} was put out of {town.Name}";
        }
        else if (rec.Deaths.Count >= Math.Max(1, DeathsToMove))
        {
            cause = Cause.Deaths;
            why = $"died {rec.Deaths.Count} times in {DeathHours:0.#}h where it lives";
        }
        else if (now - rec.WindowTick >= (long)(StretchHours * 3600000))
        {
            Judged++;

            var gold = BotYield.Standing(bot) - rec.WindowWealth;
            var learned = bot.Progress - rec.WindowProgress;
            var info = BotSettle.Of(town);

            // <b>A town nobody would choose is left, whatever the living there.</b> A bot that left The Needle still lives at
            // Cove, which has no bank; one filed at the boot in a town since barred from the list is the same.
            if (info is { Fit: false } && !ReferenceEquals(town, BotSettle.HomeTown))
            {
                cause = Cause.Settling;
                why = $"{town.Name} is not fit to live in ({info.Why})";
            }
            else if (gold < PoorGold && learned < PoorProgress)
            {
                cause = Cause.Poor;
                why = $"earned {gold}gp and {learned:P1} of its trade in {StretchHours:0.#}h there";
            }
            else
            {
                Restart(bot, rec, now);

                return;
            }
        }

        if (why == null)
        {
            return;
        }

        switch (cause)
        {
            case Cause.Deaths:
                {
                    Deadly++;

                    break;
                }
            case Cause.Poor:
                {
                    Poor++;

                    break;
                }
            default:
                {
                    Misplaced++;

                    break;
                }
        }

        // The draw and the clock are for a reason to move, not for a town that is no place to live.
        if (cause != Cause.Settling
            && ((Core.Now - rec.Since).TotalHours < MoveEveryHours || Utility.RandomDouble() >= MoveOdds))
        {
            Stayed++;
            Restart(bot, rec, now);

            return;
        }

        var from = BotTowns.Nearest(bot.Location);
        var to = BotSettle.Choose(bot, from, true, town, rec.Left, out var outmatched);

        if (outmatched > 0)
        {
            Outmatched++;
        }

        if (to == null || ReferenceEquals(to, town))
        {
            Nowhere++;
            Restart(bot, rec, now);

            return;
        }

        Mark(bot, rec, to, cause, why, now);
    }

    /// <summary>
    /// A bot with no record: filed where home was until now, then asked to choose — and a choice elsewhere is a move it may
    /// take when it has nothing better to do.
    /// </summary>
    private static void FirstSight(BotMobile bot, BotTowns.Town forced, long now)
    {
        var was = forced ?? BotTowns.Nearest(BotSeat.Home(bot)) ?? BotSettle.HomeTown;

        if (was == null)
        {
            return;
        }

        var rec = BotResidence.File(bot, was);

        Filed++;
        Restart(bot, rec, now);
        rec.SeenTick = now;
        rec.Watched = true;

        if (forced != null || BotUnderworld.Member(bot))
        {
            return;
        }

        var from = BotTowns.Nearest(bot.Location) ?? was;
        var to = BotSettle.Choose(bot, from, true, null, null, out var outmatched);

        FirstChoices++;

        if (outmatched > 0)
        {
            Outmatched++;
        }

        if (to == null || ReferenceEquals(to, was))
        {
            return;
        }

        // Chosen where it already stands — a traveller that was in Vesper at the boot: nothing to walk.
        if (ReferenceEquals(to, from) && to.Bounds.Contains(bot.Location))
        {
            BotResidence.File(bot, to);
            StoodIn++;

            return;
        }

        Mark(bot, rec, to, Cause.Settling, $"settling: chose {to.Name} over {was.Name}", now);
    }

    /// <summary>The guild lives by another town now: so does the member, at once.</summary>
    private static void Follow(BotMobile bot, BotResidence.Record rec, BotTowns.Town town, BotTowns.Town forced)
    {
        var left = town?.Name;

        rec.Pending = null;
        BotResidence.File(bot, forced);
        rec.Left = left;
        rec.Moves++;
        Followed++;
        Restart(bot, rec, Core.TickCount);

        var why = Pinned(bot) ? "its calling is done at home" : $"{(bot.Guild as Guild)?.Name} is seated there";

        logger.Information(
            "{Name} the {Class} lives in {Town} from now on: {Why}; it lived in {Left}",
            bot.Name,
            bot.Class?.Name,
            forced.Name,
            why,
            left ?? "no town"
        );

        BotEvents.Post("residence", bot, $"lives in {forced.Name}: {why}");
    }

    private static void Mark(BotMobile bot, BotResidence.Record rec, BotTowns.Town to, Cause cause, string why, long now)
    {
        rec.Pending = to;
        rec.PendingCause = cause;
        rec.PendingWhy = why;
        rec.PendingTick = now;
        Marked++;

        switch (cause)
        {
            case Cause.Settling:
                {
                    MarkedSettling++;

                    break;
                }
            case Cause.Poor:
                {
                    MarkedPoor++;

                    break;
                }
            default:
                {
                    MarkedDeaths++;

                    break;
                }
        }

        if (!_said)
        {
            _said = true;

            logger.Information("{Name} the {Class} is the first bot marked to move house: to {Town}, {Why}", bot.Name, bot.Class?.Name, to.Name, why);
        }
    }

    /// <summary>A marked move: let go when it has stood untaken too long or its town has been barred meanwhile.</summary>
    private static void Pending(BotMobile bot, BotResidence.Record rec, long now)
    {
        var stale = now - rec.PendingTick > (long)(PendingMinutes * 60000);
        var barred = BotBarred.Holds(bot.Map, BotSettle.Hearth(rec.Pending));

        if (!stale && !barred)
        {
            return;
        }

        rec.Pending = null;
        Expired++;
        Restart(bot, rec, now);
    }

    /// <summary>The stretch starts again: the purse and the trade as they stand, and no deaths counted.</summary>
    private static void Restart(BotMobile bot, BotResidence.Record rec, long now)
    {
        rec.WindowTick = now;
        rec.WindowWealth = BotYield.Standing(bot);
        rec.WindowProgress = bot.Progress;
        rec.Deaths.Clear();
    }

    /// <summary>
    /// The boot's newborns, put down at home before the towns could be read, moved into the towns they were born to — before
    /// they have done anything worth keeping. A newborn older than <see cref="NewbornMinutes"/>, dead, resting or in a
    /// company is left where it is and counted <see cref="Late"/>; the first look files it where it stands.
    /// </summary>
    internal static void MoveIn()
    {
        var unhomed = BotResidence.Unhomed;
        var now = Core.TickCount;

        for (var i = 0; i < unhomed.Count; i++)
        {
            var (bot, tick) = unhomed[i];

            if (bot is not { Deleted: false, Alive: true } || bot.Map != BotPopulation.Home || bot.Squad != null
                || now - tick > (long)(NewbornMinutes * 60000))
            {
                Late++;

                continue;
            }

            var rec = BotResidence.Adopt(bot);
            var town = BotResidence.TownOf(rec);

            if (town == null)
            {
                town = Forced(bot) ?? BotSettle.Choose(bot, null, false, null, null, out _) ?? BotSettle.HomeTown;

                if (town == null)
                {
                    continue;
                }

                BotResidence.File(bot, town);
            }

            var home = BotPopulation.HomeOf(bot);

            // Born to the home town, or already in its own: it was put down where it lives.
            if (ReferenceEquals(town, BotSettle.HomeTown) || town.Bounds.Contains(bot.Location)
                || Utility.InRange(bot.Location, home, BotHomeward.Away))
            {
                continue;
            }

            var from = bot.Location;

            BotWill.PutDown(bot, $"born to {town.Name}, and moved in");
            bot.Journey?.Discard();

            if (!BotPopulation.PlaceAtHome(bot))
            {
                continue;
            }

            MovedIn++;

            logger.Information(
                "{Name} the {Class}, born at the boot before the towns were read, is moved into {Town}: from {From} to {Where}",
                bot.Name,
                bot.Class?.Name,
                town.Name,
                from,
                bot.Location
            );
        }

        unhomed.Clear();
    }

    public static string Describe() =>
        $"{Filed} filed where they lived and {FirstChoices} asked to choose ({StoodIn} chose the town they stood in); {Judged} living judged over {StretchHours:0.#}h, {Poor} earning poorly (under {PoorGold}gp and {PoorProgress:P0} of the trade), {Deadly} dying where they live ({DeathsToMove} in {DeathHours:0.#}h), {Misplaced} living in a town not fit to live in, {Stayed} stayed (by the draw at {MoveOdds:P0} or within {MoveEveryHours:0.#}h of settling), {Nowhere} had nowhere to go; {Outmatched} choices passed a town over for a road too strong for the bot; {Marked} marked to move ({MarkedSettling} settling, {MarkedPoor} for a poor living, {MarkedDeaths} for deaths), {Expired} let go untaken; {Followed} followed their guild abroad or their calling home; {MovedIn} newborns of the boot moved into their towns, {Late} too late or gone";

    public static void Forget()
    {
        Filed = 0;
        FirstChoices = 0;
        StoodIn = 0;
        Judged = 0;
        Poor = 0;
        Deadly = 0;
        Misplaced = 0;
        Stayed = 0;
        Nowhere = 0;
        Outmatched = 0;
        Marked = 0;
        MarkedSettling = 0;
        MarkedPoor = 0;
        MarkedDeaths = 0;
        Expired = 0;
        Followed = 0;
        MovedIn = 0;
        Late = 0;
        _lookedTick = 0;
        _said = false;
    }
}
