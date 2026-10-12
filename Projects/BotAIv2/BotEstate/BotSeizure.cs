using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A claim on a square another guild holds: war at once, without a meeting, the price of it in every guild's and every town's
/// regard for the claimant, and the weighing a guild does before it pays that price.
///
/// <para>
/// <b>Patrick's order of 06.10.2026, 00:10:</b> "if another guild tries to remove a claim or seizes a square, war is declared
/// without negotiations … but for such a thing every guild's and every town's rating towards that guild falls; they must
/// understand that they are taking a risky step with a great price". Until build 402 a claim on held ground only moved the
/// holder's opinion (<c>BotRegard.Claimed</c>), and since 29.09.2026 an opinion reaches a war only through a meeting — whose
/// duke, on 04.10, answered every grievance with trade terms. At 00:35 on 06.10 The Hammer thought −198 of The Wolf for
/// "claiming the ground by our hall ×8, claiming our ground ×2", and the two stood under trade terms: ten claims on one
/// victim's ground and no war.
/// </para>
///
/// <para>
/// <b>Both kinds of claim count, and only on ground somebody holds.</b> Taking a square (<c>Oust</c>) and striking a name off
/// it (<c>Strip</c>) are the order's "seizes" and "removes a claim". Ground nobody holds is untouched by all of this, including
/// ground by another guild's hall that it never claimed: that is a border (<c>BotRegard.Border</c>), not a seizure. The
/// grievance "claiming the ground by our hall" is written only for a square the victim <em>holds</em> next to its hall
/// (<c>BotClaim.Important</c>), so both of the victim's grievance kinds are seizures.
/// </para>
///
/// <para>
/// <b>The price compounds and is felt for days.</b> The n-th seizure inside <see cref="MemoryMs"/> costs n times
/// <see cref="Regard"/> of every other guild's opinion and n times <see cref="TownStep"/> on the claimant's prices in every
/// town; while a seizure is remembered, time's forgetting (<c>BotRegard.Mend</c>) lifts no opinion of the claimant above
/// −<see cref="Cold"/> times the number remembered; the towns' prices come back down only by tasks done for them
/// (<c>BotBurgh</c>, half a percent each). The fall stops one point above the war line for any guild that was above it, so one
/// seizure never sets the island on the claimant on the same beat — but every neighbour is that much nearer its own grievance.
/// </para>
/// </summary>
public static class BotSeizure
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSeizure));

    /// <summary>Whether a claim on held ground is war at once and has a price. Off, and it moves only the holder's opinion, as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// What one seizure costs of every other guild's opinion of the claimant, times the seizures remembered with it. Thirty: an
    /// ally at the alliance line (60) survives one and not two (<c>BotRegard.Estrangement</c>, 20); a stranger at nought is
    /// at −30, −90 after a second (−60), past the half of the war line (−50) at which the claimant goes to ask for peace.
    /// </summary>
    public static double Regard { get; set; } = 30.0;

    /// <summary>What one seizure adds to the claimant's prices in every town, times the seizures remembered with it. Two percent: four of a town's tasks done to win back.</summary>
    public static double TownStep { get; set; } = 0.02;

    /// <summary>How long a seizure is remembered, in milliseconds of the shard running: two days.</summary>
    public static int MemoryMs { get; set; } = 172800000;

    /// <summary>While a seizure is remembered, time mends no opinion of the claimant above minus this, times the number remembered.</summary>
    public static double Cold { get; set; } = 20.0;

    /// <summary>A guild with this many seizures remembered does not make another: the next would make it an outcast. Nought: no limit.</summary>
    public static int MostRemembered { get; set; } = 2;

    /// <summary>
    /// How many times the claimant's might the holder's side may be, allies counted on both sides, for the claimant still to
    /// seize. One: a guild seizes only from a side no stronger than its own — the war starts the moment it declares, and a
    /// holder above <c>BotWar.YieldMight</c> times its might could make it capitulate. Measured by <c>BotWar.Might</c>, the
    /// same measure as "the weak do not declare on the strong" (<c>BotWar.DauntedAt</c>).
    /// </summary>
    public static double Odds { get; set; } = 1.0;

    /// <summary>How long a weighing between two guilds stands before it is made again. A minute: the staker asks per bot and per square.</summary>
    public static int WeighMs { get; set; } = 60000;

    /// <summary>The cause written into every other guild's book of grievances. See <c>BotGrievances</c>.</summary>
    public const string Grievance = "seizing another guild's ground";

    /// <summary>Why a weighing said no.</summary>
    public enum Cause
    {
        None,
        Ally,
        Busy,
        TheirsBusy,
        Infamy,
        Weaker,

        /// <summary>The holder pays the claimant tribute, or has just agreed to (07.10.2026). See <c>BotTribute</c>.</summary>
        Tribute
    }

    /// <summary>Claims declared on ground another guild holds.</summary>
    public static long Seized { get; private set; }

    /// <summary>Of those, the wars they began at once.</summary>
    public static long WarsBegun { get; private set; }

    /// <summary>Of those, the ones that would have begun a war while war is switched off (<c>BotRegard.Warring</c>).</summary>
    public static long WarsWithheld { get; private set; }

    /// <summary>Of those, the ones made inside a war already standing between the two: no new war, no new price.</summary>
    public static long InWar { get; private set; }

    /// <summary>Agreements, alliances, truces and meetings between the two broken by a seizure.</summary>
    public static long PactsBroken { get; private set; }

    public static long AlliancesBroken { get; private set; }

    public static long TrucesBroken { get; private set; }

    public static long Overtaken { get; private set; }

    /// <summary>Regard taken from claimants, in points, and the opinions it was taken in.</summary>
    public static double RegardTaken { get; private set; }

    public static long Cooled { get; private set; }

    /// <summary>Town standings raised by a seizure, and by how much in all (as a share).</summary>
    public static long TownsRaised { get; private set; }

    public static double TownsAdded { get; private set; }

    /// <summary>Weighings made, and refused by cause.</summary>
    public static long Weighed { get; private set; }

    private static readonly long[] _refused = new long[7];

    /// <summary>Steps of time's forgetting withheld because the opinion was of a guild with a seizure remembered.</summary>
    public static long Withheld { get; private set; }

    /// <summary>Per guild, the ticks of its seizures still remembered.</summary>
    private static readonly Dictionary<string, List<long>> _memory = [];

    private static readonly Dictionary<(string Ours, string Holder), (bool Go, long Tick, Cause Cause)> _weighed = [];

    private static readonly List<string> _others = [];

    /// <summary>How many seizures this guild has made inside <see cref="MemoryMs"/>. Forgets the older ones as it counts.</summary>
    public static int Remembered(string guild)
    {
        if (guild == null || !_memory.TryGetValue(guild, out var ticks))
        {
            return 0;
        }

        var now = Core.TickCount;

        for (var i = ticks.Count - 1; i >= 0; i--)
        {
            if (now - ticks[i] >= MemoryMs)
            {
                ticks.RemoveAt(i);
            }
        }

        if (ticks.Count == 0)
        {
            _memory.Remove(guild);
        }

        return ticks.Count;
    }

    /// <summary>
    /// The highest time's forgetting may lift an opinion of this guild, while it has a seizure remembered. False when it has
    /// none, and the mend is as it always was. Asked by <c>BotRegard.Drift</c> once a drift per opinion held.
    /// </summary>
    public static bool Ceiling(string about, out double ceiling)
    {
        ceiling = 0.0;

        if (!Running || Cold <= 0.0 || _memory.Count == 0)
        {
            return false;
        }

        var n = Remembered(about);

        if (n == 0)
        {
            return false;
        }

        ceiling = -Cold * n;

        return true;
    }

    /// <summary>One step of forgetting withheld. See <see cref="Ceiling"/>.</summary>
    public static void Kept() => Withheld++;

    /// <summary>
    /// Whether <paramref name="ours"/> would lay claim to a square <paramref name="holder"/> holds, having weighed the war it
    /// starts and the price it pays. Asked by the staker (<c>BotHolder</c>) of every held square it looks at; the answer for a
    /// pair stands for <see cref="WeighMs"/>, and is counted once per weighing, not once per square looked at.
    /// </summary>
    public static bool Weigh(Guild ours, string holder)
    {
        if (!Running || ours == null || holder == null || holder == ours.Name)
        {
            return true;
        }

        var key = (ours.Name, holder);
        var now = Core.TickCount;

        var had = _weighed.TryGetValue(key, out var was);

        if (had && now - was.Tick < WeighMs)
        {
            return was.Go;
        }

        var go = Judge(ours, holder, out var cause, out var said);

        _weighed[key] = (go, now, cause);
        Weighed++;

        if (!go)
        {
            _refused[(int)cause]++;
        }

        // Said when the answer for the pair changes, not every minute it is asked again.
        if (had ? was.Cause != cause : !go)
        {
            if (go)
            {
                logger.Information("{Ours} would now lay claim to {Holder}'s ground and pay for it: {Said}", ours.Name, holder, said);
            }
            else
            {
                logger.Information("{Ours} will not lay claim to {Holder}'s ground: {Said}", ours.Name, holder, said);
            }
        }

        return go;
    }

    private static bool Judge(Guild ours, string holder, out Cause cause, out string said)
    {
        cause = Cause.None;
        said = "nothing stands against it";

        if ((BotGuilds.Named(holder) ?? BaseGuild.FindByName(holder) as Guild) is not Guild theirs)
        {
            return true;
        }

        // Already at war with the holder: its ground is the war's own business, and costs nothing new.
        if (BotWar.Of(ours.Name, holder) != null || ours.IsWar(theirs))
        {
            said = "they are at war already";

            return true;
        }

        if (ours.IsAlly(theirs))
        {
            cause = Cause.Ally;
            said = "they are allies";

            return false;
        }

        if (BotWar.Fighting(ours.Name) > 0)
        {
            cause = Cause.Busy;
            said = "it is fighting a war already and could not carry a second";

            return false;
        }

        if (BotWar.Fighting(holder) >= Math.Max(1, BotWar.MostWars))
        {
            cause = Cause.TheirsBusy;
            said = $"{holder} stands in as many wars as it may";

            return false;
        }

        var remembered = Remembered(ours.Name);

        if (MostRemembered > 0 && remembered >= MostRemembered)
        {
            cause = Cause.Infamy;
            said = $"it has seized {remembered} times in {MemoryMs / 3600000} hours, and the next would cost {Regard * (remembered + 1):F0} of every guild's regard and {TownStep * (remembered + 1):P0} in every town";

            return false;
        }

        var mine = Side(ours, theirs);
        var against = Side(theirs, ours);

        if (mine <= 0.0 || against > mine * Odds)
        {
            cause = Cause.Weaker;
            said = $"{holder}'s side is {(mine > 0.0 ? against / mine : 0.0):F2} times its might ({against:F0} against {mine:F0}), and the war starts the moment it declares";

            return false;
        }

        // <b>An ultimatum before a seizure (07.10.2026).</b> A claimant twice the holder's might or more demands tribute instead
        // of taking the square; a holder already paying it, or whose chest can, pays, and the square is left alone. A holder that
        // cannot pay is seized from as before. See BotTribute.Demand.
        if (BotTribute.Demand(ours, theirs, "its ground", out var terms))
        {
            cause = Cause.Tribute;
            said = terms;

            return false;
        }

        said = $"its side's might {mine:F0} against {against:F0}, {remembered} seizures remembered, the price {Regard * (remembered + 1):F0} of every guild's regard and {TownStep * (remembered + 1):P0} in every town";

        return true;
    }

    /// <summary>A guild's might and its allies' — the ones that would join its war against <paramref name="enemy"/>. See <c>BotWar.Enlist</c>.</summary>
    private static double Side(Guild guild, Guild enemy)
    {
        var total = BotWar.Might(guild.Name);
        var allies = guild.Allies;

        for (var i = 0; allies != null && i < allies.Count; i++)
        {
            if (allies[i] is Guild ally && ally != enemy && !ally.IsWar(enemy) && !ally.IsAlly(enemy))
            {
                total += BotWar.Might(ally.Name);
            }
        }

        return total;
    }

    /// <summary>
    /// <paramref name="ours"/> has declared a claim on a square <paramref name="holder"/> holds, and paid <paramref name="paid"/>
    /// for it. Called by <c>BotClaim.Open</c> before the holder's own opinion moves, so that opinion finds the war already
    /// standing and calls no meeting.
    /// </summary>
    public static void Seize(Guild ours, string holder, Point3D middle, BotClaim.Kind want, int paid)
    {
        if (!Running || ours == null || holder == null || holder == ours.Name)
        {
            return;
        }

        if ((BotGuilds.Named(holder) ?? BaseGuild.FindByName(holder) as Guild) is not Guild theirs)
        {
            return;
        }

        Seized++;

        var deed = want == BotClaim.Kind.Strip ? "to strike its name off it" : "to take it";

        if (BotWar.Of(ours.Name, holder) != null || ours.IsWar(theirs))
        {
            InWar++;

            logger.Warning(
                "{Ours} lays claim to {Holder}'s square at {X},{Y} ({Deed}, {Paid}gp) inside the war between them: no new war and no new price",
                ours.Name,
                holder,
                middle.X,
                middle.Y,
                deed,
                paid
            );

            return;
        }

        var n = Remembered(ours.Name) + 1;
        string broken = null;

        // The war at once: whatever stood between the two is broken by the one who seized, first, so nothing sworn at a
        // meeting can come of it afterwards.
        if (BotParley.Overtake(ours.Name, holder, $"{ours.Name} laid claim to {holder}'s square at {middle.X},{middle.Y}"))
        {
            Overtaken++;
            broken = "the meeting between them";
        }

        if (ours.IsAlly(theirs))
        {
            ours.RemoveAlly(theirs);
            AlliancesBroken++;
            broken = broken == null ? "their alliance" : $"{broken}, their alliance";
        }

        if (BotPact.Break(ours.Name, holder) is { } pacts)
        {
            PactsBroken++;
            broken = broken == null ? pacts : $"{broken}, {pacts}";
        }

        if (BotWar.Untruce(ours.Name, holder))
        {
            TrucesBroken++;
            broken = broken == null ? "a truce" : $"{broken}, a truce";
        }

        // War switched off (BotRegard.Warring, "War" in bot-estate.json) is off for this road to it too; the price stands.
        if (BotRegard.Warring)
        {
            BotWar.Declare(ours, theirs, $"seizing: a claim on its square at {middle.X},{middle.Y} {deed}");
            WarsBegun++;
        }
        else
        {
            WarsWithheld++;
        }

        // The price: every other guild thinks worse of it, never past the war line on this beat.
        var fall = Regard * n;
        var cooled = 0;
        var taken = 0.0;

        _others.Clear();

        foreach (var guild in BotGuilds.Standing)
        {
            if (guild != null && !guild.Disbanded && guild.Name != ours.Name && guild.Name != holder && guild.Name != BotUnderworld.GuildName)
            {
                _others.Add(guild.Name);
            }
        }

        for (var i = 0; i < _others.Count; i++)
        {
            var other = _others[i];
            var before = BotRegard.Of(other, ours.Name);
            var to = before - fall;

            if (before > BotRegard.Enmity)
            {
                to = Math.Max(to, BotRegard.Enmity + 1.0);
            }

            to = Math.Max(to, BotRegard.Floor);

            if (to >= before)
            {
                continue;
            }

            BotRegard.Move(other, ours.Name, to - before, Grievance);

            var moved = before - BotRegard.Of(other, ours.Name);

            if (moved > 0.0)
            {
                cooled++;
                taken += moved;
            }
        }

        Cooled += cooled;
        RegardTaken += taken;

        // And every town charges it more.
        var add = TownStep * n;
        var towns = BotBurgh.Shame(ours.Name, add, out var dearest, out var added);

        TownsRaised += towns;
        TownsAdded += added;

        if (!_memory.TryGetValue(ours.Name, out var ticks))
        {
            _memory[ours.Name] = ticks = [];
        }

        ticks.Add(Core.TickCount);
        _weighed.Clear();

        logger.Warning(
            "{Ours} laid claim to {Holder}'s square at {X},{Y} ({Deed}, {Paid}gp): {War}{Broken}; the price, as its seizure {N} in {Hours} hours: {Cooled} guilds think {Taken:F0} worse of it in all ({Fall:F0} each, never past the war line on this beat) and {Towns} towns charge it {Add:P1} more (dearest now ×{Dearest:F3}); time will mend no opinion of it above {Ceiling:F0} while this is remembered",
            ours.Name,
            holder,
            middle.X,
            middle.Y,
            deed,
            paid,
            BotRegard.Warring ? "war at once, without a meeting" : "no war, for war is switched off",
            broken == null ? "" : $", breaking {broken}",
            n,
            MemoryMs / 3600000,
            cooled,
            taken,
            fall,
            towns,
            add,
            dearest,
            -Cold * n
        );

        BotEvents.Post("parley", ours.Name, null, ours.Name, middle.X, middle.Y, $"{ours.Name} laid claim to {holder}'s ground at {middle.X},{middle.Y}: war at once, {taken:F0} of regard and {add:P1} in {towns} towns", "seizure", null);
    }

    /// <summary>A seizure read back off the save, made <paramref name="agoMs"/> of the shard running ago. See <c>BotSeizureStore</c>.</summary>
    public static void Restore(string guild, long agoMs)
    {
        if (string.IsNullOrEmpty(guild) || agoMs < 0 || agoMs >= MemoryMs)
        {
            return;
        }

        if (!_memory.TryGetValue(guild, out var ticks))
        {
            _memory[guild] = ticks = [];
        }

        ticks.Add(Core.TickCount - agoMs);
    }

    /// <summary>Every seizure remembered, as guild and how long ago. For the store.</summary>
    public static IEnumerable<(string Guild, long AgoMs)> Memory()
    {
        var now = Core.TickCount;

        foreach (var (guild, ticks) in _memory)
        {
            for (var i = 0; i < ticks.Count; i++)
            {
                var ago = now - ticks[i];

                if (ago >= 0 && ago < MemoryMs)
                {
                    yield return (guild, ago);
                }
            }
        }
    }

    private static string Remembering()
    {
        if (_memory.Count == 0)
        {
            return "nobody";
        }

        using var say = Server.Text.ValueStringBuilder.Create(128);

        foreach (var (guild, ticks) in _memory)
        {
            if (ticks.Count == 0)
            {
                continue;
            }

            say.Append(say.Length > 0 ? ", " : "");
            say.Append($"{guild} ×{ticks.Count}");
        }

        return say.Length == 0 ? "nobody" : say.ToString();
    }

    public static string Describe() =>
        !Running
            ? "seizures: a claim on held ground moves only the holder's opinion"
            : $"seizures (a claim on a square another guild holds is war at once, without a meeting, and costs the claimant {Regard:F0}×n of every other guild's regard, never past the war line on that beat, and +{TownStep:P0}×n in every town, n counting its seizures in {MemoryMs / 3600000}h, time mending no opinion of it above −{Cold:F0}×n; a guild with {MostRemembered} remembered, at war, allied with the holder or weaker than the holder's side ×{Odds:0.##} does not seize): "
            + $"{Seized} declared, {WarsBegun} wars begun by one ({WarsWithheld} withheld while war is switched off; {Overtaken} meetings, {AlliancesBroken} alliances, {PactsBroken} agreements and {TrucesBroken} truces broken by them), {InWar} inside a war already standing; "
            + $"{RegardTaken:F0} regard taken from the claimants in {Cooled} opinions, {TownsRaised} town standings raised by {TownsAdded:P1} in all, {Withheld} steps of forgetting withheld; "
            + $"{Weighed} weighings, refused {_refused[(int)Cause.Ally]} for an ally, {_refused[(int)Cause.Busy]} for a war of its own, {_refused[(int)Cause.TheirsBusy]} for a holder at its limit of wars, {_refused[(int)Cause.Infamy]} for the seizures it remembers, {_refused[(int)Cause.Weaker]} for a stronger holder, {_refused[(int)Cause.Tribute]} for a holder paying tribute instead; remembered now: {Remembering()}";

    /// <summary>The session's counters and the weighings. The memory is the store's, and goes only with <see cref="Wipe"/>.</summary>
    public static void Forget()
    {
        Seized = 0;
        WarsBegun = 0;
        WarsWithheld = 0;
        InWar = 0;
        PactsBroken = 0;
        AlliancesBroken = 0;
        TrucesBroken = 0;
        Overtaken = 0;
        RegardTaken = 0;
        Cooled = 0;
        TownsRaised = 0;
        TownsAdded = 0;
        Weighed = 0;
        Withheld = 0;
        Array.Clear(_refused);
        _weighed.Clear();
        _others.Clear();
    }

    /// <summary>Every seizure forgotten, for Patrick's reset.</summary>
    public static int Wipe()
    {
        var gone = _memory.Count;

        _memory.Clear();
        Forget();

        return gone;
    }
}
