using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The dashboard as a module: one command, registered once.
///
/// <para>
/// <see cref="BotPhase.Settings"/>, because a command is not a place — it needs nothing about the world to
/// exist, and registering it before the world loads means it is there for the first question somebody asks.
/// It is a module rather than a folder of services for exactly one reason: registering a command is start-up
/// work, and start-up work is what a module is.
/// </para>
///
/// <para>
/// It is also the only module whose switch is about people rather than bots. Off, the population runs exactly
/// as it did and nobody can look at it — which is occasionally what you want on a live shard and never what
/// you want while building one.
/// </para>
/// </summary>
public sealed class BotDashboardModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDashboardModule));

    public override string Name => "Dashboard";

    public override BotPhase Phase => BotPhase.Settings;

    public override void Start()
    {
        CommandSystem.Register("bots", AccessLevel.Administrator, OnCommand);

        logger.Information("The dashboard is on [bots — five tabs: the population, their market, what they are short of, what the city wants, and what the population has learned");
    }

    [Usage("bots")]
    [Description("Opens the BotAI v2 dashboard: the population, the bots' own market, and what the population is short of.")]
    private static void OnCommand(CommandEventArgs e) => BotDashboardGump.DisplayTo(e.Mobile);
}
