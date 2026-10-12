using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the town's books on crime across restarts: the thieves' roll, every report, the wanted list with its bounties, the
/// terms served, the release and repentance clocks and the traces against a fence. See <see cref="BotCrime"/>.
///
/// <para>
/// <b>A file of its own</b>, as <c>BotSeizureStore</c> is beside the war ledger: an older build ignores a save file it does not
/// know, and a new shape of <c>BotUnderworldStore</c> would stop it on the engine's prompt. Clocks are written as how long ago
/// they were, of the shard running, and rebased at load, so a posting's six hours and a repentance's two are hours of uptime.
/// What is not kept: the stolen goods in a pack (an hour's book, cold by the time a restart is over) and the ambush standing.
/// </para>
/// </summary>
public sealed class BotCrimeStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCrimeStore));

    private const int Shape = 1;

    private static BotCrimeStore _store;

    /// <summary>Registered from <c>BotCore.Configure</c>: a persistence must exist before the world loads.</summary>
    public static void Configure() => _store ??= new BotCrimeStore();

    public BotCrimeStore() : base("BotCrime", 16)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotCrime.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning(
                "The saved books on crime are shape {Found} and this build reads {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotCrime/BotCrime.bin is deleted",
                shape,
                Shape
            );

            return;
        }

        var count = BotCrime.Load(reader);

        if (count > 0)
        {
            logger.Information("The books on crime were read back: {Count} names on record", count);
        }
    }
}
