using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Spells;
using Server.Spells.Eighth;
using Server.Targeting;

namespace Server.BotAI.V2;

/// <summary>One infirmary while it is open: the healer keeping it, its guild, where it stands, who waits, and the body it went out to.</summary>
public sealed class BotInfirmary
{
    public BotInfirmary(BotMobile healer, Guild guild, Map map, Point3D stand)
    {
        Healer = healer;
        Guild = guild;
        Map = map;
        Stand = stand;
        OpenedTick = Core.TickCount;
        BusyTick = OpenedTick;
    }

    public BotMobile Healer { get; }

    public Guild Guild { get; }

    public Map Map { get; }

    public Point3D Stand { get; }

    public long OpenedTick { get; }

    public bool Shut { get; internal set; }

    /// <summary>Patients standing by, in the order they came.</summary>
    internal readonly List<BotMobile> Waiting = [];

    /// <summary>Patients seen and charged, until their own errand reads it.</summary>
    internal readonly HashSet<Serial> Seen = [];

    /// <summary>The fallen guildmate the healer has gone out to raise, and the death it is raising it from.</summary>
    public BotMobile Call { get; internal set; }

    public long CallFell { get; internal set; }

    /// <summary>The fee agreed for the call: the raise fee, or more when the healer reads a scroll that costs more (life18b).</summary>
    public int CallFee { get; internal set; }

    /// <summary>
    /// The healer was called away for a moment — a meeting, a duel — and the infirmary waits for it rather than shutting (07.10.2026,
    /// life18b): on build 416 140 of 142 shifts were shut "called away" after 2.1 minutes on average, and reopened as new ones.
    /// </summary>
    public bool Away { get; internal set; }

    public long AwayTick { get; internal set; }

    public int Healed { get; internal set; }

    public int Raised { get; internal set; }

    /// <summary>Opened on demand (life19): shut after <see cref="BotInfirmaries.IdleMs"/> with nobody to see, once <see cref="BotInfirmaries.DemandMinutes"/> are kept.</summary>
    public bool OnDemand { get; internal set; }

    /// <summary>The last moment somebody needed it: opened, a patient sent or standing by, a body claimed, a fresh want of its guild.</summary>
    public long BusyTick { get; internal set; }

    public int Takings { get; internal set; }

    public string Name => $"{Healer?.Name}'s infirmary of {Guild?.Name}";
}

