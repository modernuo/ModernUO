using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps where every bot lives across restarts: by name, the town, when it settled there, how often it has moved and the
/// town it last left. See <see cref="BotResidence"/>. Registered by the engine's own sweep of static <c>Configure</c>
/// methods, as <c>BotRestStore</c> is, so it exists before the world is read; kept by the engine's persistence and never by
/// hand under <c>Saves</c>, which the engine moves into <c>Backups</c> at every save (builds 289–290). A shape this build
/// cannot read is dropped whole rather than guessed at, and the population chooses again.
/// </summary>
public sealed class BotResidenceStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotResidenceStore));

    private const int Shape = 1;

    private static BotResidenceStore _store;

    public static void Configure() => _store ??= new BotResidenceStore();

    public BotResidenceStore() : base("BotResidences", 20)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotResidence.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < 1 || shape > Shape)
        {
            logger.Warning("The saved residences are shape {Found} and this build reads {Wanted}; every bot chooses its town again", shape, Shape);

            return;
        }

        var read = BotResidence.Load(reader);

        logger.Information("Residences: where {Bots} bots live was read back from the save", read);
    }
}
