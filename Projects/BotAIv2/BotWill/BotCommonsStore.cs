using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps what the population has found out about what pays where across restarts.
///
/// <para>
/// <b>Every restart was an hour of relearning, and the shard restarts every hour.</b> <see cref="BotCommons"/> is the
/// board the auction reads when a bot has never worked a place — what each trade pays in each patch of ground, and how
/// far each trade's claim stands from what it turned out to pay — and it lived only in memory. A deploy emptied it, the
/// first half hour after every boot read worse than the half hour before it, and every forty-five-minute window this
/// project measures itself by carried that warm-up inside it. Patrick's choice of 16.09.2026: keep it, the way the
/// island's reputation is kept.
/// </para>
///
/// <para>
/// Written with each record's age rather than its tick, because a tick count belongs to the process that made it —
/// on some hosts it is the machine's uptime — and read back as "touched that long ago", so the half-life goes on
/// fading it from where it was. A shape this build cannot read is dropped whole rather than guessed at.
/// </para>
/// </summary>
public sealed class BotCommonsStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCommonsStore));

    private const int Shape = 1;

    private static BotCommonsStore _store;

    public static void Configure() => _store ??= new BotCommonsStore();

    public BotCommonsStore() : base("BotCommons", 14)
    {
    }

    /// <summary>Patches read back at the last load. For the summary.</summary>
    public static int Restored { get; private set; }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotCommons.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning(
                "The saved commons are shape {Found} and this build reads {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotCommons/BotCommons.bin is deleted",
                shape,
                Shape
            );

            return;
        }

        var (patches, trades, seams) = BotCommons.Load(reader);

        Restored = patches;

        logger.Information(
            "The commons were read back: {Patches} patches, {Trades} trades and {Seams} seams of what pays where, faded from when they were written",
            patches,
            trades,
            seams
        );
    }
}