/// <summary>
/// The infirmaries: a guild's healer keeping one at the guild's seat for a shift, its wounded guildmates and allies coming to be
/// bound or healed for a fee by the hit point, and its fallen raised where they fell for a fee below what a death costs — the
/// healer going out to the body, as a company's raiser does (07.10.2026, trades17).
///
/// <para>
/// <b>Patrick, 07.10.2026: "a healer with an infirmary (resurrection and healing for a fee by its guild)".</b> The value is real on
/// both sides. A wounded bot with no bandage and no spell recovers a hit point every eleven seconds and is too hurt to hunt below
/// <c>BotHunter.FitAt</c>; the infirmary binds it in a minute for <see cref="FeePerHit"/> a point. A bot that dies leaves some 186gp
/// on and around its corpse (67 of coin, 119 of kit: an-money14 §3.2) and is carried home a minute later with the pack left
/// where it lay; raised where it fell it strips its own corpse at once (<c>BotRaise.Rise</c>), and pays <see cref="RaiseFee"/> for
/// it, a third of that. The means are the engine's (<c>BotRaise.Best</c>: the scroll, the book, the bandage of a master), spent only
/// as the engine spends them; and the ordinary revival is held for the healer's walk at most <see cref="RaiseHoldMs"/> after the
/// death (<see cref="Holding"/>, one hook in <c>BotRaise.Holding</c>).
/// </para>
/// </summary>
public static class BotInfirmaries
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotInfirmaries));

    // ---- Dials ------------------------------------------------------------------------------------------------------------

    /// <summary>Infirmaries at all. Off, none opens, no patient is sent and no body is claimed.</summary>
    public static bool Running { get; set; } = true;

    public static bool On => BotTrades.Running && Running;

    /// <summary>How long a healer keeps its infirmary, in minutes. Twenty.</summary>
    public static double ShiftMinutes { get; set; } = 20.0;

    /// <summary>How long after a shift the same healer may keep another. Twenty minutes.</summary>
    public static int RestMs { get; set; } = 1200000;

    /// <summary>How far a healer may be from its guild's seat and be offered the shift. A hundred and fifty tiles.</summary>
    public static int KeeperReach { get; set; } = 150;

    /// <summary>The fewest bandages a healer without a heal spell must carry to keep an infirmary. Ten.</summary>
    public static int LeastBandages { get; set; } = 10;

    /// <summary>
    /// How far a wounded guildmate may be from the infirmary and be sent to it. A hundred and sixty tiles (life18b; 120 before): the
    /// walk is priced by the appraisal's own nearness, so a far patient is offered and weighs it.
    /// </summary>
    public static int PatientReach { get; set; } = 160;

    /// <summary>How long an infirmary waits for a healer called away before it shuts. Three minutes (life18b).</summary>
    public static int AwayMs { get; set; } = 180000;

    /// <summary>
    /// Whether a healer keeping an open infirmary is left at it by a company's call (<c>BotSquads.Pressable</c>, <c>BotBand.LevyMedic</c>):
    /// on build 416, 104 of 142 shifts ended "pressed to enlist". The company calls the next healer. A switch (life18b).
    /// </summary>
    public static bool SpareKeepers { get; set; } = true;

    /// <summary>
    /// The raise fee over a resurrection scroll's market price, when the healer raises with one: a quarter over (life18b). A scroll
    /// costs 75–88gp on build 416, so a raise at 60 lost the healer money every time; the fee is the larger of this and <see cref="RaiseFee"/>.
    /// </summary>
    public static double RaiseMarkup { get; set; } = 1.25;

    /// <summary>
    /// What a guild's Healer that reads a resurrection scroll keeps after buying one for its infirmary (life18b): a hundred, against the
    /// 300gp in the pack every other reader keeps (<c>BotRaise.StockReserve</c>). Read in <c>BotLifeline</c> through <see cref="Stocks"/>.
    /// </summary>
    public static int ScrollKeep { get; set; } = 100;

    /// <summary>The most patients waiting at one infirmary. Six.</summary>
    public static int MostWaiting { get; set; } = 6;

    /// <summary>The fee for each hit point healed. A half: a bandage at the shopkeeper's five gold binds some twenty points.</summary>
    public static double FeePerHit { get; set; } = 0.5;

    /// <summary>The least fee for a visit that healed anything. Five.</summary>
    public static int FeeLeast { get; set; } = 5;

    /// <summary>Tries at one patient before it is sent away with what it got.</summary>
    public static int HealTries { get; set; } = 6;

    /// <summary>
    /// The fee for raising a fallen guildmate where it fell. Sixty: a third of the 186gp a death costs on this shard (an-money14 §3.2),
    /// most of which a raising where it fell gives back.
    /// </summary>
    public static int RaiseFee { get; set; } = 60;

    /// <summary>
    /// How far from the infirmary a body may lie for its healer to go out to it. A hundred and fifty tiles (life18b; 60 before): of 386
    /// guild deaths on build 416 two fell within 60 tiles of their seat and fourteen within 200. A mounted healer covers it inside
    /// the hold.
    /// </summary>
    public static int RaiseReach { get; set; } = 150;

    /// <summary>How soon after the death a body must be claimed: later, the ordinary revival is close and the healer would only race it.</summary>
    public static int ClaimWithinMs { get; set; } = 30000;

    /// <summary>
    /// The longest after a death the ordinary revival is held for an infirmary's healer. Three minutes (life18b; two before): the walk
    /// out of <see cref="RaiseReach"/> and a scroll read at a fifth's chance.
    /// </summary>
    public static int RaiseHoldMs { get; set; } = 180000;

    /// <summary>How often an open infirmary looks for a fallen guildmate. Three seconds; a walk of the population list, no spatial query.</summary>
    public static int ScanMs { get; set; } = 3000;

    /// <summary>
    /// <b>The shift on demand, not standing (07.10.2026, life19).</b> On build 419 (2.3 h) 14 infirmaries opened and 13 shut with 0 patients and
    /// 0 raised: of 699 wounded looks 341 were in a fight, a company or a delve and 358 could heal themselves, and of 88 guild deaths 66 fell
    /// with none open and 18 farther than 150 tiles from it. Nobody who could not mend itself came, because almost every bot carries
    /// bandages. With this on, a guild's healer is offered the shift only while somebody of its guild needs it within reach of the seat — a
    /// wounded bot that cannot mend itself (<see cref="BotPatienter"/>), or a fresh death the healer can raise — and the shift is short:
    /// <see cref="DemandMinutes"/>, shut after <see cref="IdleMs"/> with nobody to see. Off: the standing twenty-minute shift.
    /// </summary>
    public static bool OnDemand { get; set; } = true;

    /// <summary>How long a want (a wounded bot that cannot mend itself, a body near the seat) stands for a healer to answer. Two minutes.</summary>
    public static int DemandMs { get; set; } = 120000;

    /// <summary>The least an on-demand shift is kept before it may shut idle. Three minutes.</summary>
    public static double DemandMinutes { get; set; } = 3.0;

    /// <summary>How long an on-demand infirmary stays open with nobody waiting, nobody sent and no body to go to. Two minutes.</summary>
    public static int IdleMs { get; set; } = 120000;

    /// <summary>How long after an on-demand shift the same healer may keep another. Two minutes: the shift was for somebody.</summary>
    public static int DemandRestMs { get; set; } = 120000;

    /// <summary>
    /// How long the ordinary revival is held for a guild death near the seat while no infirmary is open but a healer of the guild could raise
    /// it (life19): the healer's walk to the seat and the claim. Ninety seconds after the death; once claimed, <see cref="RaiseHoldMs"/> holds.
    /// </summary>
    public static int DemandHoldMs { get; set; } = 90000;

    /// <summary>
    /// life19c: a log line at each step of the path from a guild death to a healer pressed into a shift ("death seen", "seat", "healer",
    /// "pressed"), so the trigger is seen to run. Deaths are some forty an hour: four lines each.
    /// </summary>
    public static bool TraceTrigger { get; set; } = true;

    // ---- Counters ---------------------------------------------------------------------------------------------------------

    public static long Opened { get; private set; }

    public static long Closed { get; private set; }

    public static long Patients { get; private set; }

    public static long HitsHealed { get; private set; }

    public static long HealGold { get; private set; }

    public static long Unhealed { get; private set; }

    public static long Unpaid { get; private set; }

    public static long Claimed { get; private set; }

    public static long Raised { get; private set; }

    public static long RaiseGold { get; private set; }

    public static long RaiseThings { get; private set; }

    public static long GaveUp { get; private set; }

    public static long Beaten { get; private set; }

    // The fallen, judged once a death when its claim window closes: why no infirmary's healer went out to it (life18b).

    /// <summary>Deaths of guilded bots looked at by the scan, while infirmaries were on.</summary>
    public static long Fell { get; private set; }

    public static long FellGuildless { get; private set; }

    public static long FellNoneOpen { get; private set; }

    public static long FellFar { get; private set; }

    public static long FellNoMeans { get; private set; }

    public static long FellBusy { get; private set; }

    public static long FellAway { get; private set; }

    public static long FellCompany { get; private set; }

    public static long FellDelving { get; private set; }

    public static long FellUnholy { get; private set; }

    public static long FellGround { get; private set; }

    public static long FellPoor { get; private set; }

    /// <summary>Times the healer was called away and the infirmary waited for it; came back; and shut for not coming back.</summary>
    public static long Aways { get; private set; }

    public static long Returns { get; private set; }

    public static long AwayShut { get; private set; }

    /// <summary>Times a company's call passed over a healer keeping an open infirmary. See <see cref="SpareKeepers"/>.</summary>
    public static long Spared { get; private set; }

    // life19: the shift on demand.

    /// <summary>Wants raised with no infirmary of the guild's open: a wounded bot that cannot mend itself near its seat; a death near its seat.</summary>
    public static long WantsHurt { get; private set; }

    public static long WantsFallen { get; private set; }

    /// <summary>Deaths with none open, by where: within <see cref="RaiseReach"/> of the seat with a healer able to raise; near it with none; far.</summary>
    public static long FellNoneNear { get; private set; }

    public static long FellNoneNoRaiser { get; private set; }

    public static long FellNoneFar { get; private set; }

    /// <summary>life19b: deaths with none open near the seat whose able healers were all busy; and the far ones by distance to the seat.</summary>
    public static long FellNoneBusy { get; private set; }

    public static long FarTo300 { get; private set; }

    public static long FarTo600 { get; private set; }

    public static long FarBeyond { get; private set; }

    /// <summary>life19b: calls to open a shift (a death or a wound near the seat), by outcome.</summary>
    public static long Triggers { get; private set; }

    public static long TriggerCalled { get; private set; }

    public static long CalledFallen { get; private set; }

    public static long CalledHurt { get; private set; }

    public static long TriggerOpen { get; private set; }

    public static long TriggerAgain { get; private set; }

    public static long TriggerBusy { get; private set; }

    public static long TriggerNoHealer { get; private set; }

    /// <summary>Deaths whose revival was held for an on-demand healer; of them claimed after; the rest lapsed to the ordinary revival.</summary>
    public static long Held { get; private set; }

    public static long HeldClaimed { get; private set; }

    public static long HeldLapsed { get; private set; }

    /// <summary>Infirmaries opened on demand, and shut idle.</summary>
    public static long OpenedOnDemand { get; private set; }

    public static long IdleShut { get; private set; }

    /// <summary>Minutes kept open by all shifts, and by those that saw nobody: the healer's time the shifts cost.</summary>
    public static double MinutesOpen { get; private set; }

    public static double MinutesEmpty { get; private set; }

    internal static void NoteIdleShut() => IdleShut++;

    internal static void NoteWantHurt() => WantsHurt++;

    private sealed class Want
    {
        public long Tick;

        public int Worth;

        public bool Hurt;

        public bool Fallen;
    }

    private static readonly Dictionary<Guild, Want> _wants = [];

    private static readonly Dictionary<Serial, long> _held = [];

    private sealed class Judging
    {
        public long Fell;

        public string Cause;

        /// <summary>Where a death with none open lay against its seat (life19), judged once: "none open near" / "none open no raiser" / "none open far".</summary>
        public string NoneCause;

        public long Window = ClaimWithinMs;

        /// <summary>Whether the trace said why no call was made for this death (life19c), once.</summary>
        public bool Traced;
    }

    private static readonly Dictionary<Serial, Judging> _judging = [];

    private static readonly List<Serial> _settled = [];

    private static readonly List<BotInfirmary> _open = [];

    private static readonly Dictionary<Serial, BotInfirmary> _claims = [];

    private static long _scanTick;

    public static IReadOnlyList<BotInfirmary> Open => _open;

    // ---- Opening and shutting ----------------------------------------------------------------------------------------------

    /// <summary>The infirmary of this guild open now, or null.</summary>
    public static BotInfirmary Of(Guild guild)
    {
        if (guild == null)
        {
            return null;
        }

        for (var i = 0; i < _open.Count; i++)
        {
            if (!_open[i].Shut && ReferenceEquals(_open[i].Guild, guild))
            {
                return _open[i];
            }
        }

        return null;
    }

    /// <summary>Whether a bot of <paramref name="guild"/> is treated at <paramref name="infirmary"/>: its own guild's, or an ally's.</summary>
    public static bool Treats(BotInfirmary infirmary, Guild guild) =>
        infirmary?.Guild != null && guild != null && (ReferenceEquals(infirmary.Guild, guild) || infirmary.Guild.IsAlly(guild));

    /// <summary>Where a guild's infirmary stands: its seat, the house in a town first. Zero when it has none.</summary>
    public static Point3D Seat(Guild guild) => BotSeat.Of(guild);

    internal static BotInfirmary OpenAt(BotMobile healer, Guild guild, Map map, bool onDemand = false, Point3D stand = default)
    {
        if (healer == null || guild == null || Of(guild) != null)
        {
            return null;
        }

        var infirmary = new BotInfirmary(healer, guild, map, stand == Point3D.Zero ? healer.Location : stand) { OnDemand = onDemand };

        _open.Add(infirmary);
        Opened++;

        if (onDemand)
        {
            OpenedOnDemand++;
        }

        logger.Information(
            "{Name} opened {Guild}'s infirmary at ({X}, {Y}): wounded of the guild and its allies within {Reach} tiles are bound for {Fee}gp a point, its fallen within {RaiseReach} tiles raised where they fell for {RaiseFee}gp",
            healer.Name,
            guild.Name,
            healer.X,
            healer.Y,
            PatientReach,
            FeePerHit,
            RaiseReach,
            RaiseFee
        );

        BotEvents.Post("infirmary", healer, $"opened {guild.Name}'s infirmary");

        var fill = new Dictionary<string, string> { ["guild"] = guild.Name };
        var line = BotVoice.Phrase("infirmary:open", null, healer, fill);

        if (line != null)
        {
            BotVoice.Say(healer, "guild", line);
        }

        return infirmary;
    }

    internal static void Close(BotInfirmary infirmary, string why)
    {
        if (infirmary == null || infirmary.Shut)
        {
            return;
        }

        infirmary.Shut = true;
        _open.Remove(infirmary);

        if (infirmary.Call != null)
        {
            _claims.Remove(infirmary.Call.Serial);
            infirmary.Call = null;
        }

        Closed++;

        var kept = (Core.TickCount - infirmary.OpenedTick) / 60000.0;

        MinutesOpen += kept;

        if (infirmary.Healed == 0 && infirmary.Raised == 0)
        {
            MinutesEmpty += kept;
        }

        logger.Information(
            "{Name} shut {Guild}'s infirmary after {Minutes:F1} minutes — {Why}: {Healed} patients healed, {Raised} raised where they fell, {Gold}gp in fees",
            infirmary.Healer?.Name,
            infirmary.Guild?.Name,
            (Core.TickCount - infirmary.OpenedTick) / 60000.0,
            why,
            infirmary.Healed,
            infirmary.Raised,
            infirmary.Takings
        );
    }

    // ---- Wants (life19) -----------------------------------------------------------------------------------------------------

    /// <summary>
    /// Somebody of <paramref name="guild"/> needs its infirmary within reach of the seat: a wounded bot that cannot mend itself, or a body.
    /// An open one is kept from shutting idle; with none open, the want stands <see cref="DemandMs"/> for a healer (<see cref="BotInfirmarer"/>).
    /// </summary>
    internal static void Wanted(Guild guild, int worth, bool fallen, long now)
    {
        if (guild == null)
        {
            return;
        }

        if (Of(guild) is { } open)
        {
            open.BusyTick = now;

            return;
        }

        if (!_wants.TryGetValue(guild, out var want) || now - want.Tick >= DemandMs)
        {
            want = new Want();
            _wants[guild] = want;
        }

        want.Tick = now;
        want.Worth = Math.Max(want.Worth, worth);
        want.Hurt |= !fallen;
        want.Fallen |= fallen;
    }

    /// <summary>Whether a want of this guild stands now, and what it is worth in fees, for the hurt and for the fallen.</summary>
    public static bool Wanting(Guild guild, long now, out int worth, out bool hurt, out bool fallen)
    {
        worth = 0;
        hurt = false;
        fallen = false;

        if (guild == null || !_wants.TryGetValue(guild, out var want) || now - want.Tick >= DemandMs)
        {
            return false;
        }

        worth = want.Worth;
        hurt = want.Hurt;
        fallen = want.Fallen;

        return true;
    }

    /// <summary>An infirmary somebody is on the way to or standing by: kept from shutting idle.</summary>
    internal static void Busy(BotInfirmary infirmary)
    {
        if (infirmary != null)
        {
            infirmary.BusyTick = Core.TickCount;
        }
    }

    /// <summary>
    /// <b>A want opens the shift (07.10.2026, life19b).</b> A guild death within <see cref="RaiseReach"/> of the seat, or a wounded guildmate
    /// that cannot mend itself within <see cref="PatientReach"/> of it, with no hall of the guild's open: the nearest Healer of the guild
    /// within <see cref="KeeperReach"/> of the seat that can serve it (raise the body; bandages or a heal for the wound) and whose work a
    /// company's call could put down (<c>BotSquads.Pressable</c>) is pressed into an on-demand shift there and then. One call a guild per
    /// <see cref="DemandMs"/>. Returns "called", "already called", "open", "no healer" or "busy"; every call logged and counted.
    /// </summary>
    internal static string Call(Guild guild, Point3D seat, Map map, BotMobile cause, bool fallen, int worth, long now)
    {
        Triggers++;

        if (Of(guild) != null)
        {
            TriggerOpen++;

            return "open";
        }

        if (_called.TryGetValue(guild, out var last) && now - last < DemandMs)
        {
            TriggerAgain++;

            return "already called";
        }

        BotMobile best = null;
        var nearest = int.MaxValue;
        string busy = null;
        var bots = BotPopulation.Bots;

        // life19c: every Healer of the guild counted by why it is not the one, for the trace.
        int healers = 0, dead = 0, elsewhere = 0, far = 0, company = 0, tired = 0, meansless = 0, busied = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is not { Deleted: false } healer || healer.Class?.Name != "Healer" || !ReferenceEquals(healer.Guild, guild) || ReferenceEquals(healer, cause))
            {
                continue;
            }

            healers++;

            if (!healer.Alive)
            {
                dead++;

                continue;
            }

            if (healer.Map != map)
            {
                elsewhere++;

                continue;
            }

            if (!Utility.InRange(healer.Location, seat, KeeperReach))
            {
                far++;

                continue;
            }

            if (healer.Squad != null)
            {
                company++;

                continue;
            }

            if (healer.Tired)
            {
                tired++;

                continue;
            }

            if (fallen ? BotRaise.Best(healer, out _, out _) == BotRaise.Means.None
                    : BotMend.Cloth(healer) < LeastBandages && !BotMend.Herbs(healer, BotArsenal.SpellHeal) && !BotMend.Herbs(healer, BotArsenal.SpellGreaterHeal))
            {
                meansless++;

                continue;
            }

            var away = Math.Max(Math.Abs(healer.X - seat.X), Math.Abs(healer.Y - seat.Y));

            if (away >= nearest)
            {
                continue;
            }

            if (healer.Combatant != null || !BotSquads.Pressable(healer))
            {
                busy = healer.Resolve?.Deed?.Kind ?? "a fight";
                busied++;

                continue;
            }

            best = healer;
            nearest = away;
        }

        if (TraceTrigger)
        {
            logger.Information(
                "Infirmary trigger 3/4, healer: for {Cause}'s {What}, {Found}; {Guild} has {Healers} Healers: {Dead} dead, {Elsewhere} on another map, {Far} over {Reach} tiles from the seat, {Company} in a company, {Tired} tired, {Meansless} {Means}, {Busy} busy",
                cause?.Name,
                fallen ? "death" : "wound",
                best != null ? $"able healer found: {best.Name}, {nearest} tiles from the seat" : "no able healer found",
                guild.Name,
                healers,
                dead,
                elsewhere,
                far,
                KeeperReach,
                company,
                tired,
                meansless,
                fallen ? $"with nothing to raise with (no resurrection scroll and Magery for one, no book, no 80 Healing and 80 Anatomy)" : $"with under {LeastBandages} bandages and no heal",
                busied
            );
        }

        if (best == null)
        {
            if (busy != null)
            {
                TriggerBusy++;
                _busyKinds[busy] = _busyKinds.GetValueOrDefault(busy) + 1;

                logger.Information(
                    "{Cause}'s {What} at ({X}, {Y}) calls nobody to open {Guild}'s infirmary: its healers able are busy ({Busy})",
                    cause?.Name,
                    fallen ? "death" : "wound",
                    cause?.X,
                    cause?.Y,
                    guild.Name,
                    busy
                );

                return "busy";
            }

            TriggerNoHealer++;

            if (TraceTrigger)
            {
                logger.Information("Infirmary trigger 4/4, pressed: no — {Guild} has no healer able for {Cause}'s {What}", guild.Name, cause?.Name, fallen ? "death" : "wound");
            }

            return "no healer";
        }

        var claim = BotInfirmarer.Prior + worth / Math.Max(1.0, DemandMinutes);
        var deed = new BotKeepInfirmary(map, guild, seat, claim, true, fallen);
        var why = fallen ? $"{cause?.Name} fell {(int)cause.GetDistanceToSqrt(seat)} tiles from the seat" : $"{cause?.Name} is hurt past mending itself near the seat";

        var pressed = BotWill.Press(best, deed, why);

        if (TraceTrigger)
        {
            logger.Information(
                "Infirmary trigger 4/4, pressed: {Answer} — {Healer} into an on-demand shift at {Guild}'s seat for {Cause}'s {What}",
                pressed ? "yes" : "no, the press was refused",
                best.Name,
                guild.Name,
                cause?.Name,
                fallen ? "death" : "wound"
            );
        }

        if (!pressed)
        {
            TriggerBusy++;

            return "busy";
        }

        _called[guild] = now;
        TriggerCalled++;

        if (fallen)
        {
            CalledFallen++;
        }
        else
        {
            CalledHurt++;
        }

        logger.Information(
            "{Cause}'s {What} at ({X}, {Y}) calls {Healer} to open the infirmary at {Guild}'s seat ({Away} tiles from it, the healer {From} tiles off)",
            cause?.Name,
            fallen ? "death" : "wound",
            cause?.X,
            cause?.Y,
            best.Name,
            guild.Name,
            cause == null ? -1 : (int)cause.GetDistanceToSqrt(seat),
            nearest
        );

        BotVoice.Say(best, "guild", fallen ? $"{cause?.Name} is down near the hall. I'm on my way." : $"{cause?.Name}, come to the hall, I'll bind you.");

        return "called";
    }

    private static readonly Dictionary<Guild, long> _called = [];

    private static readonly Dictionary<string, long> _busyKinds = [];

    /// <summary>
    /// Whether a guild healer could go out to a body near the seat (life19): a Healer of the guild, alive, out of any company, within
    /// <see cref="KeeperReach"/> of the seat, with the means to raise. A walk of the population list, once a death.
    /// </summary>
    private static bool Raiser(Guild guild, Point3D seat, Map map)
    {
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false, Alive: true, Squad: null } healer && healer.Class?.Name == "Healer" && ReferenceEquals(healer.Guild, guild)
                && healer.Map == map && Utility.InRange(healer.Location, seat, KeeperReach) && BotRaise.Best(healer, out _, out _) != BotRaise.Means.None)
            {
                return true;
            }
        }

        return false;
    }

    // ---- Patients -----------------------------------------------------------------------------------------------------------

    /// <summary>The open infirmary of this bot's guild or an ally's nearest it within reach, on joined ground; null with why not.</summary>
    public static BotInfirmary For(BotMobile patient, out string refusal)
    {
        refusal = "none";
        BotInfirmary best = null;
        var nearest = int.MaxValue;

        if (patient?.Guild is not Guild guild)
        {
            return null;
        }

        for (var i = 0; i < _open.Count; i++)
        {
            var infirmary = _open[i];

            if (infirmary.Shut || infirmary.Map != patient.Map || ReferenceEquals(infirmary.Healer, patient) || !Treats(infirmary, guild))
            {
                continue;
            }

            var away = Math.Max(Math.Abs(infirmary.Stand.X - patient.X), Math.Abs(infirmary.Stand.Y - patient.Y));

            if (away > PatientReach || away >= nearest)
            {
                refusal = "far";

                continue;
            }

            if (infirmary.Waiting.Count >= MostWaiting)
            {
                refusal = "full";

                continue;
            }

            // Its healer is away for a moment: not sent a patient that would wait on an empty hall (life18b).
            if (infirmary.Away)
            {
                refusal = "away";

                continue;
            }

            if (!BotGates.Joined(patient.Map, patient.Location, infirmary.Stand))
            {
                refusal = "far";

                continue;
            }

            best = infirmary;
            nearest = away;
        }

        return best;
    }

    internal static void Arrive(BotInfirmary infirmary, BotMobile patient)
    {
        if (infirmary != null && patient != null && !infirmary.Waiting.Contains(patient))
        {
            infirmary.Waiting.Add(patient);
            infirmary.BusyTick = Core.TickCount;
        }
    }

    internal static void Leave(BotInfirmary infirmary, BotMobile patient)
    {
        if (infirmary != null && patient != null)
        {
            infirmary.Waiting.Remove(patient);
            infirmary.Seen.Remove(patient.Serial);
        }
    }

    /// <summary>
    /// The visit is over: the fee for what was healed, coin hand to hand, and the patient told. Returns the fee taken.
    /// </summary>
    internal static int Settle(BotInfirmary infirmary, BotMobile healer, BotMobile patient, int healed)
    {
        infirmary.Waiting.Remove(patient);
        infirmary.Seen.Add(patient.Serial);

        if (healed <= 0)
        {
            Unhealed++;

            return 0;
        }

        Patients++;
        HitsHealed += healed;
        infirmary.Healed++;

        var fee = Math.Max(FeeLeast, (int)Math.Ceiling(healed * Math.Max(0.0, FeePerHit)));

        fee = Math.Min(fee, BotYield.Spendable(patient));

        if (fee <= 0 || !healer.InRange(patient.Location, BotHandover.MeetTiles + 1) || !BotMarket.Charge(patient, fee))
        {
            Unpaid++;

            logger.Information("{Healer} healed {Patient} {Hits} points at {Infirmary} and was paid nothing: it could not pay", healer.Name, patient.Name, healed, infirmary.Name);

            return 0;
        }

        BotMarket.Pay(healer, fee);
        HealGold += fee;
        infirmary.Takings += fee;

        logger.Information(
            "{Healer} healed {Patient} the {Class} {Hits} points at {Infirmary} for {Fee}gp",
            healer.Name,
            patient.Name,
            patient.Class?.Name,
            healed,
            infirmary.Name,
            fee
        );

        var fill = new Dictionary<string, string> { ["fee"] = fee.ToString(), ["hits"] = healed.ToString() };

        BotCampTalk.Say(patient, "infirmary:thanks", null, healer, fill);

        return fee;
    }

    // ---- The fallen -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Whether the ordinary revival keeps its hands off this ghost because an infirmary's healer is on its way to it. One hook in
    /// <c>BotRaise.Holding</c>; at most <see cref="RaiseHoldMs"/> after the death, so a healer that never comes costs a minute.
    /// </summary>
    public static bool Holding(BotMobile ghost)
    {
        if (ghost == null)
        {
            return false;
        }

        // life19: a death near the seat with none open and a healer of the guild able to raise: held while one comes to open the hall.
        if (!_claims.ContainsKey(ghost.Serial) && _held.TryGetValue(ghost.Serial, out var fell))
        {
            return On && OnDemand && fell == ghost.FellTick && !ghost.Alive && Core.TickCount - fell < DemandHoldMs;
        }

        if (!_claims.TryGetValue(ghost.Serial, out var infirmary))
        {
            return false;
        }

        if (infirmary.Shut || !ReferenceEquals(infirmary.Call, ghost) || infirmary.CallFell != ghost.FellTick
            || Core.TickCount - ghost.FellTick >= RaiseHoldMs || ghost.Alive)
        {
            _claims.Remove(ghost.Serial);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Each open infirmary with no body to go to looks at the population once for a guildmate or ally fallen within
    /// <see cref="RaiseReach"/>, lately, that no company is raising, and that can pay. From the module's clock.
    /// </summary>
    internal static void Scan(long now)
    {
        if (!On || now - _scanTick < ScanMs)
        {
            return;
        }

        _scanTick = now;

        if (!_bootSaid && BotPopulation.Bots.Count > 0)
        {
            _bootSaid = true;
            logger.Information("Infirmaries at boot (life19c): {Raisers}", Raisers());
        }

        // A healer called away that has not come back in AwayMs: the infirmary shuts after all (life18b).
        for (var k = _open.Count - 1; k >= 0; k--)
        {
            var infirmary = _open[k];

            if (infirmary.Away && now - infirmary.AwayTick >= AwayMs)
            {
                AwayShut++;
                Close(infirmary, $"the healer did not come back in {AwayMs / 60000} minutes");
            }
        }

        // <b>Every fresh death looked at, whether or not an infirmary is open (life18b).</b> On build 416 the scan ran only while one
        // was open and walked away from any body it could not take without a word: 0 claimed in nine hours and no line saying why. Each
        // death is judged on every scan inside its claim window and its last reason counted once when the window closes.
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var ghost = bots[i];

            if (ghost is not { Deleted: false, Alive: false, Fallen: true } || _claims.ContainsKey(ghost.Serial)
                || now - ghost.FellTick >= (HeldFor(ghost) ? DemandHoldMs : ClaimWithinMs))
            {
                continue;
            }

            if (!_judging.TryGetValue(ghost.Serial, out var judging) || judging.Fell != ghost.FellTick)
            {
                // A new death: the last one's verdict, if it was never settled, is settled now.
                if (judging != null)
                {
                    Tally(judging.Cause);
                }

                judging = new Judging { Fell = ghost.FellTick };
                _judging[ghost.Serial] = judging;

                if (TraceTrigger && ghost.Guild is Guild seen && !BotUnderworld.Member(ghost))
                {
                    logger.Information(
                        "Infirmary trigger 1/4, death seen: {Name} the {Class} of {Guild} at ({X}, {Y}), {Open} infirmaries open on the island",
                        ghost.Name,
                        ghost.Class?.Name,
                        seen.Name,
                        ghost.X,
                        ghost.Y,
                        _open.Count
                    );
                }
            }

            if (judging.Cause == "claimed")
            {
                continue;
            }

            judging.Cause = Judge(ghost, now, judging, out var infirmary, out var fee);

            if (judging.Cause != "claimed")
            {
                continue;
            }

            if (_held.Remove(ghost.Serial))
            {
                HeldClaimed++;
            }

            Busy(infirmary);

            var healer = infirmary.Healer;

            infirmary.Call = ghost;
            infirmary.CallFell = ghost.FellTick;
            infirmary.CallFee = fee;
            _claims[ghost.Serial] = infirmary;
            Claimed++;

            logger.Information(
                "{Healer} leaves {Infirmary} to raise {Name} the {Class} where it fell at ({X}, {Y}), {Away} tiles off, for {Fee}gp",
                healer.Name,
                infirmary.Name,
                ghost.Name,
                ghost.Class?.Name,
                ghost.X,
                ghost.Y,
                (int)healer.GetDistanceToSqrt(ghost.Location),
                fee
            );

            BotVoice.Say(healer, "guild", $"{ghost.Name} is down at ({ghost.X}, {ghost.Y}). I'm going out to raise it.");
        }

        // Windows closed: each death's last verdict counted once.
        _settled.Clear();

        foreach (var (serial, judging) in _judging)
        {
            if (now - judging.Fell >= judging.Window)
            {
                _settled.Add(serial);
            }
        }

        for (var i = 0; i < _settled.Count; i++)
        {
            if (_judging.Remove(_settled[i], out var judging))
            {
                Tally(judging.Cause);
            }

            if (_held.Remove(_settled[i]))
            {
                HeldLapsed++;
            }
        }

        // Wants nobody answered in time are forgotten.
        _stale.Clear();

        foreach (var (guild, want) in _wants)
        {
            if (now - want.Tick >= DemandMs)
            {
                _stale.Add(guild);
            }
        }

        for (var i = 0; i < _stale.Count; i++)
        {
            _wants.Remove(_stale[i]);
        }
    }

    private static readonly List<Guild> _stale = [];

    private static bool HeldFor(BotMobile ghost) =>
        _held.TryGetValue(ghost.Serial, out var fell) && fell == ghost.FellTick;

    /// <summary>
    /// Why the nearest open infirmary of this ghost's guild or an ally's will not go out to it, or "claimed" with the infirmary and the
    /// fee when it will. The order is the order the reasons are worth reading in: whose it is, where, then the healer's means and the purse.
    /// </summary>
    private static string Judge(BotMobile ghost, long now, Judging judging, out BotInfirmary chosen, out int fee)
    {
        chosen = null;
        fee = 0;

        if (ghost.Guild is not Guild guild || BotUnderworld.Member(ghost))
        {
            return "guildless";
        }

        BotInfirmary nearest = null;
        var best = int.MaxValue;

        for (var k = 0; k < _open.Count; k++)
        {
            var infirmary = _open[k];

            if (infirmary.Shut || infirmary.Map != ghost.Map || !Treats(infirmary, guild))
            {
                continue;
            }

            var away = Math.Max(Math.Abs(infirmary.Stand.X - ghost.X), Math.Abs(infirmary.Stand.Y - ghost.Y));

            if (away < best)
            {
                best = away;
                nearest = infirmary;
            }
        }

        if (nearest == null)
        {
            return NoneOpen(ghost, guild, now, judging);
        }

        if (BotRaise.Unholy(ghost))
        {
            return "unholy";
        }

        if (!BotRaise.Takes(ghost))
        {
            return "ground";
        }

        if (BotDelveParty.Delving(ghost))
        {
            return "delving";
        }

        if (BotRaise.Awaited(ghost))
        {
            return "company";
        }

        if (best > RaiseReach || !BotGates.Joined(ghost.Map, ghost.Location, nearest.Stand))
        {
            return "far";
        }

        if (nearest.Away)
        {
            return "away";
        }

        if (nearest.Call != null || nearest.Healer is not { Deleted: false, Alive: true } healer)
        {
            return "busy";
        }

        var means = BotRaise.Best(healer, out _, out _);

        if (means == BotRaise.Means.None)
        {
            return "no means";
        }

        fee = FeeFor(means);

        if (BotYield.Spendable(ghost) < fee)
        {
            return "poor";
        }

        chosen = nearest;

        return "claimed";
    }

    /// <summary>
    /// A death with no infirmary of its guild's or an ally's open (life19): the reasons a hall could not raise it read first; then where it
    /// lies against its own seat, judged once a death. Near the seat with a healer able to raise: the guild's want is raised and the
    /// ordinary revival held <see cref="DemandHoldMs"/> while the healer comes (<see cref="OnDemand"/>).
    /// </summary>
    private static string NoneOpen(BotMobile ghost, Guild guild, long now, Judging judging)
    {
        var early = NoneOpenEarly(ghost);

        if (early != null)
        {
            if (TraceTrigger && !judging.Traced)
            {
                judging.Traced = true;
                logger.Information("Infirmary trigger 2/4, no call: {Name} of {Guild} is {Why}", ghost.Name, guild.Name, early switch
                {
                    "unholy" => "a criminal or a murderer",
                    "ground" => "on ground that takes no body",
                    "delving" => "in a delve",
                    _ => "awaited by its company's raiser"
                });
            }

            return early;
        }

        return NoneOpenNear(ghost, guild, now, judging);
    }

    private static string NoneOpenEarly(BotMobile ghost)
    {
        if (BotRaise.Unholy(ghost))
        {
            return "unholy";
        }

        if (!BotRaise.Takes(ghost))
        {
            return "ground";
        }

        if (BotDelveParty.Delving(ghost))
        {
            return "delving";
        }

        if (BotRaise.Awaited(ghost))
        {
            return "company";
        }

        return null;
    }

    private static string NoneOpenNear(BotMobile ghost, Guild guild, long now, Judging judging)
    {
        if (judging.NoneCause == null)
        {
            var seat = Seat(guild);

            if (TraceTrigger)
            {
                logger.Information(
                    "Infirmary trigger 2/4, seat: {Name} of {Guild} lies {Away} tiles from its seat at ({X}, {Y}): {Verdict}",
                    ghost.Name,
                    guild.Name,
                    seat == Point3D.Zero ? -1 : Math.Max(Math.Abs(seat.X - ghost.X), Math.Abs(seat.Y - ghost.Y)),
                    seat.X,
                    seat.Y,
                    seat == Point3D.Zero ? "the guild has no seat, no call"
                    : !Utility.InRange(seat, ghost.Location, RaiseReach) ? $"beyond {RaiseReach}, no call"
                    : !BotGates.Joined(ghost.Map, ghost.Location, seat) ? "on ground no road joins to the seat, no call"
                    : !On || !OnDemand ? "within reach, but the shift on demand is off"
                    : $"within {RaiseReach}, a healer is looked for"
                );
            }

            if (seat == Point3D.Zero || !Utility.InRange(seat, ghost.Location, RaiseReach) || !BotGates.Joined(ghost.Map, ghost.Location, seat))
            {
                judging.NoneCause = "none open far";

                // How far, so the clause says whether a wider reach would change anything (life19b).
                var away = seat == Point3D.Zero ? int.MaxValue : Math.Max(Math.Abs(seat.X - ghost.X), Math.Abs(seat.Y - ghost.Y));

                if (away <= 300)
                {
                    FarTo300++;
                }
                else if (away <= 600)
                {
                    FarTo600++;
                }
                else
                {
                    FarBeyond++;
                }
            }
            else if (!On || !OnDemand)
            {
                judging.NoneCause = Raiser(guild, seat, ghost.Map) ? "none open near" : "none open no raiser";
            }
            else
            {
                // <b>The death is the trigger (life19b).</b> On build 420 no shift ever opened on demand: the want waited for a healer's
                // own auction. Now the nearest guild healer able to raise is pressed into the shift at once, opens the hall from where
                // it stands, and the scan claims this body for it; the ordinary revival is held DemandHoldMs meanwhile.
                var outcome = Call(guild, seat, ghost.Map, ghost, true, RaiseFee, now);

                if (outcome == "called" || outcome == "already called")
                {
                    judging.NoneCause = "none open near";

                    if (!_held.ContainsKey(ghost.Serial))
                    {
                        _held[ghost.Serial] = ghost.FellTick;
                        judging.Window = DemandHoldMs;
                        Held++;
                        WantsFallen++;
                    }
                }
                else
                {
                    judging.NoneCause = outcome == "no healer" ? "none open no raiser" : "none open busy";
                }
            }
        }

        return judging.NoneCause;
    }

    /// <summary>What a raising costs the raised: <see cref="RaiseFee"/>, or a scroll's market price and <see cref="RaiseMarkup"/> when it is read off one.</summary>
    public static int FeeFor(BotRaise.Means means)
    {
        if (means != BotRaise.Means.Scroll)
        {
            return RaiseFee;
        }

        var scroll = BotMarket.Worth(typeof(ResurrectionScroll), BotGrimoire.ShopPrice(BotGrimoire.Circle(BotRaise.Spell)));

        return Math.Max(RaiseFee, (int)Math.Ceiling(scroll * Math.Max(1.0, RaiseMarkup)));
    }

    private static void Tally(string cause)
    {
        if (cause == null)
        {
            return;
        }

        if (cause != "guildless")
        {
            Fell++;
        }

        switch (cause)
        {
            case "guildless": FellGuildless++; break;
            case "none open": FellNoneOpen++; break;
            case "none open near": FellNoneOpen++; FellNoneNear++; break;
            case "none open no raiser": FellNoneOpen++; FellNoneNoRaiser++; break;
            case "none open busy": FellNoneOpen++; FellNoneBusy++; break;
            case "none open far": FellNoneOpen++; FellNoneFar++; break;
            case "far": FellFar++; break;
            case "no means": FellNoMeans++; break;
            case "busy": FellBusy++; break;
            case "away": FellAway++; break;
            case "company": FellCompany++; break;
            case "delving": FellDelving++; break;
            case "unholy": FellUnholy++; break;
            case "ground": FellGround++; break;
            case "poor": FellPoor++; break;
        }
    }

    /// <summary>Whether a company's call leaves this deed's bot at its infirmary. See <see cref="SpareKeepers"/>; one hook in <c>BotSquads.Pressable</c>.</summary>
    public static bool Spares(BotDeed deed)
    {
        if (!On || !SpareKeepers || deed is not BotKeepInfirmary { Open: true })
        {
            return false;
        }

        Spared++;

        return true;
    }

    /// <summary>Whether this bot keeps an open infirmary now: passed over by a company's search for a medic (<c>BotBand.LevyMedic</c>).</summary>
    public static bool Keeping(Mobile body) =>
        On && SpareKeepers && body is BotMobile { Resolve.Deed: BotKeepInfirmary { Open: true } };

    /// <summary>
    /// Whether this bot stocks a resurrection scroll for its guild's infirmary on the lower keep (<see cref="ScrollKeep"/>, read against all
    /// it may spend rather than its pocket): a Healer of a guild, with the infirmaries on. One hook in <c>BotLifeline</c>.
    /// </summary>
    public static bool Stocks(Mobile body) =>
        On && body is BotMobile { Class.Name: "Healer" } healer && healer.Guild is Guild && !BotUnderworld.Member(healer);

    internal static void WentAway(BotInfirmary infirmary)
    {
        if (infirmary == null || infirmary.Shut || infirmary.Away)
        {
            return;
        }

        infirmary.Away = true;
        infirmary.AwayTick = Core.TickCount;
        Aways++;

        if (infirmary.Call != null)
        {
            EndCall(infirmary, true);
        }
    }

    internal static void CameBack(BotInfirmary infirmary)
    {
        if (infirmary is { Away: true, Shut: false })
        {
            infirmary.Away = false;
            Returns++;
        }
    }

    /// <summary>The healer is done with its call, raised or not.</summary>
    internal static void EndCall(BotInfirmary infirmary, bool gaveUp)
    {
        if (infirmary?.Call == null)
        {
            return;
        }

        _claims.Remove(infirmary.Call.Serial);

        if (gaveUp)
        {
            if (infirmary.Call.Alive)
            {
                Beaten++;
            }
            else
            {
                GaveUp++;
            }
        }

        infirmary.Call = null;
    }

    /// <summary>The body is up, its corpse gone through: the fee, coin hand to hand, and the tally.</summary>
    internal static void Rose(BotInfirmary infirmary, BotMobile healer, BotMobile ghost, BotRaise.Means means)
    {
        Raised++;
        infirmary.Raised++;

        var fee = Math.Min(infirmary.CallFee > 0 ? infirmary.CallFee : RaiseFee, BotYield.Spendable(ghost));

        if (fee > 0 && BotMarket.Charge(ghost, fee))
        {
            BotMarket.Pay(healer, fee);

            // The raised bot's work in hand did not lose the fee, the healer's shift earned it.
            BotYield.Aside(ghost, fee);
            RaiseGold += fee;
            infirmary.Takings += fee;
        }
        else
        {
            fee = 0;
            Unpaid++;
        }

        logger.Information(
            "{Healer} raised {Name} the {Class} where it fell, with {Means}, from {Infirmary}, for {Fee}gp",
            healer.Name,
            ghost.Name,
            ghost.Class?.Name,
            BotRaise.Said(means),
            infirmary.Name,
            fee
        );

        BotEvents.Post("raise", ghost, $"raised where it fell by {healer.Name} of {infirmary.Name} for {fee}gp");

        var fill = new Dictionary<string, string> { ["fee"] = fee.ToString() };

        BotCampTalk.Say(ghost, "infirmary:raised", null, healer, fill);
    }

    // ---- Words --------------------------------------------------------------------------------------------------------------

    public static Dictionary<string, string[]> Phrases() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["took:infirmary"] = ["I'll keep the infirmary for a while.", "Anybody hurt, come to the hall.", "Bandages ready. The infirmary's open."],
            ["finished:infirmary"] = ["That's the infirmary closed.", "Enough patching for one shift."],
            ["took:infirmary visit"] = ["I need patching up.", "Off to the infirmary.", "Somebody bind this, please."],
            ["infirmary:open"] = ["The infirmary at the hall is open. Hurt? Come in.", "{guild}: bandages and healing at the hall, for a small fee."],
            ["infirmary:thanks"] = ["Much better. {fee} gold well spent.", "Thank you, {other}.", "Good as new, nearly."],
            ["infirmary:raised"] = ["Back from the dead. Thank you, {other}.", "I owe you, {other}. {fee} gold, and my life.", "Up again. Where's my pack?"]
        };

    public static string Describe() =>
        $"infirmaries {_open.Count} open now: {Opened} opened, {Closed} shut; {Patients} patients healed {HitsHealed} points for {HealGold}gp, {Unhealed} left unhealed; "
        + $"{Claimed} fallen claimed, {Raised} raised where they fell for {RaiseGold}gp, {GaveUp} given up to the ordinary revival, {Beaten} risen before the healer came; {Unpaid} could not pay; "
        + $"{Fell} deaths of guilds judged for a raising ({FellGuildless} guildless left out): {FellNoneOpen} with no infirmary of theirs or an ally's open (the trigger had its turn on each before that verdict: {FellNoneNear} called a healer, {FellNoneBusy} found the able ones busy, {FellNoneNoRaiser} near the seat with none able to raise, {FellNoneFar} too far from the seat to call), {FellFar} over {RaiseReach} tiles from it, "
        + $"{FellNoMeans} whose healer had nothing to raise with (no scroll at {BotRaise.StockChance:P0}, no book, no 80/80 bandage), {FellBusy} found it out on another body, {FellAway} found it away, "
        + $"{FellCompany} left to their company, {FellDelving} delving, {FellUnholy} criminals or murderers, {FellGround} on ground that takes no body, {FellPoor} could not pay the fee; "
        + $"healers called away {Aways} times: {Returns} came back, {AwayShut} shut for not coming back in {AwayMs / 60000} minutes; {Spared} company calls passed a healer at its infirmary; "
        + $"{Demand()}; raisers: {Raisers()}; "
        + $"{BotInfirmarer.Describe()}; {BotPatienter.Describe()}";

    /// <summary>The shift on demand's clause (life19): wants, the deaths with none open by where, holds, shifts and the healer's time.</summary>
    private static string Demand() =>
        !OnDemand
            ? $"shifts standing, not on demand; {MinutesOpen:F0} minutes kept open, {MinutesEmpty:F0} of them by shifts that saw nobody"
            : $"shifts on demand ({DemandMinutes:F0} minutes at least, shut after {IdleMs / 60000} idle): {OpenedOnDemand} opened on demand, {IdleShut} shut idle; "
              + $"wants raised: {WantsHurt} by a wounded bot that could not mend itself near its seat, {WantsFallen} by a death near its seat; "
              + $"of the deaths with none open, {FellNoneNear} within {RaiseReach} tiles of the seat with a healer called, {FellNoneBusy} near it with its able healers busy, {FellNoneNoRaiser} near it with none able, {FellNoneFar} farther ({FarTo300} within 300, {FarTo600} within 600, {FarBeyond} beyond or seatless); "
              + $"calls to open: {Triggers} ({TriggerCalled} healers pressed into a shift: {CalledFallen} by a death, {CalledHurt} by a wound; {TriggerOpen} found it open, {TriggerAgain} already called, {TriggerBusy} found the able healers busy{BusyKinds()}, {TriggerNoHealer} found none able); "
              + $"{Held} revivals held up to {DemandHoldMs / 1000}s for a healer: {HeldClaimed} claimed, {HeldLapsed} lapsed; "
              + $"{MinutesOpen:F0} minutes kept open, {MinutesEmpty:F0} of them by shifts that saw nobody";

    /// <summary>
    /// life19c: how many guilds have a Healer able to raise at all (a resurrection scroll and the Magery for it, the book and herbs, or a
    /// bandage at 80 Healing and 80 Anatomy). Zero makes the whole raising path moot, and the line says so. A walk of the population.
    /// </summary>
    public static string Raisers()
    {
        var bots = BotPopulation.Bots;
        var guilds = new HashSet<Guild>();
        var able = new HashSet<Guild>();
        int healers = 0, scroll = 0, book = 0, cloth = 0, magery = 0, master = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is not { Deleted: false } bot || bot.Guild is not Guild guild || BotUnderworld.Member(bot))
            {
                continue;
            }

            guilds.Add(guild);

            if (bot.Class?.Name != "Healer")
            {
                continue;
            }

            healers++;

            if (bot.Skills[SkillName.Magery].Value >= 50.0)
            {
                magery++;
            }

            if (bot.Skills[SkillName.Healing].Value >= 80.0 && bot.Skills[SkillName.Anatomy].Value >= 80.0)
            {
                master++;
            }

            switch (BotRaise.Best(bot, out _, out _))
            {
                case BotRaise.Means.Scroll: scroll++; able.Add(guild); break;
                case BotRaise.Means.Book: book++; able.Add(guild); break;
                case BotRaise.Means.Bandage: cloth++; able.Add(guild); break;
            }
        }

        var text = $"{able.Count} of {guilds.Count} guilds have a Healer able to raise now ({scroll + book + cloth} of {healers} guild Healers: {scroll} by a scroll, {book} by the book, {cloth} by an 80/80 bandage; {magery} have Magery 50+, {master} have 80 Healing and 80 Anatomy)";

        return able.Count == 0 ? $"{text}: NO guild can raise, so raising where they fell is moot until one can" : text;
    }

    private static bool _bootSaid;

    private static string BusyKinds()
    {
        if (_busyKinds.Count == 0)
        {
            return "";
        }

        List<string> parts = [];

        foreach (var (kind, count) in _busyKinds)
        {
            parts.Add($"{kind} {count}");
        }

        return $" ({string.Join(", ", parts)})";
    }

    public static void Forget()
    {
        for (var i = 0; i < _open.Count; i++)
        {
            _open[i].Shut = true;
        }

        _called.Clear();
        _busyKinds.Clear();
        FellNoneBusy = 0;
        FarTo300 = 0;
        FarTo600 = 0;
        FarBeyond = 0;
        Triggers = 0;
        TriggerCalled = 0;
        CalledFallen = 0;
        CalledHurt = 0;
        TriggerOpen = 0;
        TriggerAgain = 0;
        TriggerBusy = 0;
        TriggerNoHealer = 0;

        _wants.Clear();
        _held.Clear();
        WantsHurt = 0;
        WantsFallen = 0;
        FellNoneNear = 0;
        FellNoneNoRaiser = 0;
        FellNoneFar = 0;
        Held = 0;
        HeldClaimed = 0;
        HeldLapsed = 0;
        OpenedOnDemand = 0;
        IdleShut = 0;
        MinutesOpen = 0;
        MinutesEmpty = 0;

        _open.Clear();
        _claims.Clear();
        Opened = 0;
        Closed = 0;
        Patients = 0;
        HitsHealed = 0;
        HealGold = 0;
        Unhealed = 0;
        Unpaid = 0;
        Claimed = 0;
        Raised = 0;
        RaiseGold = 0;
        RaiseThings = 0;
        GaveUp = 0;
        Beaten = 0;
        _judging.Clear();
        Fell = 0;
        FellGuildless = 0;
        FellNoneOpen = 0;
        FellFar = 0;
        FellNoMeans = 0;
        FellBusy = 0;
        FellAway = 0;
        FellCompany = 0;
        FellDelving = 0;
        FellUnholy = 0;
        FellGround = 0;
        FellPoor = 0;
        Aways = 0;
        Returns = 0;
        AwayShut = 0;
        Spared = 0;
    }
}

