using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server.Guilds;
using Server.Json;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Politics inside guilds and towns, as one clock: the page's politics object, the phrases, and the seams other folders wire.
///
/// <para>
/// The four parts are <see cref="BotElection"/> (the head by vote, and the plot that deposes it), <see cref="BotPurpose"/>
/// (what the chest saves for, by vote), <see cref="BotCouncil"/> (the towns' posts, their income and the outcasts' surcharge)
/// and <see cref="BotTribute"/> (ultimatums, and tribute instead of war). Patrick's order of 07.10.2026, night.
/// </para>
///
/// <para>
/// <b>Three seams are left for other folders, wired at merge.</b> <see cref="TiesRegard"/> (what one bot thinks of another —
/// <c>BotTies.Regard</c>) and <see cref="Veteran"/> (<c>BotTraits.Veteran</c>) come from the character folder, and
/// <see cref="Ransom"/> (what a guild's jailed members' ransom needs) from the crime folder. Until wired they are null and
/// read as nought: voters weigh records alone, nobody is discontented by dislike, and no chest saves for a ransom.
/// </para>
/// </summary>
public static class BotPolitics
{
    /// <summary>What <c>a</c> thinks of <c>b</c>, roughly −100 to 100. Wire to <c>BotTies.Regard</c> at merge.</summary>
    public static Func<BotMobile, BotMobile, double> TiesRegard { get; set; }

    /// <summary>Whether this bot is a veteran. Wire to <c>BotTraits.Veteran</c> at merge.</summary>
    public static Func<BotMobile, bool> Veteran { get; set; }

    /// <summary>What freeing this guild's jailed members would cost, or nought. Wire to the crime folder's ransom at merge.</summary>
    public static Func<Guild, int> Ransom { get; set; }

    public static double RegardOf(BotMobile a, BotMobile b) => a == null || b == null || TiesRegard == null ? 0.0 : TiesRegard(a, b);

    public static bool IsVeteran(BotMobile bot) => bot != null && Veteran != null && Veteran(bot);

    public static int RansomOf(Guild guild) => guild == null || Ransom == null ? 0 : Math.Max(0, Ransom(guild));

    /// <summary>One look of all four parts. From the module's clock, once a minute.</summary>
    public static void Look(long now)
    {
        BotElection.Look(now);
        BotPurpose.Look(now);
        BotCouncil.Look(now);

        if (BotTribute.Running)
        {
            BotTribute.Look(now);
        }

        // Each town's steward and its decrees (07.10.2026, steward20). See BotStewardship.
        BotStewardship.Look(now);
    }

    /// <summary>
    /// <b>Patrick's reset (07.10.2026, reset26): the politics of a world that no longer exists, forgotten.</b> The module's own reload
    /// (<see cref="BotPoliticsModule.Reset"/>) forgets counters only and keeps what its two stores write; the reset of 20:56 that night
    /// left <c>Saves/BotPolitics</c> and <c>Saves/BotStewardship</c> whole, every office kept by a guild's or a member's name, so a
    /// guild founded again under an old name would have come back with its old head, its old purpose, the town posts it held and
    /// the tributes it paid. Each part empties exactly what its store writes. The stewards and the council go first because they
    /// alone hold the city's coin — the coffers, and the wages of watches on duty — and hand it back to the purse; the rest hold
    /// none. The clock keeps running, so a new world's first term and first steward come as at a first start. Returns the gold
    /// handed back to the purse.
    /// </summary>
    public static int Wipe()
    {
        var back = BotStewardship.Wipe() + BotCouncil.Wipe();

        BotElection.Wipe();
        BotPurpose.Wipe();
        BotTribute.Wipe();

        return back;
    }

    /// <summary>The guild's politics on the page, inside its object in <c>/api/state</c>'s <c>guilds</c>. From <c>BotWebSnapshot</c>.</summary>
    public static void Write(Utf8JsonWriter w, Guild guild)
    {
        if (guild?.Name == null)
        {
            return;
        }

        w.WritePropertyName("politics");
        w.WriteStartObject();
        BotElection.Write(w, guild.Name);
        BotPurpose.Write(w, guild.Name);
        BotCouncil.Write(w, guild.Name);
        BotTribute.Write(w, guild.Name);
        w.WriteEndObject();
    }

