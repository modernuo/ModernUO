using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The board of errands as a module: opens the board's clock and offers the errands to the population. See
/// <see cref="BotQuests"/>.
/// </summary>
public sealed class BotQuestModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotQuestModule));

    public override string Name => "Quests";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Will", "Market", "Hunt"];

    public override void Start()
    {
        BotQuests.Start();
        BotWill.Offer(new BotQuester());

        logger.Information(
            "The board of errands is up: at most {Most} at once, each lapsing after {Lapse} hours untaken, going back on the board after {Hold} minutes without progress and coming down after {LetGo} takers let it go with nothing done; a kill is looked for within {Range} tiles of the errand's place, or at the nearest of the island's lairs of the creature when it names none, given up after {Empty} minutes of seeing nothing; errands are steadfast and claimed at their reward over the walk and the work; {Standing} errands stand on it now",
            BotQuests.MostOpen,
            BotQuests.LapseMs / 3600000,
            BotQuests.HoldMs / 60000,
            BotQuests.MostLetGo,
            BotQuests.KillRange,
            BotQuests.EmptyMs / 60000,
            BotQuests.All.Count
        );
    }

    public override void Reset()
    {
        BotQuests.Stop();
        BotQuests.Wipe();
        BotQuests.Forget();
        BotQuester.Forget();
        BotLairs.Forget();
        BotKept.Forget();
        BotQuestDeed.Forget();
    }
}