/// <summary>
/// A healer's shift keeping its guild's infirmary: the walk to the seat, the patients bound or healed one at a time, a fallen
/// guildmate gone out to and raised where it fell, and the shift's end (07.10.2026, trades17). Paid in fees, measured as coin.
/// </summary>
public sealed class BotKeepInfirmary : BotDeed
{
    public const string Trade = "infirmary";

    private readonly Map _map;

    private readonly Guild _guild;

    private readonly Point3D _seat;

    private readonly double _claim;

    private BotInfirmary _infirmary;

    private double _openBefore;

    private long _openedTick;

    // The patient in hand.
    private BotMobile _patient;

    private int _startHits;

    private int _tries;

    private long _triedTick;

    private bool _tried;

    private bool _awaiting;

    // The body gone out to.
    private BotRaise.Means _means;

    private double _chance;

    private int _raiseTries;

    private long _wound;

    private bool _wounding;

    private long _callTick;

    private readonly bool _onDemand;

    /// <summary>Called out by a death (life19b): the hall is opened at the seat from where the healer stands, so the body is claimed at once.</summary>
    private readonly bool _atOnce;

    public BotKeepInfirmary(Map map, Guild guild, Point3D seat, double claim, bool onDemand = false, bool atOnce = false)
    {
        _map = map;
        _guild = guild;
        _seat = seat;
        _claim = Math.Max(0.01, claim);
        _onDemand = onDemand;
        _atOnce = atOnce;
    }

