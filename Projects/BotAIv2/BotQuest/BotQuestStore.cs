using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the board of errands across restarts, each errand with its count and its held reward; a taker is not
/// kept, so every errand is open again after a boot. See <see cref="BotQuests"/>.
/// </summary>
public sealed class BotQuestStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotQuestStore));

    private const int Shape = 1;

    private static BotQuestStore _store;

    public static void Configure() => _store ??= new BotQuestStore();

    public BotQuestStore() : base("BotQuests", 18)
    {
    }

    /// <summary>Errands read back at the last load. For the summary.</summary>
    public static int Restored { get; private set; }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotQuests.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning(
                "The saved board is shape {Found} and this build reads {Wanted}; it cannot be read, and the shard will stop on the engine's own prompt until Saves/BotQuests/BotQuests.bin is deleted",
                shape,
                Shape
            );

            return;
        }

        Restored = BotQuests.Load(reader);

        logger.Information("The board of errands was read back: {Errands} errands, every one open again", Restored);
    }
}
