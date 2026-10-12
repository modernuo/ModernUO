using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps what the events owe across a restart, so that no coin is made or lost by one (07.10.2026). Written with the world save:
/// what an invasion and a caravan hold of the city's purse, the invasion's creatures, the fair's day and turn and its lottery
/// tickets, and the rumoured chest. Read back, nothing is resumed but the rumour and the fair's calendar: the held coin goes back to
/// the purse, the creatures are taken off the island, and every ticket is handed back to its holder — at the module's start, once
/// the world and the city's purse are read (<see cref="Apply"/>).
/// </summary>
public sealed class BotEventsStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotEventsStore));

    private const int Shape = 1;

    private static BotEventsStore _store;

    public static void Configure() => _store ??= new BotEventsStore();

    public BotEventsStore() : base("BotEvents", 19)
    {
    }

    private static int _held;

    private static readonly List<Serial> _creatures = [];

    private static readonly List<(Serial Who, int Paid)> _tickets = [];

    private static bool _rumour;

    private static Serial _chest;

    private static int _town;

    private static bool _lair;

    private static double _asks;

    private static long _left;

    private static bool _read;

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);

        writer.WriteEncodedInt(BotInvasion.Holding + BotCaravans.Holding);

        List<Serial> creatures = [];

        foreach (var c in BotInvasion.Standing())
        {
            if (c is { Deleted: false })
            {
                creatures.Add(c.Serial);
            }
        }

        writer.WriteEncodedInt(creatures.Count);

        for (var i = 0; i < creatures.Count; i++)
        {
            writer.Write(creatures[i]);
        }

        writer.Write(BotFair.LastUtc);
        writer.WriteEncodedInt(BotFair.Turn);

        List<(Serial Who, int Paid)> tickets = [..BotFair.Holders()];

        writer.WriteEncodedInt(tickets.Count);

        for (var i = 0; i < tickets.Count; i++)
        {
            writer.Write(tickets[i].Who);
            writer.WriteEncodedInt(tickets[i].Paid);
        }

        var tale = BotRumour.Kept;

        writer.Write(tale != null);

        if (tale != null)
        {
            writer.Write(tale.Chest.Serial);
            writer.WriteEncodedInt(BotRumour.TownIndex(tale.Town));
            writer.Write(tale.Lair);
            writer.Write(tale.Asks);
            writer.Write(Math.Max(0L, tale.LapseTick - Core.TickCount));
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning(
                "The saved events are shape {Found} and this build reads {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotEvents/BotEvents.bin is deleted",
                shape,
                Shape
            );

            return;
        }

        _held = reader.ReadEncodedInt();

        var creatures = reader.ReadEncodedInt();

        for (var i = 0; i < creatures; i++)
        {
            _creatures.Add(reader.ReadSerial());
        }

        BotFair.LastUtc = reader.ReadDateTime();
        BotFair.Turn = reader.ReadEncodedInt();

        var tickets = reader.ReadEncodedInt();

        for (var i = 0; i < tickets; i++)
        {
            var who = reader.ReadSerial();
            var paid = reader.ReadEncodedInt();

            _tickets.Add((who, paid));
        }

        _rumour = reader.ReadBool();

        if (_rumour)
        {
            _chest = reader.ReadSerial();
            _town = reader.ReadEncodedInt();
            _lair = reader.ReadBool();
            _asks = reader.ReadDouble();
            _left = reader.ReadLong();
        }

        _read = true;
    }

    /// <summary>What was read back, settled: the hold back to the purse, the creatures away, the tickets back, the rumour open again.</summary>
    public static void Apply()
    {
        if (!_read)
        {
            return;
        }

        _read = false;

        if (_held > 0)
        {
            BotSeasons.HeldBefore(_held);
            BotSeasons.Return(_held);
        }

        var gone = 0;

        for (var i = 0; i < _creatures.Count; i++)
        {
            if (World.FindMobile(_creatures[i]) is BaseCreature { Deleted: false } c)
            {
                c.Delete();
                gone++;
            }
        }

        var refunded = 0;

        if (_tickets.Count > 0)
        {
            BotFair.Refund(_tickets);

            for (var i = 0; i < _tickets.Count; i++)
            {
                refunded += _tickets[i].Paid;
            }
        }

        var chest = _rumour ? World.FindItem(_chest) as Container : null;

        if (chest != null)
        {
            BotRumour.Restore(chest, _town, _lair, _asks, _left);
        }

        logger.Information(
            "The events were read back: {Held}gp held for an invasion or a caravan back to the city's purse, {Gone} of an invasion's creatures taken off the island, {Tickets} lottery tickets ({Refunded}gp) handed back, {Rumour}; the last fair opened {Last}",
            _held,
            gone,
            _tickets.Count,
            refunded,
            chest != null ? $"the rumoured chest at ({chest.X}, {chest.Y}) open again for {_left / 60000} minutes" : "no rumour standing",
            BotFair.LastUtc == DateTime.MinValue ? "never" : $"{BotFair.LastUtc:yyyy-MM-dd HH:mm} UTC"
        );

        _held = 0;
        _creatures.Clear();
        _tickets.Clear();
        _rumour = false;
    }
}