    /// <summary>The shift's length: the standing twenty minutes, or the on-demand few (life19).</summary>
    private double Shift => _onDemand ? Math.Max(1.0, BotInfirmaries.DemandMinutes) : BotInfirmaries.ShiftMinutes;

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _seat;

    public override double Expects => _claim;

    public override double Minutes => Shift;

    public override double Coin => 1.0;

    public override SkillName? Trains => SkillName.Healing;

    public override bool Still => _infirmary != null && _infirmary.Call == null;

    public override bool Committed => _infirmary != null;

    public override bool Steadfast => true;

    /// <summary>Open and its healer at it: what a company's call leaves alone (<see cref="BotInfirmaries.Spares"/>).</summary>
    public bool Open => _infirmary is { Shut: false, Away: false };

    public override double HoldsFor => _infirmary != null ? Math.Max(1.0, Shift - OpenMinutes(Core.TickCount)) + 1.0 : 0.0;

    public override string Stage =>
        _infirmary == null
            ? $"going to {_guild?.Name}'s seat to keep its infirmary"
            : _infirmary.Call != null
                ? $"going out from {_infirmary.Name} to raise {_infirmary.Call.Name}"
                : $"keeping {_infirmary.Name}: {_infirmary.Waiting.Count} waiting, {_infirmary.Healed} healed, {_infirmary.Raised} raised, {_infirmary.Takings}gp";

