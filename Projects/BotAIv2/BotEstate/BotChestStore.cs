using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the guilds' chests across restarts. See <see cref="BotChest"/>: the tithes are the claims' income, and
/// the claims survive a restart (<see cref="BotClaimStore"/>), so their income must too. Written as the guild's name
/// and what the chest holds; a shape this build cannot read is dropped whole rather than guessed at.
/// </summary>
public sealed class BotChestStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotChestStore));

    private const int Shape = 1;

    private static BotChestStore _store;

    public static void Configure() => _store ??= new BotChestStore();

    public BotChestStore() : base("BotChests", 17)
    {
    }

    /// <summary>Chests read back at the last load. For the summary.</summary>
    public static int Restored { get; private set; }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotChest.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning(
                "The saved chests are shape {Found} and this build reads {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotChests/BotChests.bin is deleted",
                shape,
                Shape
            );

            return;
        }

        Restored = BotChest.Load(reader);

        logger.Information("The guilds' chests were read back: {Chests} of them, holding what they held", Restored);
    }
}
