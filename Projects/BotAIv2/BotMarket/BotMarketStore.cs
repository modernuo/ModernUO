using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the Needs board — the spoken wants and their lines — across restarts (02.10.2026, build 391; Patrick: "the board may
/// be saved across restarts").
///
/// <para>
/// <b>A restart lied about every line's clock.</b> On build 389, 157 of 418 wants were said in the first five minutes after
/// boot: every bot that still needed a thing asked for it again, on a fresh clock, so a line two hours old came back as a new
/// one and the hour Patrick measures began again for it. The README's "a restart is a new market" was right while a want held
/// gold in escrow; since 01.10.2026 it holds none, and what it promises is the buyer's own money, which the save keeps anyway.
/// So the wants are kept: each with its buyer by serial, its kind by name, its units, offer and anchor, what was handed against
/// it, and its lines, every clock written as an age and read back as "that long ago". What carriers had reserved on it is not,
/// because no carrier's delivery is saved; the sweep would drop it on the first beat anyway.
/// </para>
///
/// <para>
/// <b>Safe on a world that never had it.</b> No file, nothing read, and the board starts empty as before. A want whose buyer was
/// deleted, or whose kind this build no longer knows, is read to its last byte and dropped, counted on the market line. Every
/// byte written is read, because a short read stops a headless shard on the engine's skip prompt. A wipe
/// (<c>BotHand.ResetPopulation</c>) clears the board through the market's reset, and the next save writes it empty.
/// </para>
///
/// <para>
/// Registered by the engine's own sweep of static <c>Configure</c> methods, like <see cref="BotCityStore"/>, at a priority after the
/// mobiles, so a buyer read by serial is already there.
/// </para>
/// </summary>
public sealed class BotMarketStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMarketStore));

    private const int Shape = 1;

    private static BotMarketStore _store;

    public static void Configure() => _store ??= new BotMarketStore();

    public BotMarketStore() : base("BotMarket", 16)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotMarket.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < 1 || shape > Shape)
        {
            logger.Warning(
                "The saved board is shape {Found} and this build reads {Wanted}; it cannot be read, and the shard will stop on the engine's own prompt until Saves/BotMarket/BotMarket.bin is deleted",
                shape,
                Shape
            );

            return;
        }

        var (kept, dropped) = BotMarket.Load(reader);

        logger.Information(
            "The board was read from the save (shape {Shape}): {Kept} wants to make again once the world is up, {Dropped} dropped for a buyer or a kind that is gone",
            shape,
            kept,
            dropped
        );
    }
}