    private double OpenMinutes(long now) => _openBefore + (_infirmary != null ? (now - _openedTick) / 60000.0 : 0.0);

    public override void Taken(IBotWilful bot)
    {
        if (bot?.Self is { } body)
        {
            BotInfirmarer.Went(body);
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || _map == null || body.Map != _map)
        {
            return BotDoing.Failed("no body, or not on the seat's map");
        }

        var now = Core.TickCount;

        if (!BotInfirmaries.On)
        {
            return End(now, "the infirmaries were shut by order");
        }

        if (!ReferenceEquals(body.Guild, _guild))
        {
            return End(now, "it is no longer of the guild");
        }

        if (_infirmary is { Shut: true })
        {
            _openBefore += (now - _openedTick) / 60000.0;
            _infirmary = null;
        }

        // Back from whatever called it away: the infirmary it left waiting is open again (life18b).
        if (_infirmary is { Away: true })
        {
            BotInfirmaries.CameBack(_infirmary);
        }

        if (_infirmary == null && _atOnce && _openBefore <= 0.0 && BotInfirmaries.Of(_guild) == null)
        {
            _infirmary = BotInfirmaries.OpenAt(body, _guild, _map, true, _seat);

            if (_infirmary != null)
            {
                _openedTick = now;

                return BotDoing.Work(Stage);
            }
        }

        if (_infirmary == null)
        {
            if (!body.InRange(_seat, 2))
            {
                return BotDoing.Walk(_map, _seat, BotArrival.Within(2), $"to {_guild?.Name}'s seat to keep its infirmary");
            }

            if (BotInfirmaries.Of(_guild) is { } other && !ReferenceEquals(other.Healer, body))
            {
                return BotDoing.Failed($"{other.Healer?.Name} keeps {_guild?.Name}'s infirmary already");
            }

            _infirmary = BotInfirmaries.OpenAt(body, _guild, _map, _onDemand);

            if (_infirmary == null)
            {
                return BotDoing.Failed($"{_guild?.Name}'s infirmary could not open");
            }

            _openedTick = now;

            return BotDoing.Work(Stage);
        }

        if (_infirmary.Call != null)
        {
            return Raising(body, now);
        }

        if (OpenMinutes(now) >= BotInfirmaries.ShiftMinutes)
        {
            return End(now, "the shift is over");
        }

        // life19: an on-demand shift shuts once nobody needs it: kept its few minutes, nobody waiting or in hand, nobody sent lately.
        if (_onDemand && OpenMinutes(now) >= BotInfirmaries.DemandMinutes && _patient == null && _infirmary.Waiting.Count == 0
            && now - _infirmary.BusyTick >= BotInfirmaries.IdleMs)
        {
            BotInfirmaries.NoteIdleShut();

            return End(now, $"nobody of the guild hurt past mending or fallen within reach for {BotInfirmaries.IdleMs / 60000} minutes");
        }

        if (!body.InRange(_infirmary.Stand, 3))
        {
            return BotDoing.Walk(_map, _infirmary.Stand, BotArrival.Within(1), $"back to {_infirmary.Name}");
        }

        return Healing(body, now);
    }

