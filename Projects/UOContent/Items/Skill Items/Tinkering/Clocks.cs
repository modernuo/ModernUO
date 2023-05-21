using System;
using ModernUO.Serialization;

namespace Server.Items;

public enum MoonPhase
{
    NewMoon,
    WaxingCrescentMoon,
    FirstQuarter,
    WaxingGibbous,
    FullMoon,
    WaningGibbous,
    LastQuarter,
    WaningCrescent
}

[Flippable(0x104B, 0x104C)]
[SerializationGenerator(0, false)]
public partial class Clock : Item
{
    public const double SecondsPerUOMinute = 5.0;
    public const double MinutesPerUODay = SecondsPerUOMinute * 24;

    private static readonly DateTime WorldStart = new(1997, 9, 1);

    [Constructible]
    public Clock(int itemID = 0x104B) : base(itemID) => Weight = 3.0;

    public static DateTime ServerStart { get; private set; }

    public static void Configure()
    {
        ServerStart = Core.Now;
    }

    public static MoonPhase GetMoonPhase(Map map, int x, int y)
    {
        GetTime(map, x, y, out _, out _, out var totalMinutes);

        if (map != null)
        {
            totalMinutes /= 10 + map.MapIndex * 20;
        }

        return (MoonPhase)(totalMinutes % 8);
    }

    public static void GetTime(Map map, int x, int y, out int hours, out int minutes)
    {
        GetTime(map, x, y, out hours, out minutes, out _);
    }

    public static void GetTime(Map map, int x, int y, out int hours, out int minutes, out int totalMinutes)
    {
        var timeSpan = Core.Now - WorldStart;

        totalMinutes = (int)(timeSpan.TotalSeconds / SecondsPerUOMinute);

        if (map != null)
        {
            totalMinutes += map.MapIndex * 320;
        }

        // Really on OSI this must be by subserver
        totalMinutes += x / 16;

        hours = totalMinutes / 60 % 24;
        minutes = totalMinutes % 60;
    }

    public static void GetTime(out int generalNumber, out string exactTime)
    {
        GetTime(null, 0, 0, out generalNumber, out exactTime);
    }

    public static void GetTime(Mobile from, out int generalNumber, out string exactTime)
    {
        GetTime(from.Map, from.X, from.Y, out generalNumber, out exactTime);
    }

    public static void GetTime(Map map, int x, int y, out int generalNumber, out string exactTime)
    {
        GetTime(map, x, y, out var hours, out int minutes);

        // 00:00 AM - 00:59 AM : Witching hour
        // 01:00 AM - 03:59 AM : Middle of night
        // 04:00 AM - 07:59 AM : Early morning
        // 08:00 AM - 11:59 AM : Late morning
        // 12:00 PM - 12:59 PM : Noon
        // 01:00 PM - 03:59 PM : Afternoon
        // 04:00 PM - 07:59 PM : Early evening
        // 08:00 PM - 11:59 AM : Late at night

        generalNumber = hours switch
        {
            >= 20 => 1042957, // It's late at night
            >= 16 => 1042956, // It's early in the evening
            >= 13 => 1042955, // It's the afternoon
            >= 12 => 1042954, // It's around noon
            >= 08 => 1042953, // It's late in the morning
            >= 04 => 1042952, // It's early in the morning
            >= 01 => 1042951, // It's the middle of the night
            _     => 1042950  // 'Tis the witching hour. 12 Midnight.
        };

        hours %= 12;

        if (hours == 0)
        {
            hours = 12;
        }

        exactTime = $"{hours}:{minutes:D2}";
    }

    public override void OnDoubleClick(Mobile from)
    {
        GetTime(from, out var genericNumber, out var exactTime);

        SendLocalizedMessageTo(from, genericNumber);
        SendLocalizedMessageTo(from, 1042958, exactTime); // ~1_TIME~ to be exact
    }
}

[Flippable(0x104B, 0x104C)]
[SerializationGenerator(0, false)]
public partial class ClockRight : Clock
{
    [Constructible]
    public ClockRight()
    {
    }
}

[Flippable(0x104B, 0x104C)]
[SerializationGenerator(0, false)]
public partial class ClockLeft : Clock
{
    [Constructible]
    public ClockLeft() : base(0x104C)
    {
    }
}