    /// <summary>What the bots say about it, put into the voice's bank wherever the bank has nothing for the occasion.</summary>
    public static Dictionary<string, string[]> Phrases() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["politics:elected"] =
            [
                "I will lead {guild}. {votes} of {of} said so.",
                "{guild} has chosen me. Let us not waste it.",
                "You voted, and I heard. To work, {guild}."
            ],
            ["politics:reelected"] = ["{guild} keeps me at its head: {votes} of {of}.", "Again, then. I will not let you down."],
            ["politics:plot"] = ["{head} has led us badly. I say we vote.", "Enough of {head}. Who is with me?", "{reason}. {head} must answer for it."],
            ["politics:deposed"] = ["{head} is no longer our head: {against} of {of} said so.", "We have put {head} down. A new vote, now."],
            ["politics:confidence"] = ["{guild} stands by {head}: only {against} of {of} wanted otherwise.", "The grumblers are outvoted. {head} stays."],
            ["politics:purpose"] = ["The chest saves for {purpose} now: {price}gp.", "We voted: {purpose} first."],
            ["politics:purpose-none"] = ["The chest keeps nothing back now: it pays for what we need.", "No saving for now. The chest is for the members."],
            ["politics:collector"] = ["{town} has made {guild} its tax collector.", "We collect {town}'s taxes now. A tenth is ours."],
            ["politics:captain"] = ["{guild} captains the guard of {town}. There is a wage in it.", "We keep {town}'s peace now, for the town's coin."],
            ["politics:outcast"] = ["{town} counts us outcasts: everything there costs us more.", "We are not wanted in {town}. Their prices say so."],
            ["politics:ultimatum"] = ["{enemy} will pay us {coin}gp a day, or it is war.", "We could crush {enemy}. They pay instead."],
            ["politics:tribute"] = ["We pay {enemy} {coin}gp a day. Better than a war we cannot win.", "Tribute to {enemy}. Swallow it."],
            ["politics:refused"] = ["{enemy} wants {coin}gp a day. The chest cannot pay it. Then it is war.", "We have nothing to pay {enemy} with. To arms."],
            ["politics:defied"] = ["{enemy} will not pay. So be it.", "{enemy} chose war over tribute."],
            ["politics:tribute-end"] = ["No more tribute to {enemy}.", "The tribute to {enemy} is over: {reason}."],
            ["parley:end:tribute"] = ["Tribute, then. Better than blood.", "We will pay, and live."],
            ["took:guard-duty"] = ["To stand the town's guard.", "A watch for the town's wage."],
            ["finished:guard-duty"] = ["Watch done. The town paid.", "My watch is over, and paid for."]
        };
}

/// <summary>The numbers of <c>Configuration/bot-politics.json</c>. A missing key keeps the code's value; keys are PascalCase.</summary>
public sealed class BotPoliticsSettings
{
    public bool? Elections { get; set; }

    public double? ElectEveryHours { get; set; }

    public bool? ElectOnFall { get; set; }

    public bool? Plotting { get; set; }

    public double? DiscontentShare { get; set; }

    public double? CarryShare { get; set; }

    public bool? Purposes { get; set; }

    public double? PurposeEveryHours { get; set; }

    public int? WarChest { get; set; }

    public bool? Councils { get; set; }

    public double? TermHours { get; set; }

    public double? TaxShare { get; set; }

    public double? OutcastFactor { get; set; }

    public double? OutcastRegard { get; set; }

    public int? Wage { get; set; }

    public int? WageBudget { get; set; }

    public bool? Ultimatums { get; set; }

    public double? TributeOdds { get; set; }

    public double? TributeHours { get; set; }

    public double? TributeShare { get; set; }
}