    private BotDoing End(long now, string why)
    {
        var infirmary = _infirmary;
        var minutes = OpenMinutes(now);

        Release();

        if (infirmary == null)
        {
            return BotDoing.Done(why);
        }

        BotInfirmaries.Close(infirmary, why);

        return BotDoing.Done($"{why}: kept {infirmary.Name} {minutes:F1} minutes, healed {infirmary.Healed}, raised {infirmary.Raised}, {infirmary.Takings}gp in fees");
    }

    private void Release()
    {
        if (_patient != null && _infirmary != null)
        {
            BotInfirmaries.Leave(_infirmary, _patient);
        }

        _patient = null;
        _awaiting = false;
        _tried = false;
    }

    // ---- A patient --------------------------------------------------------------------------------------------------------

    private BotDoing Healing(BotMobile body, long now)
    {
        if (_patient == null)
        {
            while (_infirmary.Waiting.Count > 0 && _patient == null)
            {
                var next = _infirmary.Waiting[0];

                if (next is { Deleted: false, Alive: true } && next.Map == _map && body.InRange(next.Location, BotHandover.MeetTiles + 1))
                {
                    _patient = next;
                    _startHits = next.Hits;
                    _tries = 0;
                    _tried = false;
                    _awaiting = false;
                }
                else
                {
                    _infirmary.Waiting.RemoveAt(0);
                }
            }

            if (_patient == null)
            {
                return BotDoing.Work(Stage);
            }
        }

        var patient = _patient;

        if (patient is not { Deleted: false, Alive: true } || patient.Map != _map || !body.InRange(patient.Location, BotHandover.MeetTiles + 1))
        {
            BotInfirmaries.Leave(_infirmary, patient);
            _patient = null;

            return BotDoing.Work(Stage);
        }

        if (BotMend.Whole(patient) || _tries >= BotInfirmaries.HealTries && !BotMend.Winding(body) && body.Spell == null)
        {
            BotInfirmaries.Settle(_infirmary, body, patient, Math.Max(0, patient.Hits - _startHits));
            _patient = null;

            return BotDoing.Work(Stage);
        }

        // The click a bot has no client to make: a heal of ours come round to its target. See BotSalve.
        if (_awaiting && body.Target != null)
        {
            _awaiting = false;
            BotMend.Aim(body, patient);

            return BotDoing.Work($"healing {patient.Name}");
        }

        if (body.Spell != null)
        {
            return BotDoing.Work("casting");
        }

        if (BotMend.Winding(body))
        {
            return BotDoing.Work($"binding {patient.Name}");
        }

        if (_tried && now - _triedTick < BotSalve.TryMs)
        {
            return BotDoing.Work($"healing {patient.Name}");
        }

        _tried = true;
        _triedTick = now;
        _tries++;

        if (BotMend.Cloth(body) > 0 && BotMend.Wind(body, patient))
        {
            return BotDoing.Work($"binding {patient.Name}");
        }

        var spell = BotMend.Spell(body, patient);

        if (spell >= 0 && BotMend.Begin(body, spell))
        {
            _awaiting = true;

            return BotDoing.Work($"casting on {patient.Name}");
        }

        // Nothing left to give it: what it got is what it pays for.
        _tries = BotInfirmaries.HealTries;

        return BotDoing.Work($"healing {patient.Name}");
    }

    // ---- The fallen -------------------------------------------------------------------------------------------------------

    private BotDoing Raising(BotMobile body, long now)
    {
        var ghost = _infirmary.Call;

        // Up already — the ordinary revival, or a company — or the hold is over: back to the infirmary.
        if (ghost is not { Deleted: false } || ghost.Alive || ghost.FellTick != _infirmary.CallFell || !BotInfirmaries.Holding(ghost)
            || ghost.Map != _map)
        {
            BotInfirmaries.EndCall(_infirmary, true);
            ResetCall();

            return BotDoing.Work(Stage);
        }

        if (_means == BotRaise.Means.None)
        {
            _means = BotRaise.Best(body, out _chance, out _);
            _callTick = now;

            if (_means == BotRaise.Means.None)
            {
                BotInfirmaries.EndCall(_infirmary, true);
                ResetCall();

                return BotDoing.Work(Stage);
            }
        }

        if (_raiseTries >= BotRaise.MaxTries)
        {
            BotInfirmaries.EndCall(_infirmary, true);
            ResetCall();

            return BotDoing.Work(Stage);
        }

        if (!body.InRange(ghost.Location, 1) || !body.InLOS(ghost))
        {
            return BotDoing.Walk(_map, ghost.Location, BotArrival.Beside, $"to raise {ghost.Name}");
        }

        // A ghost is seen by the living only when it manifests (Mobile.CanSee): what a player does to be raised, the bot does too.
        ghost.Warmode = true;

        if (_means == BotRaise.Means.Bandage)
        {
            return Wind(body, ghost, now);
        }

        // The cursor is up: aimed, and the engine's spending read for whether it took (BotSquad.Work's reading).
        if (body.Target is { } cursor && body.Spell is ResurrectionSpell cast)
        {
            var used = cast.Scroll;
            var before = used?.Amount ?? 0;
            var mana = body.Mana;

            cursor.Invoke(body, ghost);

            var took = used != null ? used.Deleted || used.Amount < before : body.Mana <= mana - BotRaise.Mana / 2;

            _raiseTries++;
            BotRaise.NoteTry(took);

            if (took)
            {
                Stand(body, ghost);
            }

            return BotDoing.Work($"raising {ghost.Name}");
        }

        if (body.Spell != null)
        {
            return BotDoing.Work("casting");
        }

        if (body.Target is { } foreign)
        {
            foreign.Cancel(body, TargetCancelType.Canceled);

            return BotDoing.Work($"raising {ghost.Name}");
        }

        if (body.Mana < BotRaise.Mana)
        {
            BotRaise.NoteManaWait();

            return BotDoing.Work($"waiting for the mana to raise {ghost.Name}");
        }

        var scroll = _means == BotRaise.Means.Scroll ? body.Backpack?.FindItemByType<ResurrectionScroll>() : null;

        if (_means == BotRaise.Means.Scroll && scroll == null)
        {
            _raiseTries = BotRaise.MaxTries;

            return BotDoing.Work($"raising {ghost.Name}");
        }

        var spell = SpellRegistry.NewSpell(BotRaise.Spell, body, scroll);

        if (spell != null && spell.Cast())
        {
            BotRaise.NoteCast();
        }
        else
        {
            _raiseTries++;
        }

        return BotDoing.Work($"raising {ghost.Name}");
    }

    /// <summary>The bandage of a master: one used, the engine's wait, the engine's roll (BotSquad.Wind's reading).</summary>
    private BotDoing Wind(BotMobile body, BotMobile ghost, long now)
    {
        if (!_wounding)
        {
            var cloth = body.Backpack?.FindItemByType<Bandage>();

            if (cloth == null)
            {
                _raiseTries = BotRaise.MaxTries;

                return BotDoing.Work($"raising {ghost.Name}");
            }

            cloth.Consume();
            _wounding = true;
            _wound = now + BotRaise.BandageMs(body);

            return BotDoing.Work($"binding {ghost.Name}'s body");
        }

        if (now - _wound < 0)
        {
            return BotDoing.Work($"binding {ghost.Name}'s body");
        }

        _wounding = false;
        _raiseTries++;

        var healing = body.Skills[SkillName.Healing].Value;
        var anatomy = body.Skills[SkillName.Anatomy].Value;
        var took = body.InRange(ghost, Bandage.Range) && body.InLOS(ghost) && healing >= 80.0 && anatomy >= 80.0
                   && (healing - 68.0) / 50.0 > Utility.RandomDouble();

        BotRaise.NoteTry(took);

        if (took)
        {
            body.CheckSkill(SkillName.Anatomy, 0.0, 120.0);
            body.CheckSkill(SkillName.Healing, 0.0, 120.0);
            Stand(body, ghost);
        }

        return BotDoing.Work($"raising {ghost.Name}");
    }

    private void Stand(BotMobile body, BotMobile ghost)
    {
        var means = _means;

        if (BotRaise.Rise(ghost, body, means, _chance, _raiseTries, null))
        {
            BotInfirmaries.Rose(_infirmary, body, ghost, means);
            BotInfirmaries.EndCall(_infirmary, false);
        }
        else
        {
            BotInfirmaries.EndCall(_infirmary, true);
        }

        ResetCall();
    }

    private void ResetCall()
    {
        _means = BotRaise.Means.None;
        _chance = 0.0;
        _raiseTries = 0;
        _wounding = false;
    }

    public override bool Bend(IBotWilful bot)
    {
        // A body that cannot be walked to is given up to the ordinary revival; the shift goes on.
        if (_infirmary?.Call != null)
        {
            BotInfirmaries.EndCall(_infirmary, true);
            ResetCall();

            return true;
        }

        return false;
    }

