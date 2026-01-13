using System;

namespace Server.Engines.Events;

[Flags]
public enum AllowedDays : byte
{
    None = 0,
    Sunday = 1 << 0,
    Monday = 1 << 1,
    Tuesday = 1 << 2,
    Wednesday = 1 << 3,
    Thursday = 1 << 4,
    Friday = 1 << 5,
    Saturday = 1 << 6,
    All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}

public static class DaysOfWeekExtension
{
    public static AllowedDays ToDaysOfWeek(this DayOfWeek dayOfWeek) => (AllowedDays)(1 << (int)dayOfWeek);
}
