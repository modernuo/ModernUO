using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps what the proving ground has measured across restarts. See <see cref="BotProving"/>: a fight is half a minute of
/// the field and a reading is the only thing it produces, so a deploy must not send fifty bots back to the bottom rung.
/// Written as the bot's name and, for each creature, the class, the fights, the wins, R, the build it was measured with and
/// when; a reading of another build is thrown away when it is next asked, not here.
/// </summary>
public sealed class BotProvingStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotProvingStore));

    private static BotProvingStore _store;

    public static void Configure() => _store ??= new BotProvingStore();

    public BotProvingStore() : base("BotProving", 20)
    {
    }

    public override void Serialize(IGenericWriter writer) => BotProving.Save(writer);

    public override void Deserialize(IGenericReader reader)
    {
        var read = BotProving.Load(reader);

        logger.Information("The proving ground read back {Count} readings", read);
    }
}
