using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the seizures each guild is remembered for across restarts. See <see cref="BotSeizure"/>.
///
/// <para>
/// <b>A file of its own rather than a new shape of <c>BotWarStore</c>,</b> so that a build older than this one can still read
/// the war ledger it wrote: an older build ignores a save file it does not know, and stops at the engine's prompt on a shape
/// it does not know. Written as durations, as the war ledger's clocks are: how long ago each seizure was, of the shard
/// running, rebased on the next clock at load.
/// </para>
/// </summary>
public sealed class BotSeizureStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSeizureStore));

    private const int Shape = 1;

    private const int Oldest = 1;

    private static BotSeizureStore _store;

    /// <summary>Registered from <c>BotCore.Configure</c>: a persistence must exist before the world loads.</summary>
    public static void Configure() => _store ??= new BotSeizureStore();

    public BotSeizureStore() : base("BotSeizures", 16)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);

        List<(string Guild, long Ago)> kept = [];

        foreach (var row in BotSeizure.Memory())
        {
            kept.Add(row);
        }

        writer.WriteEncodedInt(kept.Count);

        for (var i = 0; i < kept.Count; i++)
        {
            writer.Write(kept[i].Guild);
            writer.Write(kept[i].Ago);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < Oldest || shape > Shape)
        {
            logger.Warning(
                "The saved seizures are shape {Found} and this build reads {Oldest} to {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotSeizures/BotSeizures.bin is deleted",
                shape,
                Oldest,
                Shape
            );

            return;
        }

        var many = reader.ReadEncodedInt();

        for (var i = 0; i < many; i++)
        {
            var guild = reader.ReadString();
            var ago = reader.ReadLong();

            BotSeizure.Restore(guild, ago);
        }

        if (many > 0)
        {
            logger.Information("{Many} seizures read back off the save", many);
        }
    }
}
