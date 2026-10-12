using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the island's reputation across restarts. What the population found out about the ground outlives
/// the population.
///
/// <para>
/// <b>The one thing on this shard that genuinely should not start again every morning.</b> Skills are a
/// bot's own and are already carried over; belongings are rebuilt from nothing on purpose. But where the
/// ground is dangerous is a fact about the island rather than about anybody standing on it, learned slowly
/// and only by walking — four hundred quadrants take a company most of an evening — and thrown away on
/// every restart it was learned nothing at all. The map that decides where hunters go, where a Baron
/// harrows and what the world map pins say was blank at the start of every session, which is why those
/// pins never showed anything a person could act on.
/// </para>
///
/// <para>
/// <b>Names and numbers only, exactly as <see cref="BotProgress"/> is.</b> Nothing here holds a reference to
/// an item, a mobile or a serial — a quadrant is a facet id, two coordinates and a handful of counters — so
/// it can outlive a world the rest of the shard cannot. The facet is written as its id and looked up again
/// on load, because a <c>Map</c> from the world being replaced is a reference to a deleted object.
/// </para>
/// </summary>
public sealed class BotQuadStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotQuadStore));

    /// <summary>
    /// The shape of what is written below.
    ///
    /// A shape this build cannot read is dropped whole rather than guessed at; a shape it knows is read and
    /// carried forward. See <c>BotProgress.Shape</c>, which carries the note about why dropping on every
    /// bump was the wrong rule and what it cost — a stopped shard on a console prompt nobody could answer.
    /// </summary>
    private const int Shape = 2;

    private const int Oldest = 1;

    private static BotQuadStore _store;

    /// <summary>Registered from <c>BotCore.Configure</c>: a persistence must exist before the world loads.</summary>
    public static void Configure() => _store ??= new BotQuadStore();

    /// <summary>The priority is only an ordering among save files; nothing else depends on it.</summary>
    public BotQuadStore() : base("BotQuads", 13)
    {
    }

    /// <summary>Quadrants read back from the last save, for the start-up line to report.</summary>
    public static int Restored { get; private set; }

    public override void Serialize(IGenericWriter writer)
    {
        var quads = BotQuad.All;

        writer.WriteEncodedInt(Shape);
        writer.WriteEncodedInt(quads.Count);

        foreach (var quad in quads)
        {
            writer.WriteEncodedInt(quad.Map?.MapID ?? -1);
            writer.WriteEncodedInt(quad.X);
            writer.WriteEncodedInt(quad.Y);
            writer.Write(quad.Safety);
            writer.WriteEncodedInt(quad.Passes);
            writer.WriteEncodedInt(quad.Blows);
            writer.WriteEncodedInt(quad.Deaths);
            writer.WriteEncodedInt(quad.RangersLost);
            writer.Write(quad.Trodden);
            writer.Write(quad.Swept);

            // The harrowing is written as a flag rather than as its tick: a tick count is meaningless in the
            // next process — on some hosts it is the machine's uptime — and what anybody reads off this is
            // "has a great hunt been through here", which is a yes or a no.
            writer.Write(quad.HarrowedTick != 0);

            // Shape 2, 02.09.2026: what the crown owes this square. A levy that did not survive the night
            // would restart the ladder at six every morning, and the ground it is about does not forget.
            writer.WriteEncodedInt(quad.Levied);
            writer.WriteEncodedInt(quad.Wipes);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < Oldest || shape > Shape)
        {
            logger.Warning(
                "The saved island is shape {Found} and this build reads {Oldest} to {Wanted}; it cannot be read, and the shard will stop on the engine's own prompt until Saves/BotQuads/BotQuads.bin is deleted",
                shape,
                Oldest,
                Shape
            );

            return;
        }

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var facet = reader.ReadEncodedInt();
            var x = reader.ReadEncodedInt();
            var y = reader.ReadEncodedInt();
            var safety = reader.ReadDouble();
            var passes = reader.ReadEncodedInt();
            var blows = reader.ReadEncodedInt();
            var deaths = reader.ReadEncodedInt();
            var rangers = reader.ReadEncodedInt();
            var trodden = reader.ReadBool();
            var swept = reader.ReadBool();
            var harrowed = reader.ReadBool();

            // Read only where it was written. Shape 1 is still a readable island; it simply owes nobody a
            // levy yet, which is the truth about a save written before the ladder existed.
            var levied = shape >= 2 ? reader.ReadEncodedInt() : 0;
            var wipes = shape >= 2 ? reader.ReadEncodedInt() : 0;

            BotQuad.Restore(
                facet,
                x,
                y,
                safety,
                passes,
                blows,
                deaths,
                rangers,
                trodden,
                swept,
                harrowed,
                levied,
                wipes
            );
        }

        Restored = count;

        logger.Information("The island was read back: {Count} quadrants the population had already walked", count);
    }
}
