using System;
using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>The numbers of <c>Configuration/bot-officers.json</c>. A missing key keeps the code's value; keys are PascalCase.</summary>
public sealed class BotOfficersSettings
{
    public bool? Running { get; set; }

    public bool? Quartermasters { get; set; }

    public bool? Heralds { get; set; }

    public bool? Scouts { get; set; }

    public double? TermHours { get; set; }

    public bool? Stores { get; set; }

    public bool? PorterGives { get; set; }

    public int? PoorBelow { get; set; }

    public double? LienShare { get; set; }

    public double? DefaultHours { get; set; }

    public bool? GuildWants { get; set; }

    public bool? Poaching { get; set; }

    public bool? Bouts { get; set; }

    public int? BoutPurseMost { get; set; }

    public bool? Rumours { get; set; }

    public bool? Shadows { get; set; }

    public bool? Patrols { get; set; }
}

/// <summary>
/// The guilds' officers as a module: reads its numbers, puts its words into the voice's bank, offers the officers' work and the
/// members' draws to the auction, and runs the clock that appoints, collects the store's debts and referees the bouts. Patrick's
/// approval of 07.10.2026 (night 17, "a livelier world"). See <see cref="BotOfficers"/>.
///
/// <para>
/// <b>Registers itself</b> from its own <c>Configure</c> (priority 42, after <c>BotCore</c>'s 40 and the character's 41), as the
/// character module does, so the list in <c>BotCore</c> is not one more hunk four agents collide in.
/// </para>
/// </summary>
public sealed class BotOfficersModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOfficersModule));

    private const string ConfigPath = "Configuration/bot-officers.json";

    /// <summary>The clock: bouts read every beat, the appointments and the lien on their own minutes. A second.</summary>
    public static int TickMs { get; set; } = 1000;

    /// <summary>How long after the start before the first appointments, while the population is raised.</summary>
    public static int SettleMs { get; set; } = 180000;

    private static Timer _timer;

    private static long _started;

    [CallPriority(42)]
    public static void Configure()
    {
        if (BotCore.Enabled)
        {
            BotModules.Register(new BotOfficersModule());
        }
    }

    public override string Name => "Officers";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will", "Voice", "Estate"];

    public override void Start()
    {
        Load();

        var added = 0;
        Dictionary<string, string[]> missing = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, lines) in BotOfficers.Phrases())
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

        BotWill.Offer(new BotQuartermaster());
        BotWill.Offer(new BotKitDraw());
        BotWill.Offer(new BotHeraldStand());
        BotWill.Offer(new BotGuildScout());

        _started = Core.TickCount;

        logger.Information(
            "Officers: {State} (quartermasters {Q}, heralds {H}, scouts {S}), the head reviewing each post every {Term}h, a guild of {Least}+; "
            + "stores {Stores} at the guild's house or hall, {Most} a kind ({Bottles} a draught), the porter giving surplus within {Take} tiles {Porter}, kits to members short of them under {Poor}gp spendable or new, owed to the chest at {Lien:P0} of every gain, written off after {Default:0.#}h; "
            + "guild wants {Wants} (up to {WantMost} a want, the chest paying back on delivery); heralds stand {Stand}m by the bank, invite within {Reach} tiles (poaching the discontented {Poach}), news every {News}s; "
            + "bouts {Bouts} every {BoutEvery}h a guild, purse {PurseLeast}–{PurseMost}gp; scouts: rumours {Rumours}, shadows {Shadows} (warning within {Warn} tiles), patrols {Patrols} ({Hold}s a zone within {Patrol} tiles); {Phrases} phrases added to the voice",
            BotOfficers.Running ? "ON" : "OFF",
            BotOfficers.Quartermasters ? "on" : "OFF",
            BotOfficers.Heralds ? "on" : "OFF",
            BotOfficers.Scouts ? "on" : "OFF",
            BotOfficers.TermMs / 3600000.0,
            BotOfficers.LeastMembers,
            BotGuildStore.Running ? "ON" : "OFF",
            BotGuildStore.MostUnits,
            BotGuildStore.MostBottles,
            BotGuildStore.TakeReach,
            BotGuildStore.PorterGives ? "on" : "OFF",
            BotGuildStore.PoorBelow,
            BotGuildStore.LienShare,
            BotGuildStore.DefaultHours,
            BotQuartermaster.Wants ? "on" : "OFF",
            BotQuartermaster.WantMost,
            BotHeraldStand.ShiftMinutes,
            BotHeraldStand.Reach,
            BotHeraldStand.Poaching ? "on" : "OFF",
            BotHeraldStand.NewsEveryMs / 1000,
            BotGuildBout.Running ? "ON" : "OFF",
            BotGuildBout.EveryMs / 3600000.0,
            BotGuildBout.PurseLeast,
            BotGuildBout.PurseMost,
            BotGuildScout.Rumours ? "on" : "OFF",
            BotGuildScout.Shadows ? "on" : "OFF",
            BotGuildScout.WarnReach,
            BotGuildScout.Patrols ? "on" : "OFF",
            BotGuildScout.HoldMs / 1000,
            BotGuildScout.PatrolReach,
            added
        );

        _timer?.Stop();
        _timer = new OfficersTimer(TimeSpan.FromMilliseconds(Math.Max(250, TickMs)));
        _timer.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotOfficersSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotOfficersSettings());
            logger.Information("Wrote a starter officers file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotOfficers.Running = settings.Running ?? BotOfficers.Running;
        BotOfficers.Quartermasters = settings.Quartermasters ?? BotOfficers.Quartermasters;
        BotOfficers.Heralds = settings.Heralds ?? BotOfficers.Heralds;
        BotOfficers.Scouts = settings.Scouts ?? BotOfficers.Scouts;
        BotOfficers.TermMs = settings.TermHours is { } term ? (int)Math.Clamp(term * 3600000, 60000, int.MaxValue) : BotOfficers.TermMs;
        BotGuildStore.Running = settings.Stores ?? BotGuildStore.Running;
        BotGuildStore.PorterGives = settings.PorterGives ?? BotGuildStore.PorterGives;
        BotGuildStore.PoorBelow = settings.PoorBelow ?? BotGuildStore.PoorBelow;
        BotGuildStore.LienShare = settings.LienShare ?? BotGuildStore.LienShare;
        BotGuildStore.DefaultHours = settings.DefaultHours ?? BotGuildStore.DefaultHours;
        BotQuartermaster.Wants = settings.GuildWants ?? BotQuartermaster.Wants;
        BotHeraldStand.Poaching = settings.Poaching ?? BotHeraldStand.Poaching;
        BotGuildBout.Running = settings.Bouts ?? BotGuildBout.Running;
        BotGuildBout.PurseMost = settings.BoutPurseMost ?? BotGuildBout.PurseMost;
        BotGuildScout.Rumours = settings.Rumours ?? BotGuildScout.Rumours;
        BotGuildScout.Shadows = settings.Shadows ?? BotGuildScout.Shadows;
        BotGuildScout.Patrols = settings.Patrols ?? BotGuildScout.Patrols;
    }

    public override void Reset()
    {
        logger.Information("Officers, before the reload: {What}", BotOfficers.Clause());

        _timer?.Stop();
        _timer = null;

        BotOfficers.Forget();
        BotGuildStore.Forget();
        BotQuartermaster.Forget();
        BotKitDraw.Forget();
        BotHeraldStand.Forget();
        BotGuildBout.Forget();
        BotGuildScout.Forget();
    }

    private static void Tick()
    {
        var now = Core.TickCount;

        if (now - _started < SettleMs || BotGuilds.Count == 0)
        {
            return;
        }

        BotOfficers.Look(now);

        if (!BotOfficers.Running)
        {
            return;
        }

        BotGuildStore.Collect(now);
        BotQuartermaster.SettleAll(now);
        BotGuildBout.Beat(now);
    }

    private sealed class OfficersTimer : Timer
    {
        public OfficersTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick()
        {
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                logger.Error(e, "The officers' clock threw");
            }
        }
    }
}

/// <summary>
/// Keeps the guilds' officers and stores across restarts: the posts by holder's name with the age of the appointment, each store's
/// goods by kind name, the kit debts with how far into their play they are, and the defaulters. A shape this build cannot read is
/// dropped whole: the heads appoint again and the stores start empty.
/// </summary>
public sealed class BotOfficersStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOfficersStore));

    private const int Shape = 1;

    private static BotOfficersStore _store;

    public static void Configure() => _store ??= new BotOfficersStore();

    public BotOfficersStore() : base("BotOfficers", 23)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotOfficers.Save(writer);
        BotGuildStore.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning("The saved officers are shape {Found} and this build reads {Wanted}; the heads appoint again and the stores start empty", shape, Shape);

            return;
        }

        var posts = BotOfficers.Load(reader);
        var (stores, debts) = BotGuildStore.Load(reader);

        logger.Information("Officers read back: {Posts} posts held, {Stores} guild stores, {Debts} kit debts standing", posts, stores, debts);
    }
}
