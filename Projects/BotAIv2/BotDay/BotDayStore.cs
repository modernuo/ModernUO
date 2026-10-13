using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps every bot's timetable across restarts. See <see cref="BotTimetable"/>: drawn from the name, the class and the nature once
/// and then kept, so a dial change to the drawing never rewrites who anybody already is — the rule <see cref="BotCharacterStore"/>
/// set for the natures. By name, never by serial, for the same reason: a bot is raised afresh at every boot. A shape this build
/// cannot read is dropped whole, and every timetable is drawn again from the names, which gives the same timetables under the
/// same dials.
/// </summary>
public sealed class BotDayStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDayStore));

    private const int Shape = 1;

    private static BotDayStore _store;

    public static void Configure() => _store ??= new BotDayStore();

    public BotDayStore() : base("BotDay", 24)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        writer.WriteEncodedInt(BotTimetables.Count);

        foreach (var table in BotTimetables.All)
        {
            writer.Write(table.Name);
            writer.Write(table.Drawn);
            writer.Write(table.Wake);
            writer.Write(table.Bed);
            writer.Write(table.Meal);
            writer.Write(table.Supper);
            writer.WriteEncodedInt((int)table.Habit);
            writer.Write(table.Owl);
            writer.WriteEncodedInt((int)table.Place);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning("The saved timetables are shape {Found} and this build reads {Wanted}; every timetable is drawn again from the names", shape, Shape);

            return;
        }

        var count = reader.ReadEncodedInt();
        var owls = 0;

        for (var n = 0; n < count; n++)
        {
            var table = new BotTimetable(reader.ReadString())
            {
                Drawn = reader.ReadBool(),
                Wake = reader.ReadDouble(),
                Bed = reader.ReadDouble(),
                Meal = reader.ReadDouble(),
                Supper = reader.ReadDouble(),
                // Held inside the enums: the Day: line indexes stack arrays by them, and a damaged save must not throw every five
                // minutes and on every snapshot (review A of 13.10.2026, §3.7).
                Habit = (BotHabit)Math.Clamp(reader.ReadEncodedInt(), (int)BotHabit.Tavern, (int)BotHabit.Prowl),
                Owl = reader.ReadBool(),
                Place = (BotBedPlace)Math.Clamp(reader.ReadEncodedInt(), (int)BotBedPlace.Home, (int)BotBedPlace.Ground)
            };

            if (string.IsNullOrEmpty(table.Name))
            {
                continue;
            }

            if (table.Owl)
            {
                owls++;
            }

            BotTimetables.Put(table);
        }

        logger.Information("Timetables read back for {Count} bots, {Owls} of them night owls", count, owls);
    }
}
