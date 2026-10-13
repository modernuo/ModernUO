using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// One bot's timetable: when it wakes and goes to bed, when it eats, what it does with its evening, whether it is a night owl
/// and where it would rather sleep. Drawn once from the name, the class and the nature (<see cref="BotTimetables.Draw"/>) and
/// kept by <see cref="BotDayStore"/>, so a dial change to the drawing never rewrites who anybody already is — the rule
/// <see cref="BotTraits"/> set on 07.10.2026. Hours are local, from nought to twenty-four, as <see cref="BotPrime"/> reads them.
/// </summary>
public sealed class BotTimetable
{
    public BotTimetable(string name) => Name = name;

    public string Name { get; }

    /// <summary>The hour it would be back in the world by, if the floor lets it.</summary>
    public double Wake;

    /// <summary>The hour from which it is at its bedtime, for <see cref="BotDay.BedWindowHours"/> hours.</summary>
    public double Bed;

    /// <summary>The midday meal's hour, and the evening meal's: supper is raised within <see cref="BotDay.MealWindowHours"/> of either.</summary>
    public double Meal;

    public double Supper;

    public BotHabit Habit;

    /// <summary>A night owl lives the same day <see cref="BotDay.OwlShift"/> hours later: to bed in the morning, its evening before dawn.</summary>
    public bool Owl;

    /// <summary>Where it would rather sleep when it has no house of its own: an inn, or a fire in the field.</summary>
    public BotBedPlace Place;

    /// <summary>Whether the fields have been drawn; false only for a record read back empty.</summary>
    public bool Drawn;

    /// <summary>A thief of The Shadow lives by night whatever was drawn; asked of <c>BotCrime</c> once a minute. Not saved.</summary>
    public bool Thief;

    internal bool ThiefChecked;

    internal long ThiefCheckedTick;

    /// <summary>The habit's word for the take line and the page.</summary>
    public static string Word(BotHabit habit) =>
        habit switch
        {
            BotHabit.Tavern => "tavern",
            BotHabit.Fireside => "fireside",
            BotHabit.Home => "home",
            _ => "prowl"
        };

    public static string Word(BotBedPlace place) =>
        place switch
        {
            BotBedPlace.Home => "home",
            BotBedPlace.Inn => "inn",
            BotBedPlace.Field => "field",
            _ => "ground"
        };

    public override string ToString() =>
        $"wakes {Wake:0.0}h, to bed {Bed:0.0}h, eats {Meal:0.0}h and {Supper:0.0}h, evenings at the {Word(Habit)}, sleeps at the {Word(Place)}{(Owl ? ", a night owl" : "")}";
}

/// <summary>The six named parts of the day. See <see cref="BotDay"/>.</summary>
public enum BotDayPart
{
    Dawn,
    Morning,
    Noon,
    Afternoon,
    Evening,
    Night
}

/// <summary>What a bot does with its evening, drawn at birth from its nature. See <see cref="BotTimetable"/>.</summary>
public enum BotHabit
{
    Tavern,
    Fireside,
    Home,
    Prowl
}

/// <summary>Where a bot sleeps: its own house, an inn, by a fire in the field, or on the open ground when nothing took it.</summary>
public enum BotBedPlace
{
    Home,
    Inn,
    Field,
    Ground
}

/// <summary>
/// The register of timetables: by name for the store, by serial for the auction, which asks once an offer and must not hash
/// a string to do it — the shape of <see cref="BotLives"/>.
/// </summary>
public static class BotTimetables
{
    private static readonly Dictionary<string, BotTimetable> _byName = new(StringComparer.Ordinal);

    private static readonly Dictionary<Serial, BotTimetable> _bySerial = [];

    public static IEnumerable<BotTimetable> All => _byName.Values;

    public static int Count => _byName.Count;

    /// <summary>Timetables drawn fresh in this run, as against read back from the save.</summary>
    public static long Drawn { get; private set; }

    /// <summary>The classes whose members are night owls one in <see cref="BotDay.OwlShareFighters"/>: the ones that fight for a living.</summary>
    private static bool Fighter(string className) =>
        className is "Warrior" or "Archer" or "Brawler" or "Tamer" or "WarriorArcher" or "WarriorMage";

    /// <summary>The timetable for this bot, drawn the first time it is asked for. Null for anything not a bot.</summary>
    public static BotTimetable Of(Mobile m)
    {
        if (m is not BotMobile bot || string.IsNullOrEmpty(bot.Name))
        {
            return null;
        }

        if (_bySerial.TryGetValue(bot.Serial, out var table) && string.Equals(table.Name, bot.Name, StringComparison.Ordinal))
        {
            return table;
        }

        if (!_byName.TryGetValue(bot.Name, out table))
        {
            table = new BotTimetable(bot.Name);
            _byName[bot.Name] = table;
        }

        if (!table.Drawn)
        {
            Draw(table, bot.Class?.Name, bot.Class?.Office ?? false, BotTraits.Of(bot));
            Drawn++;
        }

        _bySerial[bot.Serial] = table;

        return table;
    }

    /// <summary>The timetable for a name if one exists; never makes one.</summary>
    public static BotTimetable Find(string name) =>
        name != null && _byName.TryGetValue(name, out var table) ? table : null;

