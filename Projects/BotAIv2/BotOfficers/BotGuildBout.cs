using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A bout between two guilds, called by a herald: each guild's champion against the other's at the herald's stand, before Argus,
/// for a purse paid chest to chest.
///
/// <para>
/// <b>Patrick's approval of 07.10.2026: the herald "challenges a rival guild to a tournament — a guild-vs-guild bout before Argus
/// with a purse from both chests".</b> The hourly championship (<c>BotTourney</c>, Argus's) is one bot against one for a title; this
/// is a guild's honour and a guild's coin. Once in <see cref="EveryMs"/> a herald standing by the bank names the guild nearest its
/// own in might (<see cref="BotWar.Might"/>) that it is not at war with; the purse is <see cref="PurseShare"/> of the poorer chest,
/// between <see cref="PurseLeast"/> and <see cref="PurseMost"/>, and both chests must hold it. A rival under
/// <see cref="AcceptOdds"/> of the challenger's might declines, and so does one with no champion near. Each guild's champion is its
/// strongest member free within <see cref="Reach"/> of the stand (<see cref="BotQuad.Strength"/>); both are called to the stand
/// (<see cref="BotWill.Press"/>, as a championship summons is) and the duel is the duel everyone fights (<see cref="BotDuel"/>:
/// yield at a third, two minutes, no criminal flag). The loser's chest pays the purse to the winner's (<see cref="BotChest.Pay"/>):
/// coin moves, nothing is made. Argus's herald announces it when the debugger is on (<see cref="Witness"/>, filled from
/// <c>mindedBots/debugger/BotOfficersArgus</c>).
/// </para>
/// </summary>
public static class BotGuildBout
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGuildBout));

    public static bool Running { get; set; } = true;

    /// <summary>How often one guild's herald may challenge. Four hours.</summary>
    public static int EveryMs { get; set; } = 14400000;

    public static double PurseShare { get; set; } = 0.05;

    public static int PurseLeast { get; set; } = 100;

    public static int PurseMost { get; set; } = 800;

    /// <summary>The share of the challenger's might the rival must have to accept.</summary>
    public static double AcceptOdds { get; set; } = 0.6;

    /// <summary>How far from the stand a champion may be called from.</summary>
    public static int Reach { get; set; } = 150;

    /// <summary>How long the champions are waited for.</summary>
    public static int WaitMs { get; set; } = 300000;

    /// <summary>How near the stand both must stand for the bout to begin.</summary>
    public static int RingTiles { get; set; } = 5;

    /// <summary>Argus's herald: the line called at the place. Filled by the debugger; null without one.</summary>
    public static Action<Map, Point3D, string> Witness { get; set; }

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long Challenges { get; private set; }

    public static long Accepted { get; private set; }

    public static long Begun { get; private set; }

    public static long Decided { get; private set; }

    public static long CalledOff { get; private set; }

    public static long PurseGold { get; private set; }

    public static long Witnessed { get; private set; }

    private static readonly Dictionary<string, long> _declined = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, (int Fought, int Won, int Gold)> _byGuild = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, long> _last = new(StringComparer.Ordinal);

    private sealed class Bout
    {
        public string A;

        public string B;

        public BotMobile ChampA;

        public BotMobile ChampB;

        public Map Map;

        public Point3D At;

        public long Called;

        public long Began;

        public bool Ready;

        public bool InRing;

        public int Purse;

        public string Herald;
    }

    private static readonly List<Bout> _bouts = [];

    private static void Decline(string why) => _declined[why] = _declined.GetValueOrDefault(why) + 1;

    private static bool Fighting(string guild)
    {
        for (var i = 0; i < _bouts.Count; i++)
        {
            if (_bouts[i].A == guild || _bouts[i].B == guild)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A herald at its stand challenges a rival, if its guild is due one. From <see cref="BotHeraldStand.Stand"/>.</summary>
    public static void Challenge(BotMobile herald, Guild ours, Point3D at, long now)
    {
        if (!Running || herald?.Map == null || ours?.Name == null || Fighting(ours.Name))
        {
            return;
        }

        if (_last.TryGetValue(ours.Name, out var last) && now - last < EveryMs)
        {
            return;
        }

        _last[ours.Name] = now;

        var mine = BotWar.Might(ours.Name);
        Guild rival = null;
        var gap = double.MaxValue;

        foreach (var other in BotGuilds.Standing)
        {
            if (ReferenceEquals(other, ours) || !BotOfficers.Keeps(other) || other.Members.Count < BotOfficers.LeastMembers || Fighting(other.Name))
            {
                continue;
            }

            if (BotWar.Of(ours.Name, other.Name) != null || ours.Enemies != null && ours.Enemies.Contains(other))
            {
                continue;
            }

            var d = Math.Abs(BotWar.Might(other.Name) - mine);

            if (d < gap)
            {
                gap = d;
                rival = other;
            }
        }

        Challenges++;

        if (rival == null)
        {
            Decline("no rival to challenge");

            return;
        }

        var theirs = BotWar.Might(rival.Name);

        if (mine > 0 && theirs < AcceptOdds * mine)
        {
            Decline("the rival would not fight a stronger guild");
            Say(herald, "officers:declined-bout", $"{rival.Name} will not answer our challenge. They know why.");

            return;
        }

        var purse = (int)Math.Clamp(Math.Min(BotChest.Holds(ours.Name), BotChest.Holds(rival.Name)) * PurseShare, PurseLeast, PurseMost);

        if (BotChest.Holds(ours.Name) < purse || BotChest.Holds(rival.Name) < purse)
        {
            Decline("a chest short of the purse");

            return;
        }

        var map = herald.Map;
        var a = Champion(ours, map, at);
        var b = Champion(rival, map, at);

        if (a == null || b == null)
        {
            Decline("no champion free near the stand");

            return;
        }

        var bout = new Bout { A = ours.Name, B = rival.Name, ChampA = a, ChampB = b, Map = map, At = at, Called = now, Purse = purse, Herald = herald.Name };

        if (!BotWill.Press(a, new Answer(bout), $"{ours.Name}'s champion in a bout against {rival.Name}"))
        {
            Decline("a champion could not be called");

            return;
        }

        if (!BotWill.Press(b, new Answer(bout), $"{rival.Name}'s champion in a bout against {ours.Name}"))
        {
            BotWill.Abandon(a, "the bout could not be set up", unreached: false);
            Decline("a champion could not be called");

            return;
        }

        _bouts.Add(bout);
        Accepted++;

        var fill = new Dictionary<string, string>
        {
            ["rival"] = rival.Name,
            ["purse"] = purse.ToString(),
            ["champion"] = a.Name
        };

        var line = BotVoice.Phrase("officers:challenge", null, herald, fill) ?? $"{ours.Name} challenges {rival.Name} to a bout: {purse}gp a side!";

        BotVoice.Say(herald, "local", line, force: true);
        Announce(map, at, $"{ours.Name} challenges {rival.Name} to a bout at {Town(at)}: {a.Name} against {b.Name}, {purse}gp a side.");
        BotEvents.Post("officers", herald, $"challenged {rival.Name} to a bout: {a.Name} against {b.Name} for {purse}gp");

        logger.Information(
            "{Herald}, {Guild}'s herald, challenged {Rival} to a bout at {Where}: {A} the {ClassA} (strength {SA:F0}) against {B} the {ClassB} (strength {SB:F0}) for {Purse}gp a side; might {Mine:F0} against {Theirs:F0}",
            herald.Name,
            ours.Name,
            rival.Name,
            at,
            a.Name,
            a.Class?.Name,
            BotQuad.Strength(a),
            b.Name,
            b.Class?.Name,
            BotQuad.Strength(b),
            purse,
            mine,
            theirs
        );
    }

    private static BotMobile Champion(Guild guild, Map map, Point3D at)
    {
        BotMobile best = null;
        var strongest = 0.0;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false, Alive: true } m || m.Map != map || m.Squad != null || m.Tired || BotDuel.Duelling(m))
            {
                continue;
            }

            if (!m.InRange(at, Reach) || BotDungeon.Under(m.Location) || m.Hits < m.HitsMax * 0.8)
            {
                continue;
            }

            var s = BotQuad.Strength(m);

            if (s > strongest && BotGates.Joined(map, m.Location, at))
            {
                strongest = s;
                best = m;
            }
        }

        return best;
    }

    /// <summary>The clock: champions arriving, the duel begun, the duel read, the purse paid. From the module's timer.</summary>
    internal static void Beat(long now)
    {
        for (var i = _bouts.Count - 1; i >= 0; i--)
        {
            var bout = _bouts[i];

            if (bout.Began == 0)
            {
                if (bout.ChampA is not { Deleted: false, Alive: true } || bout.ChampB is not { Deleted: false, Alive: true })
                {
                    Off(i, "a champion fell before the bout");

                    continue;
                }

                if (now - bout.Called >= WaitMs)
                {
                    Off(i, $"the champions were not both at the stand in {WaitMs / 60000} minutes");

                    continue;
                }

                if (!bout.Ready)
                {
                    bout.Ready = bout.ChampA.InRange(bout.At, RingTiles) && bout.ChampB.InRange(bout.At, RingTiles);

                    continue;
                }

                // Both arrived and their walk ended on its own beat; then the duel.
                if (bout.ChampA.Resolve?.Deed is Answer || bout.ChampB.Resolve?.Deed is Answer)
                {
                    continue;
                }

                if (!BotDuel.Begin(
                        bout.ChampA,
                        bout.ChampB,
                        bout.ChampA.Bond?.Weapon?.Skill ?? SkillName.Wrestling,
                        bout.ChampB.Bond?.Weapon?.Skill ?? SkillName.Wrestling
                    ))
                {
                    Off(i, "the duel could not be set up");

                    continue;
                }

                bout.Began = now;
                Begun++;

                continue;
            }

            var winner = BotDuel.Winner(bout.ChampA);

            if (winner == null && now - bout.Began >= BotDuel.CapMs + 15000)
            {
                BotDuel.Call(bout.ChampA, "the guilds' bout stood too long");
                winner = BotDuel.Winner(bout.ChampA);
            }

            if (winner == null)
            {
                if (BotDuel.InBout(bout.ChampA) || BotDuel.InBout(bout.ChampB))
                {
                    continue;
                }

                winner = bout.ChampA is { Deleted: false, Alive: true } ? bout.ChampA : bout.ChampB;
            }

            _bouts.RemoveAt(i);
            BotDuel.End(bout.ChampA, bout.ChampB);
            Settle(bout, winner, now);
        }
    }

    private static void Settle(Bout bout, BotMobile winner, long now)
    {
        var won = ReferenceEquals(winner, bout.ChampA) ? bout.A : bout.B;
        var lost = won == bout.A ? bout.B : bout.A;
        var loser = ReferenceEquals(winner, bout.ChampA) ? bout.ChampB : bout.ChampA;
        var paid = BotChest.Pay(lost, won, bout.Purse);

        Decided++;
        PurseGold += paid;

        var (fw, ww, gw) = _byGuild.GetValueOrDefault(won);
        var (fl, wl, gl) = _byGuild.GetValueOrDefault(lost);

        _byGuild[won] = (fw + 1, ww + 1, gw + paid);
        _byGuild[lost] = (fl + 1, wl, gl - paid);

        var how = $"{winner.Hits} hits to {loser.Hits} after {(now - bout.Began) / 1000}s";

        logger.Information(
            "{Winner} of {Won} beat {Loser} of {Lost} in the guilds' bout ({How}); {Lost}'s chest paid {Paid} of the {Purse}gp purse to {Won}'s",
            winner.Name,
            won,
            loser?.Name,
            lost,
            how,
            lost,
            paid,
            bout.Purse,
            won
        );

        var fill = new Dictionary<string, string> { ["winner"] = winner.Name, ["loser"] = loser?.Name ?? "", ["guild"] = won };

        BotVoice.Say(winner, "local", BotVoice.Phrase("officers:bout-won", null, winner, fill) ?? $"{winner.Name} has won the bout for {won}!", force: true);
        Announce(bout.Map, bout.At, $"{winner.Name} of {won} has beaten {loser?.Name} of {lost}. {won} takes the purse of {paid}gp.");
        BotEvents.Post("officers", winner, $"won the guilds' bout for {won} against {loser?.Name} of {lost}: {paid}gp from {lost}'s chest");
        BotOfficers.News(won, $"{winner.Name} beat {loser?.Name} of {lost} in a bout; {paid}gp to our chest.");
        BotOfficers.News(lost, $"{loser?.Name} lost the bout to {winner.Name} of {won}; we paid {paid}gp.");
    }

    private static void Off(int index, string why)
    {
        var bout = _bouts[index];

        _bouts.RemoveAt(index);
        CalledOff++;
        Decline(why);

        Abandon(bout.ChampA, why);
        Abandon(bout.ChampB, why);

        logger.Information("The bout of {A} against {B} was called off: {Why}; no purse changed hands", bout.A, bout.B, why);
        Announce(bout.Map, bout.At, $"The bout between {bout.A} and {bout.B} is called off: {why}.");
    }

    private static void Abandon(BotMobile champ, string why)
    {
        if (champ?.Resolve?.Deed is Answer)
        {
            BotWill.Abandon(champ, $"the bout was called off: {why}", unreached: false);
        }
    }

    private static void Announce(Map map, Point3D at, string line)
    {
        if (Witness != null)
        {
            Witnessed++;

            try
            {
                Witness(map, at, line);
            }
            catch (Exception e)
            {
                logger.Warning("Argus could not announce a bout: {Message}", e.Message);
            }
        }
    }

    private static void Say(BotMobile who, string occasion, string fallback) =>
        BotVoice.Say(who, "local", BotVoice.Phrase(occasion, null, who, null) ?? fallback, force: true);

    private static string Town(Point3D at) => BotTowns.Nearest(at)?.Name ?? $"({at.X}, {at.Y})";

    private static bool Standing(Bout bout) => _bouts.Contains(bout);

    /// <summary>A champion's walk to the stand. Pressed, ended by arriving; the duel is pressed after it.</summary>
    private sealed class Answer : BotDeed
    {
        public const string Trade = "guild-bout";

        private readonly Bout _bout;

        public Answer(Bout bout) => _bout = bout;

        public override string Kind => Trade;

        public override Map Map => _bout.Map;

        public override Point3D Where => _bout.At;

        public override double Expects => 60.0;

        public override double Minutes => 3.0;

        public override bool Unpaid => true;

        public override bool Committed => true;

        public override bool Summons => true;

        public override string Stage => $"called to the bout of {_bout.A} against {_bout.B}";

        public override BotDoing Advance(IBotWilful bot)
        {
            if (bot?.Self is not BotMobile body)
            {
                return BotDoing.Failed("no body");
            }

            if (!Standing(_bout))
            {
                return BotDoing.Done($"the bout of {_bout.A} against {_bout.B} was called off");
            }

            if (!body.InRange(_bout.At, RingTiles - 1))
            {
                return BotDoing.Walk(_bout.Map, _bout.At, BotArrival.Within(RingTiles - 2), $"to the bout of {_bout.A} against {_bout.B}");
            }

            if (!_bout.Ready)
            {
                _bout.InRing = true;

                return BotDoing.Work($"waiting at the stand for the other champion of the bout of {_bout.A} against {_bout.B}");
            }

            return BotDoing.Done($"stood for {body.Guild?.Name} in the bout of {_bout.A} against {_bout.B}");
        }
    }

    public static string Tell(string guild)
    {
        var (f, w, g) = _byGuild.GetValueOrDefault(guild);

        return $"bouts: {f} fought, {w} won, {g}gp in purses net";
    }

    public static void Write(Utf8JsonWriter w, string guild)
    {
        var (f, won, g) = _byGuild.GetValueOrDefault(guild);

        w.WriteNumber("bouts", f);
        w.WriteNumber("boutsWon", won);
        w.WriteNumber("boutGold", g);
    }

    public static string Describe()
    {
        var say = "";

        foreach (var (why, n) in _declined)
        {
            say += (say.Length == 0 ? "" : ", ") + $"{why} {n}";
        }

        return !Running
            ? "OFF"
            : $"{Challenges} challenges, {Accepted} accepted, {Begun} begun, {Decided} decided for {PurseGold}gp of purses chest to chest, {CalledOff} called off, {_bouts.Count} standing, {Witnessed} announced by Argus (refused or off: {(say.Length == 0 ? "none" : say)})";
    }

    public static void Forget()
    {
        Challenges = 0;
        Accepted = 0;
        Begun = 0;
        Decided = 0;
        CalledOff = 0;
        PurseGold = 0;
        Witnessed = 0;
        _declined.Clear();
        _byGuild.Clear();
        _bouts.Clear();
        _last.Clear();
    }
}
