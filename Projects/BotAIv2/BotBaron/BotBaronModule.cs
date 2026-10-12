using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-baron.json</c> may say. Everything optional; empty keeps the code's numbers.
///
/// <para>
/// <b>PascalCase, and it is not a style question.</b> The deserialiser matches these names as written, so a
/// key in lower case is not an error and not a warning — it is a value silently left at its default, and a
/// configuration file that appears to have been read is worse than one that fails to load.
/// </para>
/// </summary>
public sealed class BotBaronSettings
{
    /// <summary>Tiles across the ground one harrowing walks.</summary>
    public int? Side { get; set; }

    /// <summary>How many march, the Baron included.</summary>
    public int? Company { get; set; }

    /// <summary>Fewest he will set out with once the call has run its course.</summary>
    public int? Least { get; set; }

    /// <summary>How long he stands in the square calling for volunteers.</summary>
    public int? MusterMs { get; set; }

    /// <summary>Corpses that finish the errand.</summary>
    public int? Quota { get; set; }

    /// <summary>The longest one harrowing may last.</summary>
    public int? CapMs { get; set; }

    /// <summary>How far around the muster point he calls people up.</summary>
    public int? Reach { get; set; }

    /// <summary>Where the company forms up, as X, Y, Z. Absent means the population's own home.</summary>
    public int[] Square { get; set; }

    /// <summary>How many of the company should stand in the line.</summary>
    public int? Melee { get; set; }

    /// <summary>How many should shoot.</summary>
    public int? Ranged { get; set; }

    /// <summary>How many should mend.</summary>
    public int? Medics { get; set; }

    /// <summary>How far the company looks for something to kill.</summary>
    public int? Sight { get; set; }

    /// <summary>How long the company spends on one corner of the box.</summary>
    public int? RoundMs { get; set; }

    /// <summary>How far he will march a company.</summary>
    public int? Range { get; set; }

    /// <summary>What a harrowing is reckoned at per minute before experience corrects it.</summary>
    public double? Prior { get; set; }

    /// <summary>How many people a square must have taken before it is worth harrowing.</summary>
    public int? Deadly { get; set; }

    /// <summary>What a walk through the town is reckoned at per minute.</summary>
    public double? StrollPrior { get; set; }

    /// <summary>How far from the counter he wanders.</summary>
    public int? StrollReach { get; set; }

    /// <summary>How low the account may fall before the crown makes it up.</summary>
    public int? StipendFloor { get; set; }

    /// <summary>What he carries in his pocket.</summary>
    public int? StipendFloat { get; set; }
}

