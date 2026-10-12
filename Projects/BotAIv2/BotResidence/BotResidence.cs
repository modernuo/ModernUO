using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Where each bot lives: the town it was born in or chose, and what "home" means to it there.
///
/// <para>
/// <b>Patrick's order of 29.09.2026, point 5: "teach the bots to choose a town of residence, live there, hunt there, do
/// their routine — all the same things they do in Britain. The result: the bots spread over the world."</b> Until now the
/// population had one home, <c>BotPopulation.Where</c> by Britain's graveyard, and every per-bot "home" in the code asked
/// it: the walk home, the flight's last resort, the herb woods, the hunting darts, where a newborn appears and where the
/// dead rise. At the boot of 18:38 on 29.09.2026 sixty of sixty-three bots stood in four guilds, three of the four had no
/// seat ("The Crown has no seat, The Hammer has no seat, The Blade has no seat, The Needle at 2248,1204"), so everybody but
/// The Needle's fifteen lived, was born and rose within 350 tiles of one tile.
/// </para>
///
/// <para>
/// <b>One question, one answer: <c>BotPopulation.HomeOf</c>, which asks <see cref="Home"/>.</b> A member of a guild seated
/// abroad lives by its guild (<see cref="BotRelocate.Forced"/>) as it did before; everybody else has a town of its own,
/// chosen at birth or at first sight (<see cref="BotSettle.Choose"/>) and changed only by walking there
/// (<see cref="BotResettle"/>). In the population's home town "home" is exactly what it was — the guild's hall or seat,
/// else the population's home — so a bot that lives in Britain is the bot it was before this file existed. In any other
/// town home is the town's bank (<see cref="BotSettle.Hearth"/>), or the guild's own hall or seat when that stands in the
/// same town.
/// </para>
///
/// <para>
/// <b>Filed by name, as what a bot has learned is filed (<c>BotProgress</c>), and kept across restarts
/// (<see cref="BotResidenceStore"/>)</b>: a bot that lives in Trinsic still lives there after a deploy, and a bot raised
/// under a name the store knows is born in that name's town, as it is handed that name's learning. The record is also held
/// on the body (<c>BotMobile.Residence</c>), so asking where a bot lives — which every sampler does per candidate through
/// the leash — costs a field read and nothing else.
/// </para>
/// </summary>
public static class BotResidence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotResidence));

    /// <summary>What one bot's residence is. Filed by the bot's name; the session half is never saved.</summary>
    public sealed class Record
    {
        /// <summary>The name it is filed under, which is the bot's.</summary>
        public string Name;

        /// <summary>The town it lives in, by the region's name (<see cref="BotTowns.Town.Name"/>).</summary>
        public string Town;

        /// <summary>When it settled there. The clock <see cref="BotRelocate.MoveEveryHours"/> runs from, across restarts.</summary>
        public DateTime Since;

        /// <summary>Times it has moved house.</summary>
        public int Moves;

        /// <summary>The town it last moved away from, which the next move does not go back to.</summary>
        public string Left;

        // ---- the session's half: never saved ----

        internal BotTowns.Town Resolved;

        /// <summary>Where it has been told it may move to, and why; null while no move is on offer. See <see cref="BotMover"/>.</summary>
        internal BotTowns.Town Pending;

        internal string PendingWhy;

        internal BotRelocate.Cause PendingCause;

        internal long PendingTick;

        /// <summary>The stretch it is being judged over: when it began, and what the bot was worth and had learned then.</summary>
        internal long WindowTick;

        internal int WindowWealth;

        internal double WindowProgress;

        /// <summary>The last judgement that found it in the world; a gap longer than two looks is a rest, and restarts the stretch.</summary>
        internal long SeenTick;

        /// <summary>Whether a judgement has seen it this session; until then the ticks above mean nothing.</summary>
        internal bool Watched;

        /// <summary>When it died where it lives, newest last. See <see cref="Fell"/>.</summary>
        internal readonly List<long> Deaths = [];

        internal bool HomeKnown;

        internal long HomeTick;

        internal Point3D HomeAt;
    }

    /// <summary>Whether bots have towns of their own. Off, and home is the guild's seat or the population's home, as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>Whether the module has started and residences are being answered. Until then home is what it always was.</summary>
    public static bool Active { get; private set; }

    /// <summary>
    /// How long a living bot's answer to "where is home" is kept before it is asked again: five seconds. The question is asked
    /// per candidate by every sampler through the leash (<c>BotPopulation.Within</c>), hundreds of times a beat, and the
    /// answer changes only when a hall is raised or a bot moves house, which clears it.
    /// </summary>
    public static int CacheMs { get; set; } = 5000;

    /// <summary>
    /// How far round a town's bank a bot born or risen there is put down. Forty: the town, not the bank's doorstep, which is
    /// what <c>BotPopulation.Spread</c> (ten) would make of it — forty-nine bots in a thirteen-tile square was the lesson of
    /// 07.09.2026 at home, and a town that fifteen bots are born into is the same square.
    /// </summary>
    public static int TownScatter { get; set; } = 40;

    /// <summary>Bots put down in a town of their own rather than at a guild's hall or seat or the population's home.</summary>
    public static long PlacedInTown { get; private set; }

    /// <summary>Newborns given a town at birth, and how many of those were not the home town.</summary>
    public static long Born { get; private set; }

    public static long BornAbroad { get; private set; }

    /// <summary>Records read back from the save, and records handed to a bot that carries the name.</summary>
    public static int ReadBack { get; private set; }

    public static long Adopted { get; private set; }

    /// <summary>Deaths within the bot's leash of where it lives. See <see cref="Fell"/>.</summary>
    public static long DiedAtHome { get; private set; }

    private static readonly Dictionary<string, Record> _byName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Bots born before the towns could be read, to be moved into their towns once they can. See <see cref="BotRelocate.MoveIn"/>.</summary>
    private static readonly List<(BotMobile Bot, long Tick)> _unhomed = [];

    internal static List<(BotMobile Bot, long Tick)> Unhomed => _unhomed;

    /// <summary>The banks of the towns somebody lives in, other than home's. See <see cref="Around"/>.</summary>
    private static readonly List<Point3D> _occupied = [];

    /// <summary>
    /// Where this bot lives: the one answer to every per-bot "home" on the shard. Asked through <c>BotPopulation.HomeOf</c>.
    ///
    /// <para>
    /// The guild's hall or seat (<c>BotSeat.Home</c>) when the bot lives in the population's home town, when its guild's
    /// hall or seat stands in the town it lives in, when it is of The Shadow (whose hideout is its home), or when it has no
    /// town yet; the town's bank otherwise. A dead bot is never answered from the cache, because a guild's outpost is home
    /// to the dead and not to the living (BotOutpost, 26.09.2026).
    /// </para>
    /// </summary>
    public static Point3D Home(BotMobile bot)
    {
        if (!Active || bot?.Residence is not { Town: not null } rec)
        {
            return BotSeat.Home(bot);
        }

        var now = Core.TickCount;
        var alive = bot.Alive;

        if (alive && rec.HomeKnown && now - rec.HomeTick < CacheMs)
        {
            return rec.HomeAt;
        }

        var seat = BotSeat.Home(bot);
        var town = TownOf(rec);
        Point3D at;

        if (town == null || town.Home || ReferenceEquals(town, BotSettle.HomeTown) || BotUnderworld.Member(bot))
        {
            at = seat;
        }
        else if (seat != BotPopulation.Where && BotTowns.Nearest(seat) == town)
        {
            at = seat;
        }
        else
        {
            at = BotSettle.Hearth(town);
        }

        if (alive)
        {
            rec.HomeAt = at;
            rec.HomeTick = now;
            rec.HomeKnown = true;
        }

        return at;
    }

    /// <summary>The town a bot lives in, or null when it has none yet or the towns have not been read.</summary>
    public static BotTowns.Town Of(BotMobile bot) => Active ? TownOf(bot?.Residence) : null;

    /// <summary>The name of the town a bot lives in, for the page; null when it has none.</summary>
    public static string NameOf(BotMobile bot) => Active ? bot?.Residence?.Town : null;

    internal static BotTowns.Town TownOf(Record rec)
    {
        if (rec?.Town == null || !BotTowns.Surveyed)
        {
            return null;
        }

        if (rec.Resolved == null || !string.Equals(rec.Resolved.Name, rec.Town, StringComparison.OrdinalIgnoreCase))
        {
            rec.Resolved = BotTowns.Find(rec.Town);
        }

        return rec.Resolved;
    }

    /// <summary>The record carried under this bot's name, handed to the body; null when the name has none.</summary>
    internal static Record Adopt(BotMobile bot)
    {
        if (bot == null)
        {
            return null;
        }

        if (bot.Residence != null)
        {
            return bot.Residence;
        }

        if (string.IsNullOrEmpty(bot.Name) || !_byName.TryGetValue(bot.Name, out var rec))
        {
            return null;
        }

        bot.Residence = rec;
        Adopted++;

        return rec;
    }

    /// <summary>This bot lives in this town from now on, as far as the record goes. Nothing is counted or said; the callers do that.</summary>
    internal static Record File(BotMobile bot, BotTowns.Town town)
    {
        var rec = Adopt(bot);

        if (rec == null)
        {
            rec = new Record { Name = bot.Name };
            bot.Residence = rec;

            if (!string.IsNullOrEmpty(bot.Name))
            {
                _byName[bot.Name] = rec;
            }
        }

        rec.Town = town?.Name;
        rec.Resolved = town;
        rec.Since = Core.Now;
        rec.HomeKnown = false;

        return rec;
    }

    /// <summary>
    /// A bot is being raised: a name that lived somewhere keeps its town, and a newcomer is given one before it is put down —
    /// so it is born there. Called from <c>BotPopulation.Raise</c> before the placement.
    ///
    /// <para>
    /// <b>The boot's own newborns are raised before the towns can be read</b> — the gates answer which town can be walked to
    /// a few seconds after the population exists, and the roads are drawn some ten seconds after that (18:38:52 raised,
    /// 18:38:58 towns, 18:39:07 roads, on 29.09.2026) — so those are put down at home as ever and moved into their towns the
    /// moment the towns are known (<see cref="BotRelocate.MoveIn"/>), before they have done anything.
    /// </para>
    /// </summary>
    public static void Raised(BotMobile bot)
    {
        if (bot == null)
        {
            return;
        }

        var rec = Adopt(bot);

        if (!Active || !BotSettle.Reckoned)
        {
            _unhomed.Add((bot, Core.TickCount));

            return;
        }

        if (rec?.Town != null && TownOf(rec) != null)
        {
            return;
        }

        var town = BotRelocate.Forced(bot) ?? BotSettle.Choose(bot, null, false, null, null, out _) ?? BotSettle.HomeTown;

        if (town == null)
        {
            return;
        }

        File(bot, town);
        Born++;

        if (!ReferenceEquals(town, BotSettle.HomeTown))
        {
            BornAbroad++;
        }

        logger.Information(
            "{Name} the {Class} is born in {Town}, where {Residents} others of the population live",
            bot.Name,
            bot.Class?.Name,
            town.Name,
            Residents(town)
        );
    }

    /// <summary>
    /// A bot died. Counted towards "died repeatedly where it lives" when it fell within its own leash of home
    /// (<c>BotPopulation.Leash</c>): a death in a cave four thousand tiles away is the cave's, not the town's. Called from
    /// <c>BotMobile.OnDeath</c> with where it fell, taken before anything moves the ghost: the end of a delve carries its
    /// dead home, and a death in the Orc Caves read after that reads as a death in Britain (build 319).
    /// </summary>
    public static void Fell(BotMobile bot, Point3D at)
    {
        if (!Active || bot?.Residence is not { Town: not null } rec)
        {
            return;
        }

        if (!Utility.InRange(Home(bot), at, BotPopulation.Leash(bot)))
        {
            return;
        }

        rec.Deaths.Add(Core.TickCount);
        DiedAtHome++;
    }

    /// <summary>A bot was put down at its home; counted as the guild's when home was the guild's hall or seat. From <c>BotPopulation.TryPlace</c>.</summary>
    public static void Placed(BotMobile bot, Point3D at)
    {
        if (at == BotSeat.Home(bot))
        {
            BotSeat.Placed();

            return;
        }

        PlacedInTown++;
    }

    /// <summary>How far round <paramref name="at"/> a bot is put down: <see cref="TownScatter"/> round a town's bank, else the caller's own.</summary>
    public static int Span(Point3D at, int otherwise) =>
        Active && BotSettle.IsHearth(at) ? TownScatter

        // Round a guild house's heart, only as far as its walls: ten tiles either way is the next street and the shop over
        // it. See BotGuildHouses.Spread (29.09.2026, evening).
        : BotGuildHouses.IsHeart(at) ? BotGuildHouses.Spread
        : otherwise;

    /// <summary>
    /// Whether a place lies within <c>BotTowns.Roam</c> of a town somebody lives in. Part of the population's ground
    /// (<c>BotPopulation.Within</c>): the towns' own test answers only once the gates have, a few seconds after the boot, and a
    /// bot that came back from the save standing in its own town must not find that town outside the ground meanwhile.
    /// </summary>
    public static bool Around(Point3D where)
    {
        if (!Active)
        {
            return false;
        }

        for (var i = 0; i < _occupied.Count; i++)
        {
            if (Utility.InRange(_occupied[i], where, BotTowns.Roam))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a place lies within <paramref name="half"/> of a town somebody lives in: somewhere a killing field is never
    /// declared over, as the population's home and a guild's hall never are (<c>BotBarred.Homely</c>).
    /// </summary>
    public static bool Homely(Point3D where, int half)
    {
        if (!Active)
        {
            return false;
        }

        for (var i = 0; i < _occupied.Count; i++)
        {
            var at = _occupied[i];

            if (Math.Max(Math.Abs(at.X - where.X), Math.Abs(at.Y - where.Y)) <= half)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The count of bots in the world or resting that live in each town, a bot on its way to a new one counted there, and the
    /// same for the members of one guild. Returns the population counted. For the choice, which is rare.
    /// </summary>
    internal static int Tally(IReadOnlyList<BotTowns.Town> towns, Span<int> residents, Span<int> mates, Guild guild, out int guildSize)
    {
        var present = 0;

        guildSize = 0;
        residents.Clear();
        mates.Clear();

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            Count(bots[i], towns, residents, mates, guild, ref present, ref guildSize);
        }

        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            Count(away[i], towns, residents, mates, guild, ref present, ref guildSize);
        }

        return present;
    }

    private static void Count(
        BotMobile bot, IReadOnlyList<BotTowns.Town> towns, Span<int> residents, Span<int> mates, Guild guild, ref int present,
        ref int guildSize
    )
    {
        if (bot is not { Deleted: false })
        {
            return;
        }

        present++;

        var mine = guild != null && ReferenceEquals(bot.Guild, guild);

        if (mine)
        {
            guildSize++;
        }

        var rec = bot.Residence;
        var town = rec?.Pending ?? TownOf(rec);

        if (town == null)
        {
            return;
        }

        for (var i = 0; i < towns.Count; i++)
        {
            if (!ReferenceEquals(towns[i], town))
            {
                continue;
            }

            residents[i]++;

            if (mine)
            {
                mates[i]++;
            }

            return;
        }
    }

    /// <summary>How many bots in the world or resting live in this town now.</summary>
    public static int Residents(BotTowns.Town town)
    {
        if (!Active || town == null)
        {
            return 0;
        }

        var many = 0;
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && ReferenceEquals(TownOf(bot.Residence), town))
            {
                many++;
            }
        }

        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            if (away[i] is { Deleted: false } bot && ReferenceEquals(TownOf(bot.Residence), town))
            {
                many++;
            }
        }

        return many;
    }

    /// <summary>
    /// The banks of the towns lived in, for <see cref="Around"/> and <see cref="Homely"/>; and every living bot's record
    /// filed under the name it wears now, since a mind renames the bot it is given. After every judgement.
    /// </summary>
    internal static void Refresh()
    {
        _occupied.Clear();

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            Note(bots[i]);
        }

        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            Note(away[i]);
        }
    }

    private static void Note(BotMobile bot)
    {
        if (bot is not { Deleted: false, Residence: { } rec })
        {
            return;
        }

        if (!string.IsNullOrEmpty(bot.Name) && !string.Equals(rec.Name, bot.Name, StringComparison.OrdinalIgnoreCase))
        {
            if (rec.Name != null && _byName.TryGetValue(rec.Name, out var old) && ReferenceEquals(old, rec))
            {
                _byName.Remove(rec.Name);
            }

            rec.Name = bot.Name;
            _byName[bot.Name] = rec;
        }

        var town = TownOf(rec);

        if (town == null || town.Home)
        {
            return;
        }

        var hearth = BotSettle.Hearth(town);

        if (!_occupied.Contains(hearth))
        {
            _occupied.Add(hearth);
        }
    }

    /// <summary>The module has started: bots already in the world are handed the records their names carry.</summary>
    internal static void Start()
    {
        Active = Running;

        if (!Active)
        {
            _unhomed.Clear();

            return;
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            Adopt(bots[i]);
        }

        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            Adopt(away[i]);
        }

        Refresh();
    }

    /// <summary>The summary's line: who lives where, and what moved.</summary>
    public static string Describe()
    {
        if (!Active)
        {
            return "every bot lives where its guild or the population does";
        }

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var none = 0;

        Gather(BotPopulation.Bots, counts, ref none);
        Gather(BotPopulation.Away, counts, ref none);

        var sorted = new List<KeyValuePair<string, int>>(counts);
        sorted.Sort((a, b) => b.Value != a.Value ? b.Value.CompareTo(a.Value) : string.CompareOrdinal(a.Key, b.Key));

        using var line = Server.Text.ValueStringBuilder.Create(512);

        for (var i = 0; i < sorted.Count; i++)
        {
            line.Append(i > 0 ? ", " : "");
            line.Append($"{sorted[i].Key} {sorted[i].Value}");
        }

        if (none > 0)
        {
            line.Append(sorted.Count > 0 ? ", " : "");
            line.Append($"{none} with no town yet");
        }

        line.Append(
            $"; {Born} born with a town ({BornAbroad} away from home), {PlacedInTown} put down in a town of their own, {Adopted} records carried back to their names ({ReadBack} read off the save); {DiedAtHome} deaths where the bot lives; "
        );
        line.Append(BotRelocate.Describe());
        line.Append("; ");
        line.Append(BotMover.Describe());
        line.Append("; ");
        line.Append(BotSettle.Describe());

        return line.ToString();
    }

    private static void Gather(IReadOnlyList<BotMobile> bots, Dictionary<string, int> counts, ref int none)
    {
        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is not { Deleted: false } bot)
            {
                continue;
            }

            var town = TownOf(bot.Residence);

            if (town == null)
            {
                none++;

                continue;
            }

            counts[town.Name] = counts.GetValueOrDefault(town.Name) + 1;
        }
    }

    /// <summary>A world reload: the session's half forgotten, the records kept (the store is read again with the world).</summary>
    public static void Forget()
    {
        Active = false;
        PlacedInTown = 0;
        Born = 0;
        BornAbroad = 0;
        Adopted = 0;
        DiedAtHome = 0;
        _unhomed.Clear();
        _occupied.Clear();

        foreach (var (_, rec) in _byName)
        {
            rec.Resolved = null;
            rec.Pending = null;
            rec.HomeKnown = false;
            rec.Watched = false;
            rec.Deaths.Clear();
        }
    }

    /// <summary>Where everybody lives, forgotten: Patrick's reset. The next save writes the store empty. Returns how many were kept.</summary>
    public static int Wipe()
    {
        var gone = _byName.Count;

        _byName.Clear();

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] != null)
            {
                bots[i].Residence = null;
            }
        }

        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            if (away[i] != null)
            {
                away[i].Residence = null;
            }
        }

        _unhomed.Clear();
        _occupied.Clear();

        return gone;
    }

    // ---- the store: name, town, since, moves, the town left ----

    /// <summary>Read-only: a rename since the last look (<see cref="Refresh"/>, once a minute) is filed at the next.</summary>
    internal static void Save(IGenericWriter writer)
    {
        var many = 0;

        foreach (var (_, rec) in _byName)
        {
            if (rec.Town != null)
            {
                many++;
            }
        }

        writer.WriteEncodedInt(many);

        foreach (var (name, rec) in _byName)
        {
            if (rec.Town == null)
            {
                continue;
            }

            writer.Write(name);
            writer.Write(rec.Town);
            writer.Write(rec.Since);
            writer.WriteEncodedInt(rec.Moves);
            writer.Write(rec.Left);
        }
    }

    internal static int Load(IGenericReader reader)
    {
        _byName.Clear();

        var many = reader.ReadEncodedInt();

        for (var i = 0; i < many; i++)
        {
            var name = reader.ReadString();
            var town = reader.ReadString();
            var since = reader.ReadDateTime();
            var moves = reader.ReadEncodedInt();
            var left = reader.ReadString();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(town))
            {
                continue;
            }

            _byName[name] = new Record { Name = name, Town = town, Since = since, Moves = moves, Left = left };
        }

        ReadBack = _byName.Count;

        return ReadBack;
    }
}
