using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps every bot's play and rest across restarts. See <see cref="BotRest"/>: a bot resting when the shard stops is
/// still resting when it starts, and a bot four hours into its evening is not handed a fresh one by a deploy. Written as
/// the name, the kind of player and its hours, the minutes played this session, when the rest ends, when it began and how
/// many sessions there have been;
/// a shape this build cannot read is dropped whole rather than guessed at.
/// </summary>
public sealed class BotRestStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRestStore));

    private const int Shape = 4;

    private static BotRestStore _store;

    public static void Configure() => _store ??= new BotRestStore();

    public BotRestStore() : base("BotRest", 19)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotRest.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < 1 || shape > Shape)
        {
            logger.Warning("The saved rest is shape {Found} and this build reads {Wanted}; every bot starts a fresh session", shape, Shape);

            return;
        }

        var read = BotRest.Load(reader, shape);

        logger.Information("Play and rest were read back for {Bots} bots", read);
    }
}
