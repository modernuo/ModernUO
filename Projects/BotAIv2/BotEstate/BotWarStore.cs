using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the war ledger across restarts: the wars standing, with their clocks, kills and plunder; the truces
/// and the once-a-day declaration clocks; and the halls a lost war still owes a move.
///
/// <para>
/// <b>Until 14.09.2026 a restart was an amnesty.</b> <c>BotWar.Reconcile</c> ended every engine war left
/// standing at boot because the ledger had no score or clock for it, and the truces and declaration clocks
/// were in memory, so the Crown, beaten at 02:41, was free to declare on the Blade again at 02:49: the
/// day's truce lasted eight minutes. On a night of hourly builds the rules Patrick asked for ("once a day")
/// meant nothing.
/// </para>
///
/// <para>
/// <b>Clocks are saved as durations, not as ticks.</b> <c>Core.TickCount</c> is the process's own uptime and
/// means nothing to the next process; what is written is how long a war has run, how long a truce has left,
/// how long ago a guild last declared, and each is rebased against the new clock on load. The minutes the
/// shard was down are not counted against anybody: a truce of a day is a day of the shard running.
/// </para>
/// </summary>
public sealed class BotWarStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWarStore));

    private const int Shape = 2;

    private const int Oldest = 1;

    private static BotWarStore _store;

    public static void Configure() => _store ??= new BotWarStore();

    public BotWarStore() : base("BotWars", 16)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);

        var now = Core.TickCount;

        List<BotWar.War> wars = [];

        foreach (var war in BotWar.Standing)
        {
            wars.Add(war);
        }

        writer.WriteEncodedInt(wars.Count);

        for (var i = 0; i < wars.Count; i++)
        {
            var war = wars[i];

            writer.Write(war.A);
            writer.Write(war.B);
            writer.Write(war.Declarer ?? "");
            writer.Write(war.Why ?? "");
            writer.Write(now - war.Began);
            writer.WriteEncodedInt(war.KillsA);
            writer.WriteEncodedInt(war.KillsB);
            writer.WriteEncodedInt(war.LootA);
            writer.WriteEncodedInt(war.LootB);
            writer.Write(war.Joined != null);

            if (war.Joined != null)
            {
                writer.Write(war.Joined.Value.A);
                writer.Write(war.Joined.Value.B);
            }
        }

        List<(string A, string B, long Left)> truces = [];

        foreach (var (pair, until) in BotWar.Truces)
        {
            var left = until - now;

            if (left > 0)
            {
                truces.Add((pair.A, pair.B, left));
            }
        }

        writer.WriteEncodedInt(truces.Count);

        for (var i = 0; i < truces.Count; i++)
        {
            writer.Write(truces[i].A);
            writer.Write(truces[i].B);
            writer.Write(truces[i].Left);
        }

        List<(string Guild, long Ago)> declared = [];

        foreach (var (guild, last) in BotWar.Declarations)
        {
            var ago = now - last;

            if (ago >= 0 && ago < BotWar.DeclareEveryMs)
            {
                declared.Add((guild, ago));
            }
        }

        writer.WriteEncodedInt(declared.Count);

        for (var i = 0; i < declared.Count; i++)
        {
            writer.Write(declared[i].Guild);
            writer.Write(declared[i].Ago);
        }

        List<(string Loser, string Winner)> owed = [];

        foreach (var (loser, winner) in BotExile.Owing)
        {
            owed.Add((loser, winner));
        }

        writer.WriteEncodedInt(owed.Count);

        for (var i = 0; i < owed.Count; i++)
        {
            writer.Write(owed[i].Loser);
            writer.Write(owed[i].Winner);
        }

        // Shape 2: the opinions, so a restart is not a reconciliation. See BotRegard.Restore.
        List<(string Of, string For, double Held)> opinions = [];

        foreach (var (pair, held) in BotRegard.Opinions)
        {
            opinions.Add((pair.Of, pair.For, held));
        }

        writer.WriteEncodedInt(opinions.Count);

        for (var i = 0; i < opinions.Count; i++)
        {
            writer.Write(opinions[i].Of);
            writer.Write(opinions[i].For);
            writer.Write(opinions[i].Held);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < Oldest || shape > Shape)
        {
            logger.Warning(
                "The saved wars are shape {Found} and this build reads {Oldest} to {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotWars/BotWars.bin is deleted",
                shape,
                Oldest,
                Shape
            );

            return;
        }

        var wars = reader.ReadEncodedInt();

        for (var i = 0; i < wars; i++)
        {
            var war = new BotWar.War
            {
                A = reader.ReadString(),
                B = reader.ReadString(),
                Declarer = reader.ReadString(),
                Why = reader.ReadString()
            };

            var elapsed = reader.ReadLong();

            war.KillsA = reader.ReadEncodedInt();
            war.KillsB = reader.ReadEncodedInt();
            war.LootA = reader.ReadEncodedInt();
            war.LootB = reader.ReadEncodedInt();

            if (reader.ReadBool())
            {
                var a = reader.ReadString();
                var b = reader.ReadString();

                war.Joined = (a, b);
            }

            BotWar.Restore(war, elapsed);
        }

        var truces = reader.ReadEncodedInt();

        for (var i = 0; i < truces; i++)
        {
            var a = reader.ReadString();
            var b = reader.ReadString();
            var left = reader.ReadLong();

            BotWar.RestoreTruce(a, b, left);
        }

        var declared = reader.ReadEncodedInt();

        for (var i = 0; i < declared; i++)
        {
            var guild = reader.ReadString();
            var ago = reader.ReadLong();

            BotWar.RestoreDeclared(guild, ago);
        }

        var owed = reader.ReadEncodedInt();

        for (var i = 0; i < owed; i++)
        {
            var loser = reader.ReadString();
            var winner = reader.ReadString();

            BotExile.Restore(loser, winner);
        }

        var opinions = 0;

        if (shape >= 2)
        {
            opinions = reader.ReadEncodedInt();

            for (var i = 0; i < opinions; i++)
            {
                var of = reader.ReadString();
                var about = reader.ReadString();
                var held = reader.ReadDouble();

                BotRegard.Restore(of, about, held);
            }
        }

        if (wars + truces + declared + owed + opinions > 0)
        {
            logger.Information(
                "{Wars} wars, {Truces} truces, {Declared} declaration clocks, {Owed} owed moves and {Opinions} opinions read back off the save",
                wars,
                truces,
                declared,
                owed,
                opinions
            );
        }
    }
}