    public override void Paused(IBotWilful bot)
    {
        Release();

        // <b>Waited for, not shut (07.10.2026, life18b).</b> Shut on every call away, the infirmaries of build 416 were open 2.1 minutes a
        // shift; the hall now waits <see cref="BotInfirmaries.AwayMs"/> for its healer, sends no new patient meanwhile, and gives a body
        // it had gone out to back to the ordinary revival.
        if (_infirmary != null)
        {
            BotInfirmaries.WentAway(_infirmary);
        }

        ResetCall();
    }

    public override void Drop(IBotWilful bot)
    {
        Release();

        if (_infirmary != null)
        {
            BotInfirmaries.Close(_infirmary, "the shift was given up");
            _infirmary = null;
        }

        ResetCall();
    }
}

/// <summary>
/// Offers a guild's healer — a bot that can bind with bandages or heal by spell — the shift at its guild's infirmary when the guild
/// has none open (07.10.2026, trades17). Every gate counted.
/// </summary>
public sealed class BotInfirmarer : IBotProposer
{
    public string Name => "Infirmarer";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>What a shift is worth a minute until this healer's own shifts say otherwise.</summary>
    public static double Prior { get; set; } = 20.0;

    public static long Looks { get; private set; }

    public static long NoGuild { get; private set; }

    public static long NoMeans { get; private set; }

    public static long Rested { get; private set; }

    public static long Kept { get; private set; }

    public static long Far { get; private set; }

    public static long Offered { get; private set; }

    /// <summary>life19: healers passed over for want of anybody needing the infirmary; and over a want of the fallen they could not raise.</summary>
    public static long NoWant { get; private set; }

    public static long CannotRaise { get; private set; }

    public static long OfferedOnDemand { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    internal static void Went(Mobile healer)
    {
        if (healer != null)
        {
            _last[healer.Serial] = Core.TickCount;
        }
    }

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotInfirmaries.On || bot?.Self is not BotMobile body || !body.Alive || body.Tired || body.Squad != null)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal || body.Resolve?.Deed is BotKeepInfirmary)
        {
            return null;
        }

        // Only a healer by trade: the class whose kit is the bandage and the heal. Everybody else binds its own.
        if (body.Class?.Name != "Healer")
        {
            return null;
        }

        Looks++;

        if (body.Guild is not Guild guild || BotUnderworld.Member(body))
        {
            NoGuild++;

            return null;
        }

        var now = Core.TickCount;

        if (_last.TryGetValue(body.Serial, out var when) && now - when < (BotInfirmaries.OnDemand ? BotInfirmaries.DemandRestMs : BotInfirmaries.RestMs))
        {
            Rested++;

            return null;
        }

        if (BotInfirmaries.Of(guild) != null)
        {
            Kept++;

            return null;
        }

        // life19: on demand, only while somebody of the guild needs the hall within reach of the seat.
        var worth = 0;
        var wantsHurt = false;

        if (BotInfirmaries.OnDemand)
        {
            if (!BotInfirmaries.Wanting(guild, now, out worth, out wantsHurt, out var wantsFallen))
            {
                NoWant++;

                return null;
            }

            if (!wantsHurt && wantsFallen && BotRaise.Best(body, out _, out _) == BotRaise.Means.None)
            {
                CannotRaise++;

                return null;
            }
        }

        if (BotMend.Cloth(body) < BotInfirmaries.LeastBandages && !BotMend.Herbs(body, BotArsenal.SpellHeal) && !BotMend.Herbs(body, BotArsenal.SpellGreaterHeal)
            && (!BotInfirmaries.OnDemand || wantsHurt))
        {
            NoMeans++;

            return null;
        }

        var seat = BotInfirmaries.Seat(guild);

        if (seat == Point3D.Zero || !Utility.InRange(body.Location, seat, BotInfirmaries.KeeperReach) || !BotGates.Joined(map, body.Location, seat))
        {
            Far++;

            return null;
        }

        Offered++;

        if (!BotInfirmaries.OnDemand)
        {
            return new BotKeepInfirmary(map, guild, seat, Prior);
        }

        // The claim: the shift's prior and the fees the want is worth over the short shift (a raise's fee, a wound's points).
        OfferedOnDemand++;

        return new BotKeepInfirmary(map, guild, seat, Prior + worth / Math.Max(1.0, BotInfirmaries.DemandMinutes), true);
    }

    public static string Describe() =>
        Looks == 0
            ? "no healer looked at for an infirmary"
            : $"{Looks} healers looked at for an infirmary: {Offered} offered, {NoGuild} had no guild, {Rested} were resting from one, {Kept} found their guild's open, "
              + $"{NoMeans} had under {BotInfirmaries.LeastBandages} bandages and no heal, {Far} were over {BotInfirmaries.KeeperReach} tiles from the seat, "
              + $"{NoWant} found nobody of the guild needing it, {CannotRaise} were wanted for a body they could not raise, {OfferedOnDemand} of the offers on demand";

    public static void Forget()
    {
        _last.Clear();
        Looks = 0;
        NoGuild = 0;
        NoMeans = 0;
        Rested = 0;
        Kept = 0;
        Far = 0;
        Offered = 0;
        NoWant = 0;
        CannotRaise = 0;
        OfferedOnDemand = 0;
    }
}

/// <summary>
/// A wounded bot's visit to its guild's infirmary: the walk, a place in the queue, the healer's work, the fee (07.10.2026,
/// trades17). Unpaid, so priced at its claim; <see cref="BotPatienter"/> asks every reason it would fail first.
/// </summary>
public sealed class BotTend : BotDeed
{
    public const string Trade = "infirmary visit";

    private readonly BotInfirmary _infirmary;

    private readonly double _claim;

    private long _began;

    private bool _queued;

    private long _queuedTick;

    private int _arrivedHits;

    public BotTend(BotInfirmary infirmary, double claim)
    {
        _infirmary = infirmary;
        _claim = claim;
    }

    public override string Kind => Trade;

    public override Map Map => _infirmary?.Map;

    public override Point3D Where => _infirmary?.Stand ?? Point3D.Zero;

    public override double Expects => _claim;

    public override double Minutes => 2.0;

    public override bool Unpaid => true;

    public override bool Still => _queued;

    public override bool ResumesHurt => true;

    public override string Stage => _queued ? $"waiting at {_infirmary?.Name}" : $"going to {_infirmary?.Name} to be healed";

    public override bool Repeats(BotDeed other) => other is BotTend tend && ReferenceEquals(tend._infirmary, _infirmary);

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;
        BotInfirmaries.Busy(_infirmary);

        if (bot?.Self is { } body)
        {
            BotPatienter.Went(body);
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || _infirmary == null || body.Map != _infirmary.Map)
        {
            return BotDoing.Failed("no body, or no infirmary on this map");
        }

        var now = Core.TickCount;
        var healer = _infirmary.Healer;

        if (_infirmary.Shut || healer is not { Deleted: false, Alive: true })
        {
            BotInfirmaries.Leave(_infirmary, body);

            return _queued ? BotDoing.Done($"{_infirmary.Name} shut while it waited") : BotDoing.Failed($"{_infirmary.Name} shut before it got there");
        }

        if (_infirmary.Seen.Contains(body.Serial))
        {
            BotInfirmaries.Leave(_infirmary, body);

            return BotDoing.Done($"seen at {_infirmary.Name}: {body.Hits - _arrivedHits} points back");
        }

        if (BotMend.Whole(body))
        {
            BotInfirmaries.Leave(_infirmary, body);

            return BotDoing.Done($"whole again before {healer.Name} could see it");
        }

        // <b>The hall, not the healer, while the healer is out (life18b).</b> A patient followed the healer itself, so one sent while it
        // went out to a body, or was called away, walked after it across the country. It waits at the infirmary instead.
        var home = !_infirmary.Away && _infirmary.Call == null && healer.InRange(_infirmary.Stand, 4);

        if (!home)
        {
            if (body.InRange(_infirmary.Stand, 3))
            {
                if (!_queued)
                {
                    _queued = true;
                    _queuedTick = now;
                    _arrivedHits = body.Hits;
                    body.Journey.Finish();
                }

                if (now - _queuedTick >= BotPatienter.WaitMs)
                {
                    BotInfirmaries.Leave(_infirmary, body);
                    BotPatienter.NoteTired();

                    return BotDoing.Failed($"waited {BotPatienter.WaitMs / 60000} minutes at {_infirmary.Name} and its healer did not come back");
                }

                return BotDoing.Work($"waiting at {_infirmary.Name} for {healer.Name} to come back");
            }

            if (now - _began >= BotPatienter.WalkMs)
            {
                BotInfirmaries.Leave(_infirmary, body);
                BotPatienter.NoteNeverGot();

                return BotDoing.Failed($"could not get to {_infirmary.Name} in {BotPatienter.WalkMs / 60000} minutes");
            }

            return BotDoing.Walk(_infirmary.Map, _infirmary.Stand, BotArrival.Within(2), $"to {_infirmary.Name}");
        }

        if (!body.InRange(healer.Location, BotHandover.MeetTiles))
        {
            if (_queued && now - _queuedTick < 5000)
            {
                return BotDoing.Work("waiting");
            }

            if (now - _began >= BotPatienter.WalkMs)
            {
                BotInfirmaries.Leave(_infirmary, body);
                BotPatienter.NoteNeverGot();

                return BotDoing.Failed($"could not get to {_infirmary.Name} in {BotPatienter.WalkMs / 60000} minutes");
            }

            return BotDoing.Walk(_infirmary.Map, healer, BotArrival.Within(BotHandover.MeetTiles), $"to {_infirmary.Name}");
        }

        if (!_queued)
        {
            _queued = true;
            _queuedTick = now;
            _arrivedHits = body.Hits;
            body.Journey.Finish();
        }

        // In the queue whenever it stands by the healer: a patient that stepped off and came back is queued again.
        BotInfirmaries.Arrive(_infirmary, body);

        if (now - _queuedTick >= BotPatienter.WaitMs)
        {
            BotInfirmaries.Leave(_infirmary, body);
            BotPatienter.NoteTired();

            return BotDoing.Failed($"waited {BotPatienter.WaitMs / 60000} minutes at {_infirmary.Name} and was not seen");
        }

        return BotDoing.Work($"waiting at {_infirmary.Name}");
    }

    public override void Drop(IBotWilful bot)
    {
        if (bot?.Self is BotMobile body)
        {
            BotInfirmaries.Leave(_infirmary, body);
        }
    }
}

/// <summary>
/// Offers a wounded bot that cannot mend itself a visit to its guild's or an ally's open infirmary (07.10.2026, trades17; life19).
///
/// <para>
/// <b>A bot that can bandage itself does; the infirmary is for those who cannot (life19).</b> On build 419 (2.3 h) 699 wounded looks sent
/// nobody: 341 were in a fight, a company or a delve and 358 "could heal themselves", the test being one bandage in the pack or a heal ready.
/// A bandage wound at 0–20 Healing heals at a tenth to a third of a try and cures no poison under 60 Healing and 60 Anatomy; such a bot
/// cannot really mend itself. Now: a bot mends itself when it has a heal or cure spell ready, a potion for what ails it, or bandages and
/// <see cref="SelfBindSkill"/> Healing (60/60 when poisoned); everybody else is a patient. Poison counts as a wound. With no infirmary of its
/// guild's or an ally's open and its seat within <see cref="BotInfirmaries.PatientReach"/>, it raises its guild's want, which is what opens
/// an on-demand shift (<see cref="BotInfirmaries.OnDemand"/>). Every look counted once by its first refusal, the means a self-mender has
/// split, and an offer not taken counted by what the bot held instead.
/// </para>
/// </summary>
public sealed class BotPatienter : IBotProposer
{
    public string Name => "Patient";

    public BotStanding Rung => BotStanding.Free;

    public static double Prior { get; set; } = 25.0;