    /// <summary>A record read back from the save, put in as it was.</summary>
    public static void Put(BotTimetable table)
    {
        if (table?.Name != null)
        {
            _byName[table.Name] = table;
        }
    }

    /// <summary>
    /// The draw: FNV-1a over the name into a seeded <see cref="System.Random"/>, as <see cref="BotTraits.Draw"/> does, with a
    /// different salt so the sequence is not the nature's. The class says who is an owl; the nature leans the hours and the habit.
    /// </summary>
    public static void Draw(BotTimetable table, string className, bool office, BotTrait traits)
    {
        var hash = 2166136261u;
        var name = table.Name ?? "";

        for (var i = 0; i < name.Length; i++)
        {
            hash ^= name[i];
            hash *= 16777619u;
        }

        var rng = new System.Random((int)((hash ^ 0x5A17A9E3u) & 0x7FFFFFFF));

        var homebody = (traits & BotTrait.Homebody) != 0;
        var wanderer = (traits & BotTrait.Wanderer) != 0;
        var sociable = (traits & BotTrait.Sociable) != 0;
        var solitary = (traits & BotTrait.Solitary) != 0;

        // Offices keep the day: a Captain who drills at four in the morning drills nobody.
        var owlShare = office ? 0.0 : Fighter(className) ? BotDay.OwlShareFighters : BotDay.OwlShareOthers;

        table.Owl = rng.NextDouble() < Math.Clamp(owlShare, 0.0, 1.0);

        if (table.Owl)
        {
            // To bed in the morning, up in the afternoon.
            table.Bed = BotDay.OwlBedFrom + rng.NextDouble() * Math.Max(0.0, BotDay.OwlBedSpan);
        }
        else
        {
            var bed = BotDay.BedFrom + rng.NextDouble() * Math.Max(0.0, BotDay.BedSpan);

            if (homebody)
            {
                bed -= 1.0;
            }
            else if (wanderer)
            {
                bed += 0.5;
            }

            if (sociable)
            {
                bed += 0.5;
            }
            else if (solitary)
            {
                bed -= 0.5;
            }

            table.Bed = bed;
        }

        table.Wake = Wrap(table.Bed + 6.5 + rng.NextDouble() * 1.5);
        table.Bed = Wrap(table.Bed);
        table.Meal = table.Owl ? Wrap(table.Wake + 1.0 + rng.NextDouble()) : 11.5 + rng.NextDouble() * 2.0;
        table.Supper = Wrap(table.Bed - 1.5 - rng.NextDouble() * 1.5);

        // The evening's habit: a roulette the nature leans. A homebody without a house is read as a tavern guest at use
        // (BotDay.Fits), so Home is drawn as a leaning rather than a promise.
        var tavern = 1.0;
        var fireside = 1.0;
        var home = 1.0;
        var prowl = 0.5;

        if (sociable)
        {
            tavern *= 2.0;
            fireside *= 1.5;
            home *= 0.5;
        }
        else if (solitary)
        {
            tavern *= 0.5;
            home *= 1.5;
            prowl *= 1.5;
        }

        if (homebody)
        {
            home *= 2.0;
            prowl *= 0.3;
        }
        else if (wanderer)
        {
            fireside *= 1.5;
            prowl *= 2.0;
            home *= 0.5;
        }

        if (table.Owl)
        {
            prowl *= 3.0;
        }

        var roll = rng.NextDouble() * (tavern + fireside + home + prowl);

        table.Habit = roll < tavern ? BotHabit.Tavern
            : roll < tavern + fireside ? BotHabit.Fireside
            : roll < tavern + fireside + home ? BotHabit.Home
            : BotHabit.Prowl;

        // Where it sleeps without a house: wanderers and the solitary in the field more often than not, the sociable and
        // homebodies at an inn, the rest an inn seven times in ten.
        var field = wanderer || solitary ? 0.6 : sociable || homebody ? 0.15 : 0.3;

        table.Place = rng.NextDouble() < field ? BotBedPlace.Field : BotBedPlace.Inn;
        table.Drawn = true;
    }

    /// <summary>An hour folded into the day.</summary>
    public static double Wrap(double hour)
    {
        hour %= 24.0;

        return hour < 0.0 ? hour + 24.0 : hour;
    }

    /// <summary>Re-reads the serial cache down when it has grown past the population, as <see cref="BotLives.Refresh"/> does.</summary>
    public static void Refresh()
    {
        if (_bySerial.Count > BotPopulation.Bots.Count * 2 + 64)
        {
            _bySerial.Clear();
        }
    }

    /// <summary>A new world: everything read again from the save.</summary>
    public static void Forget()
    {
        _byName.Clear();
        _bySerial.Clear();
        Drawn = 0;
    }

    /// <summary>
    /// Patrick's reset (reset26): every timetable forgotten, so the save writes the store empty. A timetable is drawn from the name,
    /// so a name born again draws the same hours; that is the design, as it is for the natures. Returns the timetables forgotten.
    /// </summary>
    public static int Wipe()
    {
        var count = _byName.Count;

        Forget();
        BotDay.Forget();

        return count;
    }
}
