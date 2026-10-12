using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the murderers and the cells across restarts.
///
/// <para>
/// <b>A restart was an amnesty, and the engine did not grant it.</b> The red hour and the sentence in a cell lived in
/// <see cref="BotOutlaw"/>'s memory alone; the murderer's kills live in the world save. So on 16.09.2026 Gerda Ashdown,
/// red at 10:58, came back from the 11:25 restart with her five kills and no record: the guards still killed her on
/// sight, the Baron no longer hunted her, the city could put no price on her head, and nothing anywhere said why.
/// The wars' ledger learned the same lesson on 13.09.2026.
/// </para>
///
/// <para>
/// Written with each clock's remaining time rather than its tick, because a tick count belongs to the process that made
/// it; read back as "that much left".
/// </para>
/// </summary>
public sealed class BotOutlawStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOutlawStore));

    /// <summary>Two since build 114: each record carries its wanted clock and whether the Baron was told. One is still read.</summary>
    private const int Shape = 2;

    private static BotOutlawStore _store;

    public static void Configure() => _store ??= new BotOutlawStore();

    public BotOutlawStore() : base("BotOutlaws", 15)
    {
    }

    /// <summary>Records read back at the last load. For the summary.</summary>
    public static int Restored { get; private set; }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotOutlaw.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape is < 1 or > Shape)
        {
            logger.Warning(
                "The saved outlaws are shape {Found} and this build reads {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotOutlaws/BotOutlaws.bin is deleted",
                shape,
                Shape
            );

            return;
        }

        var (reds, jailed) = BotOutlaw.Load(reader, shape);

        Restored = reds + jailed;

        logger.Information("The outlaws were read back: {Reds} red and {Jailed} in the cells, their clocks carried on from where they stood", reds, jailed);
    }
}
