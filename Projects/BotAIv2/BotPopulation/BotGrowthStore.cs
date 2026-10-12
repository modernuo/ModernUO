using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the newcomers across restarts. See <see cref="BotGrowth"/>: how many of each class have been added over the
/// configured mix, and when the last of them arrived; without it a restart would delete them as bots nobody asked for.
/// A shape this build cannot read is dropped whole rather than guessed at.
/// </summary>
public sealed class BotGrowthStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGrowthStore));

    private const int Shape = 1;

    private static BotGrowthStore _store;

    public static void Configure() => _store ??= new BotGrowthStore();

    public BotGrowthStore() : base("BotGrowth", 19)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotGrowth.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning("The saved newcomers are shape {Found} and this build reads {Wanted}; the configured mix stands alone", shape, Shape);

            return;
        }

        var added = BotGrowth.Load(reader);

        logger.Information("Newcomers read back: {Added} bots over the configured mix", added);
    }
}
