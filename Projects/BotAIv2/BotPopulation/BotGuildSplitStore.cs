using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps across restarts who left which guild and when, and the clocks of the splits. See <see cref="BotGuilds.Hive"/>.
///
/// <para>
/// <b>Why it exists (06.10.2026): the walk-out bar lived only in memory.</b> A bot that leaves a guild — crossing over, or as
/// one of the five who found a new guild out of a full one — may not join another for <see cref="BotGuilds.RejoinMs"/> and may
/// not go back to the one it left. Kept in memory, a restart forgot it, and a new guild founded by a split (nothing to show)
/// would have bled straight back into the five places its founders had left in the old one: the guild founded and emptied
/// again inside an hour of the next boot. Saved here as time elapsed, so the bar counts the shard's running time across
/// restarts exactly as it did within one.
/// </para>
///
/// <para>
/// Also kept: when each guild last lost five to a split (its cooldown), how many places its splits left (not counted as room
/// another five could take), when each split guild was founded and from whom (its grace before it may close), and when the
/// last split happened anywhere (the gap between two). A shape this build cannot read is dropped whole rather than guessed at.
/// </para>
/// </summary>
public sealed class BotGuildSplitStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGuildSplitStore));

    private const int Shape = 1;

    private static BotGuildSplitStore _store;

    public static void Configure() => _store ??= new BotGuildSplitStore();

    public BotGuildSplitStore() : base("BotGuildSplits", 19)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotGuilds.SaveSplits(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning("The saved guild splits are shape {Found} and this build reads {Wanted}; nobody is barred for having left a guild", shape, Shape);

            return;
        }

        var barred = BotGuilds.LoadSplits(reader, out var young);

        logger.Information("Guild leavers read back: {Barred} bots inside the bar for having left a guild, {Young} guilds founded by a split still young", barred, young);
    }
}
