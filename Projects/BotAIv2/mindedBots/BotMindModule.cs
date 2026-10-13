using Server.BotAI.V2;
using Server.Logging;

namespace Server.BotAI.Mind;

/// <summary>
/// Two thinking bots, as a module of the shard's own bot system — registered from outside it.
///
/// <para>
/// <b>Nothing in BotAIv2 was touched to make this exist, and that is the requirement rather than a
/// nicety.</b> The seam was already there: modules register themselves, proposers offer themselves, and the
/// server loads whatever <c>Data/assemblies.json</c> names. So this assembly references BotAIv2, BotAIv2
/// references nothing of this, and switching the whole thing off is one line in a file — with the population
/// carrying on exactly as it did, because everything the two of them do is the population's own work.
/// </para>
///
/// <para>
/// <see cref="BotPhase.World"/> and after <c>Population</c> and <c>Will</c>: there is no body to think for
/// until the population has raised one, and nothing to offer work into until the auction exists.
/// </para>
/// </summary>
public sealed class BotMindModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMindModule));

    public override string Name => "Mind";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will", "Classes"];

    public override void Start()
    {
        BotMindConfig.Load();

        BotMinds.Start();

        // What the page says about the thinking layer, read from here because the page's assembly may not read this one.
        BotWebHooks.Minds = () => new BotMindsReport(
            BotOllama.Model,
            BotOllama.Endpoint,
            BotMinds.All.Count,
            BotOllama.Reachable,
            BotOllama.Asked,
            BotOllama.Answered,
            BotOllama.Refused,
            BotOllama.LastMs
        );

        // The model as the island's chronicler: the stories' one seam to it, filled whether or not any mind is running, since the
        // stories are the population's and the model is the machine's (PLAN-life A3, 13.10.2026). See BotStoryVoice.
        BotStoryVoice.Open();

        // Both of these are for minds that exist. With none running, the proposer would be asked for an
        // offer for every bot on every beat only to answer nothing, and the boot line below would announce
        // three thinkers that are not there — which is the same fault as the log file: a subsystem that is
        // switched on with nothing to do reads exactly like one that is working.
        if (BotMinds.All.Count == 0)
        {
            return;
        }

        BotWill.Offer(new BotMindProposer());

        // The numbers this is actually running with, said once at startup, because the file may have moved
        // any of them and a belief about behaviour built on the defaults in the source can be wrong by a
        // factor of two without anything looking odd.
        //
        // <b>Who is thinking is counted, not written down.</b> This line said "Three minds are awake:
        // Aldric the captain, Godric the architect and Cedric the sage" while four crafters were running and
        // those three were stood down — and it went on saying "4 of 3 have bodies" without anybody's build
        // breaking. The same fault as a hard-written population size, in the one line most likely to be read
        // as authoritative.
        logger.Information(
            "{Count} minds are awake on {Model} at {Endpoint}: {Who}; asked to choose every {Think}ms while free, reckoning up at most every {Review}ms, holding {Lessons} rules each ({PerTrade} of them about any one trade) and asking with a weight of {Insistence:F2}; {Embodied} of them have bodies. Their thinking is written to {Log}",
            BotMinds.All.Count,
            BotOllama.Model,
            BotOllama.Endpoint,
            Who(),
            BotMind.ThinkEveryMs,
            BotMind.ReviewEveryMs,
            BotMind.MostLessons,
            BotMind.MostPerTrade,
            BotMindDeed.Insistence,
            BotMinds.Embodied,
            BotMindLog.Path ?? "nowhere"
        );
    }

    /// <summary>The minds by name and office, for the boot line, read off the list rather than a constant.</summary>
    private static string Who()
    {
        var minds = BotMinds.All;
        var names = new string[minds.Count];

        for (var i = 0; i < minds.Count; i++)
        {
            names[i] = $"{minds[i].Name} the {minds[i].Trade}";
        }

        return string.Join(", ", names);
    }

    /// <summary>What the two of them have done, for whoever is reading the log.</summary>
    public static string Summarise() => $"{BotMinds.Describe()}; {BotOllama.Describe()}";

    public override void Reset()
    {
        BotStoryVoice.Close();
        BotMinds.Stop();
        BotOllama.Forget();
    }
}