/// <summary>
/// The Baron as a module: his two errands registered, his numbers read, and one line an hour saying what
/// they came to.
///
/// <para>
/// <see cref="BotPhase.World"/>, because a harrowing is a place on the map and a walk through a town needs a
/// counter that has been surveyed. It requires <c>Squads</c> for the plainest possible reason: the whole of
/// this class is six bots standing together, and without squads running the offer could never be taken up.
/// </para>
///
/// <para>
/// <b>Its own module rather than a few lines inside the squads' one, and the switch is the reason.</b>
/// Turned off, the Baron is still raised and still wears his plate, and he is simply never offered anything
/// — which is the cheapest possible experiment when the question is "is the Baron doing this, or is
/// something else doing it and he is being blamed". Half this project's investigations have been that
/// question about one subsystem or another.
/// </para>
/// </summary>
public sealed class BotBaronModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBaronModule));

    private const string ConfigPath = "Configuration/bot-baron.json";

    public override string Name => "Baron";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Squads", "Classes"];

    /// <summary>
    /// How often his two offices are summed up in the shard's own log.
    ///
    /// Printed on a clock rather than only on a reload, for the reason <c>BotBeat.Summarise</c> carries:
    /// numbers printed only when the world reloads are numbers never printed, and "the Baron is not
    /// harrowing" and "the Baron harrows and you have not been watching" would otherwise be the same log.
    /// </summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static Timer _timer;

    public override void Start()
    {
        Load();

        BotWill.Offer(new BotHarrower());

        // His own rounds: the nearest ground nobody has stood in, walked for nothing and alone if need be.
        // See BotWarden — reckoned low on purpose, so a great hunt or a rescue outbids it every time.
        BotWill.Offer(new BotWarden());
        BotWill.Offer(new BotStroll());

        logger.Information(
            "The Baron is ready: ground within {Range} tiles that has taken {Deadly} or more sends him to ({SX}, {SY}), where for up to {Muster} minutes he calls up the nearest bots within {Call} tiles — nobody is asked, and no producer is taken. He wants {Melee} in the line, {Ranged} shooting and {Medics} mending, {Company} at most and {Least} at least. He walks a box {Side} tiles across looking {Sight} ahead, and is finished by {Quota} corpses or {Cap} minutes; either ending takes the whole box off the board. With nowhere to go he walks the town within {Stroll} tiles of a counter, at a walk. He takes no share of anything the company kills; the crown keeps him at {Grant}gp and makes it up below {Floor}gp, and he carries {Float}gp",
            BotHarrower.Range,
            BotPeril.Deadly,
            BotHarrow.Square != Point3D.Zero ? BotHarrow.Square.X : BotPopulation.Where.X,
            BotHarrow.Square != Point3D.Zero ? BotHarrow.Square.Y : BotPopulation.Where.Y,
            BotHarrow.MusterMs / 60000,
            BotHarrow.Reach,
            BotHarrow.Melee,
            BotHarrow.Ranged,
            BotHarrow.Medics,
            BotHarrow.Company,
            BotHarrow.Least,
            BotHarrow.Side,
            BotHarrow.Sight,
            BotHarrow.Quota,
            BotHarrow.CapMs / 60000,
            BotRounds.Reach,
            BotClasses.Find("Baron")?.Stipend ?? 0,
            BotStipend.Floor,
            BotStipend.Float
        );

        _timer?.Stop();
        _timer = new BaronTimer(TimeSpan.FromMilliseconds(SayEveryMs));
        _timer.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotBaronSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotBaronSettings());

            logger.Information("Wrote a starter baron file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotHarrow.Side = settings.Side ?? BotHarrow.Side;
        BotHarrow.Company = settings.Company ?? BotHarrow.Company;
        BotHarrow.Least = settings.Least ?? BotHarrow.Least;
        BotHarrow.MusterMs = settings.MusterMs ?? BotHarrow.MusterMs;
        BotHarrow.Quota = settings.Quota ?? BotHarrow.Quota;
        BotHarrow.CapMs = settings.CapMs ?? BotHarrow.CapMs;
        BotHarrow.Reach = settings.Reach ?? BotHarrow.Reach;
        BotHarrow.Melee = settings.Melee ?? BotHarrow.Melee;
        BotHarrow.Ranged = settings.Ranged ?? BotHarrow.Ranged;
        BotHarrow.Medics = settings.Medics ?? BotHarrow.Medics;

        if (settings.Square is { Length: >= 3 })
        {
            BotHarrow.Square = new Point3D(settings.Square[0], settings.Square[1], settings.Square[2]);
        }
        BotHarrow.Sight = settings.Sight ?? BotHarrow.Sight;
        BotHarrow.RoundMs = settings.RoundMs ?? BotHarrow.RoundMs;
        BotHarrow.Prior = settings.Prior ?? BotHarrow.Prior;
        BotHarrower.Range = settings.Range ?? BotHarrower.Range;
        BotPeril.Deadly = settings.Deadly ?? BotPeril.Deadly;
        BotRounds.Prior = settings.StrollPrior ?? BotRounds.Prior;
        BotRounds.Reach = settings.StrollReach ?? BotRounds.Reach;
        BotStipend.Floor = settings.StipendFloor ?? BotStipend.Floor;
        BotStipend.Float = settings.StipendFloat ?? BotStipend.Float;
    }

    public static string Summarise() =>
        $"{BotHarrower.Describe()}; {BotWarden.Describe()}; {BotStroll.Describe()}; {BotStipend.Describe()}";

    private sealed class BaronTimer : Timer
    {
        public BaronTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => logger.Information("The Baron: {What}", Summarise());
    }

    public override void Reset()
    {
        BotWarden.Forget();

        logger.Information("The Baron, before the reload: {State}", Summarise());

        _timer?.Stop();
        _timer = null;

        BotHarrower.Forget();
        BotStroll.Forget();
        BotStipend.Forget();
    }
}
