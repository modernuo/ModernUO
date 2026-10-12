using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps which guild holds which town building, and each guild's wish, across restarts. See <see cref="BotGuildHouses"/>:
/// the doors and signs are world items and the world save keeps them; this keeps whose they are, by the tile of the
/// building's first door and the guild's name. Registered by the engine's own sweep of static <c>Configure</c> methods, as
/// <c>BotResidenceStore</c> is, so it exists before the world is read; kept by the engine's persistence and never by hand
/// under <c>Saves</c>, which the engine moves into <c>Backups</c> at every save. A shape this build cannot read is dropped
/// whole rather than guessed at: the houses' doors and signs then stand in the world as nobody's, and the boot's
/// reconciliation takes them down.
/// </summary>
public sealed class BotGuildHouseStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGuildHouseStore));

    private const int Shape = 1;

    private static BotGuildHouseStore _store;

    public static void Configure() => _store ??= new BotGuildHouseStore();

    public BotGuildHouseStore() : base("BotGuildHouses", 20)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotGuildHouses.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < 1 || shape > Shape)
        {
            logger.Warning("The saved guild houses are shape {Found} and this build reads {Wanted}; every guild settles again, and the old doors and signs come down at the boot", shape, Shape);

            return;
        }

        var read = BotGuildHouses.Load(reader);

        logger.Information("Guild houses: {Houses} read back from the save: {List}", read, BotGuildHouses.Tell());
    }
}
