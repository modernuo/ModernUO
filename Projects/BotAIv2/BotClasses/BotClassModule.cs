using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The class layer as a module: reads its file, then says what the nine came out as.
///
/// <para>
/// Runs in <see cref="BotPhase.Settings"/> and requires nothing. It is the only module that can make
/// both claims honestly — what a class is has no dependence on the world at all, which is exactly why
/// it can be built and read before there is one.
/// </para>
///
/// <para>
/// The census is logged here rather than by the entry point, because the entry point should not know
/// what a role is. A module that cannot report on itself has to be reported on by something that
/// reaches inside it, and that is how a loader turns into the thing it was meant to replace.
/// </para>
/// </summary>
public sealed class BotClassModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotClassModule));

    public override string Name => "Classes";

    public override BotPhase Phase => BotPhase.Settings;

    public override void Start()
    {
        BotClassConfig.Load();

        // Every one of these numbers is a claim the nine class files make. A boot that reports two
        // producers when configuration was meant to add a third is the cheapest place there will ever
        // be to notice.
        logger.Information(
            "Classes: {Count} — {Melee} melee, {Ranged} ranged, {Casters} caster, {Medics} medic, {Producers} producing; {Casting} of them cast",
            BotClasses.All.Count,
            BotClasses.Count(BotRole.Melee),
            BotClasses.Count(BotRole.Ranged),
            BotClasses.Count(BotRole.Caster),
            BotClasses.Count(BotRole.Medic),
            BotClasses.Count(BotRole.Producer),
            BotClasses.Casting
        );
    }
}