/// <summary>
/// Politics as a module: reads its file, fills the voice's bank, offers the captain's guard, and runs the clock. World phase,
/// after the population, the will, the travel, the estate and the diplomacy, whose guilds, towns, chests and wars it reads.
/// </summary>
public sealed class BotPoliticsModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPoliticsModule));

    private const string ConfigPath = "Configuration/bot-politics.json";

    /// <summary>How often the four parts look: a minute.</summary>
    public static int LookMs { get; set; } = 60000;

    /// <summary>How long after the module starts before the first look: five minutes, while the population is raised.</summary>
    public static int SettleMs { get; set; } = 300000;

    private static Timer _timer;

    private static long _started;

    public override string Name => "Politics";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will", "Travel", "Estate", "Diplomacy"];

    public override void Start()
    {
        Load();

        var added = 0;
        Dictionary<string, string[]> missing = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, lines) in BotPolitics.Phrases())
        {
            if (!BotVoice.Has(key))
            {
                missing[key] = lines;
                added++;
            }
        }

        if (missing.Count > 0)
        {
            BotVoice.Phrases(missing);
        }

        BotWill.Offer(new BotGuardDuty());

        _started = Core.TickCount;
        BotCouncil.Open(_started);
        BotStewardship.Start();

        // The watches' wages a restart caught on duty, back into the treasury (reset26 review). See BotCouncil.Settle.
        BotCouncil.Settle();

        logger.Information(
            "Politics: elections {Elections} every {Every}h (a head gone at once, a head fallen {Fall}), plots {Plots} at {Discontent:P0} discontented and carried over {Carry:P0}; the chests' purpose by vote {Purposes} every {PurposeEvery}m; councils {Councils} every {Term}m, the collector {Tax:P0} of its town's tax, the captain's guard {Wage}gp a watch up to {Budget}gp a town a term, outcasts ×{Outcast:F2}; ultimatums {Ultimatums} at {Odds:0.#} times the might, {Share:P0} of the weak chest a day for {Hours:0.#}h; {Phrases} phrases added to the voice; ties {Ties}, veterans {Veterans}, ransom {Ransom}",
            BotElection.Running ? "ON" : "OFF",
            BotElection.EveryMs / 3600000.0,
            BotElection.OnFall ? "outside the gap" : "no",
            BotElection.Plotting ? "ON" : "OFF",
            BotElection.DiscontentShare,
            BotElection.CarryShare,
            BotPurpose.Running ? "ON" : "OFF",
            BotPurpose.EveryMs / 60000,
            BotCouncil.Running ? "ON" : "OFF",
            BotCouncil.TermMs / 60000,
            BotCouncil.TaxShare,
            BotCouncil.Wage,
            BotCouncil.WageBudget,
            BotCouncil.OutcastFactor,
            BotTribute.Running ? "ON" : "OFF",
            BotTribute.Odds,
            BotTribute.Share,
            BotTribute.Hours,
            added,
            BotPolitics.TiesRegard == null ? "not wired" : "wired",
            BotPolitics.Veteran == null ? "not wired" : "wired",
            BotPolitics.Ransom == null ? "not wired" : "wired"
        );

        _timer?.Stop();
        _timer = new PoliticsTimer(TimeSpan.FromMilliseconds(Math.Max(1000, LookMs)));
        _timer.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotPoliticsSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotPoliticsSettings());
            logger.Information("Wrote a starter politics file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotElection.Running = settings.Elections ?? BotElection.Running;
        BotElection.EveryMs = Ms(settings.ElectEveryHours, 3600000) ?? BotElection.EveryMs;
        BotElection.OnFall = settings.ElectOnFall ?? BotElection.OnFall;
        BotElection.Plotting = settings.Plotting ?? BotElection.Plotting;
        BotElection.DiscontentShare = settings.DiscontentShare ?? BotElection.DiscontentShare;
        BotElection.CarryShare = settings.CarryShare ?? BotElection.CarryShare;
        BotPurpose.Running = settings.Purposes ?? BotPurpose.Running;
        BotPurpose.EveryMs = Ms(settings.PurposeEveryHours, 3600000) ?? BotPurpose.EveryMs;
        BotPurpose.WarChest = settings.WarChest ?? BotPurpose.WarChest;
        BotCouncil.Running = settings.Councils ?? BotCouncil.Running;
        BotCouncil.TermMs = Ms(settings.TermHours, 3600000) ?? BotCouncil.TermMs;
        BotCouncil.TaxShare = settings.TaxShare ?? BotCouncil.TaxShare;
        BotCouncil.OutcastFactor = settings.OutcastFactor ?? BotCouncil.OutcastFactor;
        BotCouncil.OutcastRegard = settings.OutcastRegard ?? BotCouncil.OutcastRegard;
        BotCouncil.Wage = settings.Wage ?? BotCouncil.Wage;
        BotCouncil.WageBudget = settings.WageBudget ?? BotCouncil.WageBudget;
        BotTribute.Running = settings.Ultimatums ?? BotTribute.Running;
        BotTribute.Odds = settings.TributeOdds ?? BotTribute.Odds;
        BotTribute.Hours = settings.TributeHours ?? BotTribute.Hours;
        BotTribute.Share = settings.TributeShare ?? BotTribute.Share;
    }

    private static int? Ms(double? value, int unit) => value == null ? null : (int)Math.Max(0, Math.Min(int.MaxValue, value.Value * unit));

    public override void Reset()
    {
        logger.Information("Politics, before the reload: {Elections}; {Tribute}", BotElection.Describe(), BotTribute.Describe());

        _timer?.Stop();
        _timer = null;

        BotElection.Forget();
        BotPurpose.Forget();
        BotCouncil.Forget();
        BotTribute.Forget();
        BotStewardship.Stop();
    }

    private static void Tick()
    {
        var now = Core.TickCount;

        if (now - _started < SettleMs || BotGuilds.Count == 0)
        {
            return;
        }

        BotPolitics.Look(now);
    }

    private sealed class PoliticsTimer : Timer
    {
        public PoliticsTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => Tick();
    }
}
