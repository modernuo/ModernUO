using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The scout's work, in the order it is offered: a rumoured chest reached first, another guild's company shadowed and reported,
/// and a patrol of the dangerous ground by a plan.
///
/// <para>
/// <b>The rumour.</b> A chest Argus has spread word of (<see cref="BotRumour"/>) within its reach and not a lair: the scout is offered
/// the same seeking any bot is (<see cref="BotRumourSeek"/>, at most <see cref="BotRumour.MostSeekers"/> at once), reckoned
/// <see cref="RumourLean"/> times higher — it is the scout's trade to get there first. Counted apart, and who found the chest.
/// </para>
///
/// <para>
/// <b>The shadow.</b> Another guild's company of <see cref="ShadowLeast"/> or more within <see cref="ShadowReach"/> of the scout's
/// guild's seat is followed at <see cref="ShadowKeep"/> tiles for <see cref="ShadowMs"/>, then reported in the guild's channel and
/// as news ("The Wolf gathers 12 at Cove"). When the two guilds are at war and the company is within <see cref="WarnReach"/> of the
/// seat, the report is a warning: the company's leader becomes the threat the guild's war company musters against
/// (<see cref="BotFeud.Warn"/>), before it is on the guild's ground.
/// </para>
///
/// <para>
/// <b>The patrol.</b> The hostile zone (<see cref="BotZones"/>, never a deadly one, nor the sea) within <see cref="PatrolReach"/> of
/// the seat that scouts watched longest ago, then least awake, then nearest; the scout stands at a lookout outside its aggro range on
/// the side of home, and for <see cref="HoldMs"/> passes the zone's box to the scan every <see cref="FocusMs"/>
/// (<see cref="BotZoneScan.Focus"/>, the debuggers' tour's own feed), so the patrols the paths and the danger records read are fresh.
/// It counts what it saw and says it. A square of the death map in the zone that reads high while nothing attacking lives there now
/// is knocked down (<see cref="BotPeril.Scouted"/>), so work is not refused for a field that has gone quiet.
/// </para>
/// </summary>
public sealed class BotGuildScout : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGuildScout));

    public string Name => "Scout";

    public BotStanding Rung => BotStanding.Free;

    public static double Prior { get; set; } = 28.0;

    /// <summary>How long a scout rests between outings.</summary>
    public static int RestMs { get; set; } = 900000;

    public static bool Rumours { get; set; } = true;

    public static double RumourLean { get; set; } = 1.5;

    public static bool Shadows { get; set; } = true;

    public static int ShadowLeast { get; set; } = 4;

    public static int ShadowReach { get; set; } = 160;

    public static int ShadowKeep { get; set; } = 16;

    public static int ShadowMs { get; set; } = 90000;

    /// <summary>How near the seat an enemy company must be for the report to be a warning.</summary>
    public static int WarnReach { get; set; } = 90;

    /// <summary>How often one guild's scout reports the same company.</summary>
    public static int ShadowRestMs { get; set; } = 1200000;

    public static bool Patrols { get; set; } = true;

    public static int PatrolReach { get; set; } = 220;

    public static int HoldMs { get; set; } = 60000;

    public static int FocusMs { get; set; } = 3000;

    /// <summary>Tiles kept beyond a zone's aggro range at the lookout.</summary>
    public static int Margin { get; set; } = 4;

    /// <summary>The zone's fight against the median bot above which a scout does not go near it.</summary>
    public static double MostVsBot { get; set; } = 3.0;

    /// <summary>How long a scout may walk to its outing before it is given up.</summary>
    public static int WalkMs { get; set; } = 300000;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long RumourOffers { get; private set; }

    public static long RumourFound { get; private set; }

    public static long RumourLate { get; private set; }

    public static long ShadowOffers { get; private set; }

    public static long ShadowReports { get; private set; }

    public static long Warnings { get; private set; }

    public static long WarningsTaken { get; private set; }

    public static long PatrolOffers { get; private set; }

    public static long Stops { get; private set; }

    public static long FocusPasses { get; private set; }

    public static long Quieted { get; private set; }

    public static long Sighted { get; private set; }

    public static long LateWalks { get; private set; }

    private static readonly Dictionary<string, long> _gates = new(StringComparer.Ordinal);

    private static readonly Dictionary<Serial, long> _rested = [];

    private static readonly Dictionary<(int, int), long> _watched = [];

    private static readonly Dictionary<(string, int), long> _shadowed = [];

    private static readonly Dictionary<string, (int Stops, int Reports, int Rumours)> _byGuild = new(StringComparer.Ordinal);

    private static void Gate(string why) => _gates[why] = _gates.GetValueOrDefault(why) + 1;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotOfficers.On(BotOfficerPost.Scout) || bot?.Self is not BotMobile body || body.Guild is not Guild guild)
        {
            return null;
        }

        if (!BotOfficers.Holds(body, BotOfficerPost.Scout) || !body.Alive || body.Tired || body.Squad != null)
        {
            return null;
        }

        if (body.Resolve?.Deed is Patrol or Shadow or Rumour)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map != BotPopulation.Home || BotDungeon.Under(body.Location))
        {
            Gate("off the home ground");

            return null;
        }

        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * 0.8)
        {
            Gate("hurt");

            return null;
        }

        var rumour = Rumours ? Seek(body, map) : null;

        if (rumour != null)
        {
            return rumour;
        }

        var now = Core.TickCount;

        if (_rested.TryGetValue(body.Serial, out var last) && now - last < RestMs)
        {
            return null;
        }

        var seat = BotSeat.Of(guild);
        var home = seat == Point3D.Zero ? body.Location : seat;

        if (Shadows && Follow(body, guild, map, home, now) is { } shadow)
        {
            return shadow;
        }

        return Patrols ? Plan(body, map, home) : null;
    }

    // ---- the rumour --------------------------------------------------------------------------------------------------------

    private static BotDeed Seek(BotMobile body, Map map)
    {
        if (!BotSeasons.Running || !BotRumour.Running || BotRumour.Now is not { } tale || tale.Map != map || tale.Lair)
        {
            return null;
        }

        if (tale.Seeking.Count >= BotRumour.MostSeekers || tale.Seeking.Contains(body.Serial) || !body.InRange(tale.At, BotRumour.Reach))
        {
            return null;
        }

        if (!BotGates.Joined(map, body.Location, tale.At)
            || BotDanger.Road(body, map, body.Location, tale.At, BotQuad.Strength(body), out _, out _, out _, BotRumourSeek.Trade))
        {
            Gate("no safe road to the rumoured chest");

            return null;
        }

        BotRumour.Offer();
        RumourOffers++;

        return new Rumour(new BotRumourSeek(tale));
    }

    /// <summary>The seeking of a rumoured chest, as the scout's own work: the same walk and the same chest, reckoned higher.</summary>
    public sealed class Rumour : BotDeed
    {
        public const string Trade = "scout-rumour";

        private readonly BotRumourSeek _seek;

        public Rumour(BotRumourSeek seek) => _seek = seek;

        public override string Kind => Trade;

        public override Map Map => _seek.Map;

        public override Point3D Where => _seek.Where;

        public override double Expects => _seek.Expects * RumourLean;

        public override double Minutes => _seek.Minutes;

        public override string Stage => $"scouting: {_seek.Stage}";

        public override void Taken(IBotWilful bot) => _seek.Taken(bot);

        public override void Drop(IBotWilful bot) => _seek.Drop(bot);

        public override BotDoing Advance(IBotWilful bot)
        {
            var doing = _seek.Advance(bot);

            if (doing.Kind == BotDoingKind.Done && bot?.Self is BotMobile { Guild: Guild guild } body)
            {
                RumourFound++;
                Tally(guild.Name, 0, 0, 1);
                BotOfficers.News(guild.Name, $"{body.Name}, our scout, found the rumoured chest first.");

                logger.Information("{Bot}, {Guild}'s scout, reached the rumoured chest first: {What}", body.Name, guild.Name, doing.Note);
            }
            else if (doing.Kind == BotDoingKind.Failed)
            {
                RumourLate++;
            }

            return doing;
        }
    }

    // ---- the shadow --------------------------------------------------------------------------------------------------------

    private static BotDeed Follow(BotMobile body, Guild guild, Map map, Point3D home, long now)
    {
        var squads = BotSquads.All;
        BotSquad best = null;
        var bestCount = 0;

        for (var i = 0; i < squads.Count; i++)
        {
            var squad = squads[i];

            if (squad?.Leader?.Self is not BotMobile { Deleted: false, Alive: true } leader || leader.Map != map || leader.Guild is not Guild theirs)
            {
                continue;
            }

            if (ReferenceEquals(theirs, guild) || BotUnderworld.Band(theirs) || squad.Count < ShadowLeast || !leader.InRange(home, ShadowReach))
            {
                continue;
            }

            if (_shadowed.TryGetValue((guild.Name, squad.Id), out var at) && now - at < ShadowRestMs)
            {
                continue;
            }

            // A war first, then the biggest.
            var count = squad.Count + (BotWar.Of(guild.Name, theirs.Name) != null ? 1000 : 0);

            if (count > bestCount)
            {
                bestCount = count;
                best = squad;
            }
        }

        if (best == null)
        {
            return null;
        }

        if (!BotGates.Joined(map, body.Location, best.Leader.Self.Location))
        {
            Gate("no road to the company to shadow");

            return null;
        }

        _shadowed[(guild.Name, best.Id)] = now;
        ShadowOffers++;

        return new Shadow(guild, best, home);
    }

    public sealed class Shadow : BotDeed
    {
        public const string Trade = "scout-shadow";

        private readonly Guild _guild;

        private readonly BotSquad _squad;

        private readonly BotMobile _leader;

        private readonly Point3D _home;

        private readonly Map _map;

        private long _began;

        private long _near;

        private int _most;

        public Shadow(Guild guild, BotSquad squad, Point3D home)
        {
            _guild = guild;
            _squad = squad;
            _leader = squad.Leader.Self as BotMobile;
            _home = home;
            _map = _leader?.Map;
        }

        public override string Kind => Trade;

        public override Map Map => _map;

        public override Point3D Where => _leader?.Location ?? Point3D.Zero;

        public override double Expects => Prior * 1.5;

        public override double Minutes => ShadowMs / 60000.0 + 1.0;

        public override bool Unpaid => true;

        public override bool Braves => true;

        public override string Stage => $"shadowing {_leader?.Guild?.Name ?? "a"} company";

        public override void Taken(IBotWilful bot)
        {
            _began = Core.TickCount;

            if (bot?.Self is { } body)
            {
                _rested[body.Serial] = _began;
            }
        }

        public override BotDoing Advance(IBotWilful bot)
        {
            if (bot?.Self is not BotMobile scout)
            {
                return BotDoing.Failed("no body");
            }

            var now = Core.TickCount;

            if (_leader is not { Deleted: false, Alive: true } || _leader.Squad != _squad || _leader.Map != _map)
            {
                return _near > 0 ? Report(scout, now) : BotDoing.Done("the company I set out to shadow broke up before I reached it");
            }

            _most = Math.Max(_most, _squad.Count);

            if (!scout.InRange(_leader, ShadowKeep + 4))
            {
                if (_near == 0 && now - _began >= WalkMs)
                {
                    LateWalks++;

                    return BotDoing.Failed($"could not come up with {_leader.Guild?.Name}'s company in {WalkMs / 60000} minutes");
                }

                return BotDoing.Walk(_map, _leader, BotArrival.Within(ShadowKeep), $"shadowing {_leader.Guild?.Name}'s company");
            }

            if (_near == 0)
            {
                _near = now;
            }

            return now - _near >= ShadowMs ? Report(scout, now) : BotDoing.Work($"shadowing {_leader.Guild?.Name}'s company of {_squad.Count}");
        }

        private BotDoing Report(BotMobile scout, long now)
        {
            var rival = _leader?.Guild as Guild;
            var name = rival?.Name ?? "a guild";
            var where = _leader?.Location ?? scout.Location;
            var town = BotTowns.Nearest(where)?.Name ?? $"({where.X}, {where.Y})";
            var count = Math.Max(_most, _squad.Count);
            var war = rival != null && BotWar.Of(_guild.Name, rival.Name) != null;
            var warn = war && _leader is { Deleted: false, Alive: true } && Utility.InRange(where, _home, WarnReach);

            ShadowReports++;
            Tally(_guild.Name, 0, 1, 0);

            var fill = new Dictionary<string, string> { ["rival"] = name, ["count"] = count.ToString(), ["town"] = town };

            BotVoice.Say(
                scout,
                "guild",
                BotVoice.Phrase(warn ? "officers:warn" : "officers:shadow", null, scout, fill) ?? $"{name} gathers {count} at {town}.",
                force: true
            );

            BotOfficers.News(_guild.Name, $"{name} gathers {count} near {town}, our scout {scout.Name} says.");

            var taken = false;

            if (warn)
            {
                Warnings++;
                taken = BotFeud.Warn(_guild, _leader);

                if (taken)
                {
                    WarningsTaken++;
                }
            }

            logger.Information(
                "{Scout}, {Guild}'s scout, reports {Rival} gathers {Count} near {Town} at ({X}, {Y}), {Away} tiles from the seat{War}",
                scout.Name,
                _guild.Name,
                name,
                count,
                town,
                where.X,
                where.Y,
                Math.Max(Math.Abs(where.X - _home.X), Math.Abs(where.Y - _home.Y)),
                warn ? taken ? ": a warning, the guild musters against it" : ": a warning, but the war's muster did not take it" : war ? ", at war but far from the seat" : ""
            );

            return BotDoing.Done($"shadowed {name}'s company of {count} near {town} and reported it{(warn ? " as a warning" : "")}");
        }
    }

    // ---- the patrol --------------------------------------------------------------------------------------------------------

    private static BotDeed Plan(BotMobile body, Map map, Point3D home)
    {
        var state = BotZones.State;

        if (state?.Zones == null || state.Map != map || state.Zones.Length == 0)
        {
            Gate("no zones drawn yet");

            return null;
        }

        // The three best by the plan, tried in order: a road too dangerous to one is a reason to look at the next.
        Span<int> picks = stackalloc int[3];
        picks.Fill(-1);

        var keys = new (long Watched, long Bucket, int Away)[3];

        for (var i = 0; i < state.Zones.Length; i++)
        {
            var zone = state.Zones[i];

            if (zone == null || zone.Water || zone.Level != BotZoneLevel.Hostile || zone.VsBot > MostVsBot)
            {
                continue;
            }

            var away = Math.Max(Math.Abs(zone.CenterX - home.X), Math.Abs(zone.CenterY - home.Y));

            if (away > PatrolReach)
            {
                continue;
            }

            var key = (_watched.GetValueOrDefault(Key(zone)), (long)(zone.AwakeSeconds / 60), away);

            for (var p = 0; p < picks.Length; p++)
            {
                if (picks[p] < 0 || key.CompareTo(keys[p]) < 0)
                {
                    for (var q = picks.Length - 1; q > p; q--)
                    {
                        picks[q] = picks[q - 1];
                        keys[q] = keys[q - 1];
                    }

                    picks[p] = i;
                    keys[p] = key;

                    break;
                }
            }
        }

        for (var p = 0; p < picks.Length; p++)
        {
            if (picks[p] < 0)
            {
                break;
            }

            var zone = state.Zones[picks[p]];

            if (!Lookout(map, zone, home, out var at))
            {
                Gate("no lookout outside the zone's reach");

                continue;
            }

            if (!BotGates.Joined(map, body.Location, at))
            {
                Gate("no road to the lookout");

                continue;
            }

            if (BotDanger.Road(body, map, body.Location, at, BotQuad.Strength(body), out _, out _, out _, Patrol.Trade))
            {
                Gate("the road to the lookout is too dangerous");

                continue;
            }

            PatrolOffers++;

            return new Patrol(zone, map, at);
        }

        if (picks[0] < 0)
        {
            Gate("no hostile zone within reach of the seat");
        }

        return null;
    }

    private static (int, int) Key(BotZone zone) => (zone.CenterX >> 4, zone.CenterY >> 4);

    /// <summary>A tile outside the zone's reach on the side of home, standable and not itself in a patrol.</summary>
    private static bool Lookout(Map map, BotZone zone, Point3D home, out Point3D at)
    {
        at = Point3D.Zero;

        var half = Math.Max(zone.X2 - zone.X1, zone.Y2 - zone.Y1) / 2;
        var d = half + Math.Max(zone.AggroRange, zone.ObservedRange) + Margin;
        var dx = home.X - zone.CenterX;
        var dy = home.Y - zone.CenterY;
        var len = Math.Sqrt((double)dx * dx + (double)dy * dy);

        if (len < 1.0)
        {
            dx = 1;
            dy = 0;
            len = 1.0;
        }

        for (var extra = 0; extra <= 8; extra += 4)
        {
            var x = zone.CenterX + (int)Math.Round(dx / len * (d + extra));
            var y = zone.CenterY + (int)Math.Round(dy / len * (d + extra));

            if (!BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            if (BotZones.LevelAt(map, x, y) >= BotZoneLevel.Hostile)
            {
                continue;
            }

            at = new Point3D(x, y, z);

            return true;
        }

        return false;
    }

    public sealed class Patrol : BotDeed
    {
        public const string Trade = "scout";

        private readonly BotZone _zone;

        private readonly Map _map;

        private readonly Point3D _at;

        private readonly Rectangle2D _box;

        private long _began;

        private long _since;

        private long _focused;

        private int _passes;

        public Patrol(BotZone zone, Map map, Point3D at)
        {
            _zone = zone;
            _map = map;
            _at = at;
            _box = new Rectangle2D(zone.X1, zone.Y1, Math.Max(1, zone.X2 - zone.X1 + 1), Math.Max(1, zone.Y2 - zone.Y1 + 1));
        }

        public override string Kind => Trade;

        public override Map Map => _map;

        public override Point3D Where => _at;

        public override double Expects => Prior;

        public override double Minutes => HoldMs / 60000.0 + 1.0;

        public override bool Unpaid => true;

        public override bool Braves => true;

        public override bool Committed => _since != 0;

        public override bool Still => _since != 0;

        public override string Stage => _since != 0 ? $"watching {Label}" : $"on the way to watch {Label}";

        private string Label => string.IsNullOrEmpty(_zone.Label) ? $"the ground at ({_zone.CenterX}, {_zone.CenterY})" : _zone.Label;

        public override void Taken(IBotWilful bot)
        {
            _began = Core.TickCount;

            if (bot?.Self is { } body)
            {
                _rested[body.Serial] = _began;
            }
        }

        public override BotDoing Advance(IBotWilful bot)
        {
            if (bot?.Self is not BotMobile scout)
            {
                return BotDoing.Failed("no body");
            }

            var now = Core.TickCount;

            if (!scout.InRange(_at, 3))
            {
                if (_since == 0 && now - _began >= WalkMs)
                {
                    LateWalks++;

                    return BotDoing.Failed($"could not reach the lookout over {Label} in {WalkMs / 60000} minutes");
                }

                return BotDoing.Walk(_map, _at, BotArrival.Within(2), $"to watch {Label}");
            }

            if (_since == 0)
            {
                _since = now;
                _focused = now - FocusMs;
                scout.Journey?.Finish();
            }

            if (now - _focused >= FocusMs)
            {
                _focused = now;
                BotZoneScan.Focus(_map, _box, now);
                _passes++;
                FocusPasses++;
            }

            if (now - _since < HoldMs)
            {
                return BotDoing.Work($"watching {Label}");
            }

            return Report(scout, now);
        }

        private BotDoing Report(BotMobile scout, long now)
        {
            Stops++;
            _watched[Key(_zone)] = now;

            var live = 0;
            var aggressive = 0;
            Dictionary<string, int> kinds = [];

            foreach (var c in _map.GetMobilesInBounds<BaseCreature>(_box))
            {
                if (c is not { Deleted: false, Alive: true } || c.Controlled || c.Summoned)
                {
                    continue;
                }

                live++;

                if (BotHaunts.AttacksFirst(c.FightMode))
                {
                    aggressive++;

                    var name = BotVoice.Words(c.GetType().Name) ?? "creature";

                    kinds[name] = kinds.GetValueOrDefault(name) + 1;
                }
            }

            Sighted += aggressive;

            var middle = new Point3D(_zone.CenterX, _zone.CenterY, _at.Z);
            var reading = BotPeril.Reading(_map, middle);
            var quiet = aggressive == 0 && reading > 0.0 && BotPeril.Scouted(_map, middle);

            if (quiet)
            {
                Quieted++;
            }

            var what = new StringBuilder();

            if (aggressive == 0)
            {
                what.Append("nothing hostile there now");
            }
            else
            {
                foreach (var (kind, n) in kinds)
                {
                    what.Append(what.Length == 0 ? "" : ", ").Append(n).Append(' ').Append(kind);
                }
            }

            if (_zone.Deaths > 0)
            {
                what.Append($"; {_zone.Deaths} of us died there lately");
            }

            if (scout.Guild is Guild guild)
            {
                Tally(guild.Name, 1, 0, 0);
            }

            var fill = new Dictionary<string, string> { ["place"] = Label, ["what"] = what.ToString() };

            BotVoice.Say(scout, "guild", BotVoice.Phrase("officers:scouted", null, scout, fill) ?? $"{Label}: {what}.");

            logger.Information(
                "{Scout}, {Guild}'s scout, watched {Zone} from ({X}, {Y}) for {Seconds}s: {Passes} passes to the scan, {Live} creatures ({Aggressive} that attack first){Quiet}; the zone reads {VsBot:F1} against the median bot",
                scout.Name,
                (scout.Guild as Guild)?.Name,
                Label,
                _at.X,
                _at.Y,
                (now - _since) / 1000,
                _passes,
                live,
                aggressive,
                quiet ? $", the death map's square there knocked down from {reading:F0}" : "",
                _zone.VsBot
            );

            return BotDoing.Done($"watched {Label} for {(now - _since) / 1000}s: {what}");
        }
    }

    // ---- telling -----------------------------------------------------------------------------------------------------------

    private static void Tally(string guild, int stops, int reports, int rumours)
    {
        var (s, r, u) = _byGuild.GetValueOrDefault(guild);

        _byGuild[guild] = (s + stops, r + reports, u + rumours);
    }

    public static string Tell(string guild)
    {
        var (s, r, u) = _byGuild.GetValueOrDefault(guild);

        return $"scout: {s} zones watched, {r} companies reported, {u} rumoured chests found first";
    }

    public static void Write(Utf8JsonWriter w, string guild)
    {
        var (s, r, u) = _byGuild.GetValueOrDefault(guild);

        w.WriteNumber("zonesWatched", s);
        w.WriteNumber("companiesReported", r);
        w.WriteNumber("rumoursFound", u);
    }

    public static string Describe()
    {
        var say = "";

        foreach (var (why, n) in _gates)
        {
            say += (say.Length == 0 ? "" : ", ") + $"{why} {n}";
        }

        return $"rumours {(Rumours ? "on" : "OFF")}: {RumourOffers} offered, {RumourFound} chests reached first, {RumourLate} too late; "
               + $"shadows {(Shadows ? "on" : "OFF")}: {ShadowOffers} offered, {ShadowReports} reported, {Warnings} warnings ({WarningsTaken} taken by the war's muster); "
               + $"patrols {(Patrols ? "on" : "OFF")}: {PatrolOffers} offered, {Stops} zones watched, {FocusPasses} passes to the scan, {Sighted} attackers sighted, {Quieted} death-map squares knocked down for quiet ground, {LateWalks} walks given up; "
               + $"passed over: {(say.Length == 0 ? "none" : say)}";
    }

    public static void Forget()
    {
        RumourOffers = 0;
        RumourFound = 0;
        RumourLate = 0;
        ShadowOffers = 0;
        ShadowReports = 0;
        Warnings = 0;
        WarningsTaken = 0;
        PatrolOffers = 0;
        Stops = 0;
        FocusPasses = 0;
        Quieted = 0;
        Sighted = 0;
        LateWalks = 0;
        _gates.Clear();
        _rested.Clear();
        _watched.Clear();
        _shadowed.Clear();
        _byGuild.Clear();
    }
}
