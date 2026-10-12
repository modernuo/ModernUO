using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the guilds' dealings across restarts: the agreements sworn and how long each has left, how long each pair must wait
/// before the duke hears it again, the books of grievances, and every guild's standing in every town — its prices, its failures
/// in a row, whether it is put out, and the task standing. Meetings in flight are not kept: a restart ends them, and the
/// grievance that called one calls it again at its next drift.
///
/// <para>
/// Registered by the engine's own sweep of static <c>Configure</c> methods, as <c>BotResidenceStore</c> is, and kept by the
/// engine's persistence — never by hand under <c>Saves</c>, which the engine moves into <c>Backups</c> at every save. Clocks
/// are written as time left, as the war ledger's are (<c>BotWarStore</c>), so the hours the shard is down count against no
/// agreement and no task; the one wall-clock date is a guild's exile, which the order measured in days. A shape this build
/// cannot read is dropped whole rather than guessed at.
/// </para>
/// </summary>
public sealed class BotDiplomacyStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDiplomacyStore));

    private const int Shape = 1;

    private static BotDiplomacyStore _store;

    public static void Configure() => _store ??= new BotDiplomacyStore();

    public BotDiplomacyStore() : base("BotDiplomacy", 21)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);

        List<(string A, string B, BotPact.Kind Kind, long Left)> pacts = [];

        foreach (var row in BotPact.Standing())
        {
            pacts.Add(row);
        }

        writer.WriteEncodedInt(pacts.Count);

        for (var i = 0; i < pacts.Count; i++)
        {
            writer.Write(pacts[i].A);
            writer.Write(pacts[i].B);
            writer.WriteEncodedInt((int)pacts[i].Kind);
            writer.Write(pacts[i].Left);
        }

        List<(string A, string B, long Left)> clocks = [];

        foreach (var row in BotParley.Clocks())
        {
            clocks.Add(row);
        }

        writer.WriteEncodedInt(clocks.Count);

        for (var i = 0; i < clocks.Count; i++)
        {
            writer.Write(clocks[i].A);
            writer.Write(clocks[i].B);
            writer.Write(clocks[i].Left);
        }

        List<(string Of, string About, string Why, int Count, double Moved)> causes = [];

        foreach (var (pair, book) in BotGrievances.Books)
        {
            foreach (var (why, entry) in book)
            {
                causes.Add((pair.Of, pair.About, why, entry.Count, entry.Moved));
            }
        }

        writer.WriteEncodedInt(causes.Count);

        for (var i = 0; i < causes.Count; i++)
        {
            writer.Write(causes[i].Of);
            writer.Write(causes[i].About);
            writer.Write(causes[i].Why);
            writer.WriteEncodedInt(causes[i].Count);
            writer.Write(causes[i].Moved);
        }

        BotBurgh.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < 1 || shape > Shape)
        {
            logger.Warning("The saved dealings between guilds are shape {Found} and this build reads {Wanted}; they are dropped and begin again", shape, Shape);

            return;
        }

        var pacts = reader.ReadEncodedInt();

        for (var i = 0; i < pacts; i++)
        {
            var a = reader.ReadString();
            var b = reader.ReadString();
            var kind = (BotPact.Kind)reader.ReadEncodedInt();
            var left = reader.ReadLong();

            BotPact.Restore(a, b, kind, left);
        }

        var clocks = reader.ReadEncodedInt();

        for (var i = 0; i < clocks; i++)
        {
            var a = reader.ReadString();
            var b = reader.ReadString();
            var left = reader.ReadLong();

            BotParley.RestoreClock(a, b, left);
        }

        var causes = reader.ReadEncodedInt();

        for (var i = 0; i < causes; i++)
        {
            var of = reader.ReadString();
            var about = reader.ReadString();
            var why = reader.ReadString();
            var count = reader.ReadEncodedInt();
            var moved = reader.ReadDouble();

            BotGrievances.Restore(of, about, why, count, moved);
        }

        var standing = BotBurgh.Load(reader);

        logger.Information(
            "Diplomacy: {Pacts} agreements, {Clocks} pairs waiting on the duke's word, {Causes} grievances in the books and {Standing} guilds' standing in towns read back from the save",
            pacts,
            clocks,
            causes,
            standing
        );
    }
}
