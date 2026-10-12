using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the guilds' politics across restarts: each guild's office (its elected head, the votes, when, its run of lost wars) and
/// every member's record it is elected by (kills of other guilds, wars won, dues given), what each chest was voted to save for,
/// the towns' posts and how far into its term the council is, and the tributes standing with the refusals still remembered.
/// Times are kept as running time elapsed, so a term or a tribute counts the shard's running time across a restart as it does
/// within one. A shape this build cannot read is dropped whole rather than guessed at: the guilds simply vote again.
/// </summary>
public sealed class BotPoliticsStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPoliticsStore));

    /// <summary>
    /// Shape 2 (07.10.2026, reset26 review) adds the watches' wages held at the save, after the tributes, handed back to the purse
    /// at the module's start (<see cref="BotCouncil.Settle"/>). Shape 1 is still read, with nothing held. A build older than this
    /// one drops a shape-2 save whole, as it says, and the guilds vote again.
    /// </summary>
    private const int Shape = 2;

    private const int Oldest = 1;

    private static BotPoliticsStore _store;

    public static void Configure() => _store ??= new BotPoliticsStore();

    public BotPoliticsStore() : base("BotPolitics", 22)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotElection.Save(writer);
        BotPurpose.Save(writer);
        BotCouncil.Save(writer);
        BotTribute.Save(writer);
        BotCouncil.SaveHeld(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < Oldest || shape > Shape)
        {
            logger.Warning("The saved politics are shape {Found} and this build reads {Oldest} to {Wanted}; the guilds will vote again", shape, Oldest, Shape);

            return;
        }

        var (offices, records) = BotElection.Load(reader);
        var ballots = BotPurpose.Load(reader);
        var seats = BotCouncil.Load(reader);
        var tributes = BotTribute.Load(reader);
        var wages = shape >= 2 ? BotCouncil.LoadHeld(reader) : 0;

        logger.Information(
            "Politics read back: {Offices} guilds' offices, {Records} members' records, {Ballots} chests' purposes, {Seats} towns' councils, {Tributes} tributes standing, {Wages}gp of watches' wages held at the save",
            offices,
            records,
            ballots,
            seats,
            tributes,
            wages
        );
    }
}
