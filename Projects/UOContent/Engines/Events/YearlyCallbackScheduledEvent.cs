using System;

namespace Server.Engines.Events;

public class YearlyCallbackScheduledEvent : YearlyScheduledEvent
{
    private readonly Action _callback;

    protected YearlyCallbackScheduledEvent(
        DateTime startAfter,
        TimeOnly time,
        MonthDay yearlyStart,
        MonthDay yearlyEnd,
        Action callback,
        IRecurrencePattern recurrence,
        TimeZoneInfo timeZone = null
    ) : this(startAfter, DateTime.MaxValue, time, yearlyStart, yearlyEnd, callback, recurrence, timeZone)
    {
    }

    protected YearlyCallbackScheduledEvent(
        DateTime startAfter,
        DateTime endOn,
        TimeOnly time,
        MonthDay yearlyStart,
        MonthDay yearlyEnd,
        Action callback,
        IRecurrencePattern recurrence,
        TimeZoneInfo timeZone = null
    ) : base(startAfter, endOn, time, yearlyStart, yearlyEnd, recurrence, timeZone) => _callback = callback;

    public override void OnEvent() => _callback();
}