    /// <summary>What a wound adds a minute, times the share of hits missing.</summary>
    public static double HurtWorth { get; set; } = 80.0;

    /// <summary>
    /// The share of its hits under which a bot is sent. Seven tenths (life18b; six before): of 719 wounded looked at on build 416, 205 could
    /// not heal themselves, and a bot at two thirds of its hits regains the rest at a point every eleven seconds.
    /// </summary>
    public static double HurtUnder { get; set; } = 0.7;

    /// <summary>
    /// The least Healing at which a bot's own bandages count as mending itself (life19): thirty, a bandage that takes four tries in ten. Zero
    /// restores trades17's rule (one bandage in the pack is enough).
    /// </summary>
    public static double SelfBindSkill { get; set; } = 30.0;

    public static int EveryMs { get; set; } = 600000;

    public static int WalkMs { get; set; } = 240000;

    public static int WaitMs { get; set; } = 180000;

    public static long Asked { get; private set; }

    public static long SelfHealing { get; private set; }

    public static long TooSoon { get; private set; }

    public static long None { get; private set; }

    public static long Full { get; private set; }

    public static long Poor { get; private set; }

    public static long Offered { get; private set; }

    public static long Walked { get; private set; }

    public static long NeverGot { get; private set; }

    public static long Tired { get; private set; }

    /// <summary>Wounded passed over for a fight, a company or a delve in hand; and those whose infirmary's healer was away (life18b).</summary>
    public static long Engaged { get; private set; }

    public static long Away { get; private set; }

    // life19: the looks split finer.

    /// <summary>Of <see cref="Engaged"/>: at a fight, in a company, delving.</summary>
    public static long InFight { get; private set; }

    public static long InCompany { get; private set; }

    public static long InDelve { get; private set; }

    /// <summary>Of <see cref="SelfHealing"/>: by a spell, by bandages and the skill, by a potion; and of all of them, those with a hall of theirs open within reach.</summary>
    public static long SelfSpell { get; private set; }

    public static long SelfCloth { get; private set; }

    public static long SelfPotion { get; private set; }

    public static long SelfInReach { get; private set; }

    /// <summary>The wounded that could not mend themselves, by why: no bandage and no heal; bandages under the skill; poisoned with no cure.</summary>
    public static long Bare { get; private set; }

    public static long Unskilled { get; private set; }

    public static long Uncured { get; private set; }

    /// <summary>Of <see cref="None"/>: none of theirs open anywhere with the seat within reach (a want raised); none open and the seat far; open but too far.</summary>
    public static long NoneNearSeat { get; private set; }

    public static long NoneFarSeat { get; private set; }

    public static long OpenFar { get; private set; }

    /// <summary>Offers not taken: what the bot held at its next look instead ("nothing" = refused at the auction: a veto, a zero, or less than the rest).</summary>
    private static readonly Dictionary<string, long> _lostTo = [];

    private static readonly Dictionary<Serial, long> _offers = [];

    private static readonly Dictionary<Serial, long> _last = [];

    internal static void Went(Mobile who)
    {
        if (who != null)
        {
            _last[who.Serial] = Core.TickCount;
            _offers.Remove(who.Serial);
            Walked++;
        }
    }

    internal static void NoteNeverGot() => NeverGot++;

    internal static void NoteTired() => Tired++;

    /// <summary>
    /// How this bot can mend itself now, or null with why it cannot (life19): a spell ready (Cure first when poisoned), a potion for what ails
    /// it, or bandages with <see cref="SelfBindSkill"/> Healing — 60 Healing and 60 Anatomy for a poison.
    /// </summary>
    public static string Mends(BotMobile who, out string cannot)
    {
        cannot = null;

        var poisoned = who.Poisoned;
        var spell = BotMend.Spell(who, who);
        var cloth = BotMend.Cloth(who);
        var healing = who.Skills[SkillName.Healing].Value;

        if (poisoned)
        {
            if (spell == BotArsenal.SpellCure)
            {
                return "spell";
            }

            if (BotMend.Bottles(who, BotPotionKind.Cure) > 0)
            {
                return "potion";
            }

            if (cloth > 0 && healing >= 60.0 && who.Skills[SkillName.Anatomy].Value >= 60.0)
            {
                return "cloth";
            }

            cannot = "poisoned";

            return null;
        }

        if (spell >= 0)
        {
            return "spell";
        }

        if (cloth > 0 && healing >= SelfBindSkill)
        {
            return "cloth";
        }

        if (BotMend.Bottles(who, BotPotionKind.Heal) > 0)
        {
            return "potion";
        }

        cannot = cloth > 0 ? "unskilled" : "bare";

        return null;
    }

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotInfirmaries.On)
        {
            return null;
        }

        if (bot?.Self is not BotMobile who || !who.Alive || who.Map == null || who.Map == Map.Internal || who.Guild is not Guild guild)
        {
            return null;
        }

        // An offer at the last look not taken: counted by what the bot holds instead.
        if (_offers.Count > 0 && _offers.Remove(who.Serial))
        {
            var instead = who.Resolve?.Deed is { } held ? held.Kind : "nothing";

            if (who.Resolve?.Deed is not BotTend)
            {
                _lostTo[instead] = _lostTo.GetValueOrDefault(instead) + 1;
            }
        }

        if (who.HitsMax <= 0 || who.Hits >= who.HitsMax * HurtUnder && !who.Poisoned)
        {
            return null;
        }

        if (who.Resolve?.Deed is BotTend or BotKeepInfirmary || BotUnderworld.Member(who))
        {
            return null;
        }

        Asked++;

        if (who.Combatant != null || who.Squad != null || BotDelveParty.Delving(who))
        {
            Engaged++;

            if (who.Combatant != null)
            {
                InFight++;
            }
            else if (who.Squad != null)
            {
                InCompany++;
            }
            else
            {
                InDelve++;
            }

            return null;
        }

        var means = Mends(who, out var cannot);
        var now = Core.TickCount;

        if (means != null)
        {
            SelfHealing++;

            switch (means)
            {
                case "spell": SelfSpell++; break;
                case "cloth": SelfCloth++; break;
                default: SelfPotion++; break;
            }

            if (BotInfirmaries.Open.Count > 0 && BotInfirmaries.For(who, out _) != null)
            {
                SelfInReach++;
            }

            return null;
        }

        switch (cannot)
        {
            case "poisoned": Uncured++; break;
            case "unskilled": Unskilled++; break;
            default: Bare++; break;
        }

        if (_last.TryGetValue(who.Serial, out var when) && now - when < EveryMs)
        {
            TooSoon++;

            return null;
        }

        if (BotYield.Spendable(who) < BotInfirmaries.FeeLeast)
        {
            Poor++;

            return null;
        }

        var refusal = "none";
        var infirmary = BotInfirmaries.Open.Count > 0 ? BotInfirmaries.For(who, out refusal) : null;

        if (infirmary == null)
        {

            if (refusal == "full")
            {
                Full++;
            }
            else if (refusal == "away")
            {
                Away++;
            }
            else
            {
                None++;

                if (refusal == "far")
                {
                    OpenFar++;
                }
                else
                {
                    // None of its guild's or an ally's open: near its own seat, the want that opens an on-demand shift (life19).
                    var seat = BotInfirmaries.Seat(guild);

                    if (seat != Point3D.Zero && Utility.InRange(seat, who.Location, BotInfirmaries.PatientReach) && BotGates.Joined(who.Map, who.Location, seat))
                    {
                        NoneNearSeat++;

                        if (BotInfirmaries.OnDemand)
                        {
                            BotInfirmaries.NoteWantHurt();
                            BotInfirmaries.Wanted(guild, Fee(who), false, now);

                            // life19b: the wound is a trigger too: the nearest able healer of the guild is called to open the hall.
                            BotInfirmaries.Call(guild, seat, who.Map, who, false, Fee(who), now);
                        }
                    }
                    else
                    {
                        NoneFarSeat++;
                    }
                }
            }

            return null;
        }

        Offered++;
        _offers[who.Serial] = now;
        BotInfirmaries.Busy(infirmary);

        return new BotTend(infirmary, Prior + HurtWorth * (1.0 - (double)who.Hits / who.HitsMax));
    }

    /// <summary>What a visit would take in fees: the points missing at the fee a point, never under the least.</summary>
    private static int Fee(BotMobile who) =>
        Math.Max(BotInfirmaries.FeeLeast, (int)Math.Ceiling(Math.Max(0, who.HitsMax - who.Hits) * Math.Max(0.0, BotInfirmaries.FeePerHit)));

    /// <summary>"lost to: hunt 3, nothing 2" — offers not taken by what the bot held instead, the commonest first; and stale offers dropped.</summary>
    private static string LostTo()
    {
        var now = Core.TickCount;

        _stale.Clear();

        foreach (var (serial, tick) in _offers)
        {
            if (now - tick >= EveryMs)
            {
                _stale.Add(serial);
            }
        }

        for (var i = 0; i < _stale.Count; i++)
        {
            _offers.Remove(_stale[i]);
            _lostTo["not looked at again"] = _lostTo.GetValueOrDefault("not looked at again") + 1;
        }

        if (_lostTo.Count == 0)
        {
            return "no offer lost";
        }

        List<KeyValuePair<string, long>> rows = [.._lostTo];

        rows.Sort((a, b) => b.Value.CompareTo(a.Value));

        List<string> parts = [];

        for (var i = 0; i < rows.Count && i < 6; i++)
        {
            parts.Add($"{rows[i].Key} {rows[i].Value}");
        }

        return $"offers not taken, by what the bot held instead: {string.Join(", ", parts)}";
    }

    private static readonly List<Serial> _stale = [];

    public static string Describe() =>
        Asked == 0
            ? "no wounded looked at for an infirmary"
            : $"{Asked} wounded under {HurtUnder:P0} or poisoned looked at for an infirmary: {Offered} sent, {Walked} went; {Engaged} were in a fight, a company or a delve ({InFight} at a fight, {InCompany} in a company, {InDelve} delving), "
              + $"{SelfHealing} could mend themselves ({SelfSpell} by a spell, {SelfCloth} by bandages at {SelfBindSkill:F0}+ Healing, {SelfPotion} by a potion; {SelfInReach} of them with a hall of theirs open within reach); "
              + $"{Bare + Unskilled + Uncured} could not ({Bare} with no bandage and no heal, {Unskilled} with bandages under {SelfBindSkill:F0} Healing, {Uncured} poisoned with no cure): "
              + $"{TooSoon} within {EveryMs / 60000} minutes of their last, {Poor} could not pay {BotInfirmaries.FeeLeast}gp, {None} had none of their guild's or an ally's within {BotInfirmaries.PatientReach} tiles "
              + $"({NoneNearSeat} with none open and the seat within reach: a want raised, {NoneFarSeat} with none open and the seat far, {OpenFar} with one open but farther), {Full} found it full, {Away} found its healer away; "
              + $"{NeverGot} could not get there, {Tired} waited {WaitMs / 60000} minutes unseen; {LostTo()}";

    public static void Forget()
    {
        _last.Clear();
        _offers.Clear();
        _lostTo.Clear();
        Asked = 0;
        SelfHealing = 0;
        TooSoon = 0;
        None = 0;
        Full = 0;
        Poor = 0;
        Offered = 0;
        Walked = 0;
        NeverGot = 0;
        Tired = 0;
        Engaged = 0;
        Away = 0;
        InFight = 0;
        InCompany = 0;
        InDelve = 0;
        SelfSpell = 0;
        SelfCloth = 0;
        SelfPotion = 0;
        SelfInReach = 0;
        Bare = 0;
        Unskilled = 0;
        Uncured = 0;
        NoneNearSeat = 0;
        NoneFarSeat = 0;
        OpenFar = 0;
    }
}
