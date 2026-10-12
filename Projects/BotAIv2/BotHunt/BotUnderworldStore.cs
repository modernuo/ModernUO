using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the island's criminal record and The Shadow across restarts: each bot's murders, robberies and times caught by name,
/// and the guild's members and hideout.
///
/// <para>
/// <b>A record that ends with the red hour is no record.</b> The population is raised afresh at every boot, the engine's own
/// guilds lose every member in the purge, and before build 113 nothing on the shard remembered that a bot had ever killed
/// once its hour ran out; "the most skilled murderers" could not have been asked for. See <see cref="BotUnderworld"/>.
/// </para>
/// </summary>
public sealed class BotUnderworldStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotUnderworldStore));

    private const int Shape = 1;

    private static BotUnderworldStore _store;

    public static void Configure() => _store ??= new BotUnderworldStore();

    public BotUnderworldStore() : base("BotUnderworld", 15)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotUnderworld.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning(
                "The saved underworld is shape {Found} and this build reads {Wanted}; it cannot be read, and the shard will stop on the engine's own prompt until Saves/BotUnderworld/BotUnderworld.bin is deleted",
                shape,
                Shape
            );

            return;
        }

        BotUnderworld.Load(reader);

        logger.Information("The underworld was read back: {Described}", BotUnderworld.Describe());
    }
}
