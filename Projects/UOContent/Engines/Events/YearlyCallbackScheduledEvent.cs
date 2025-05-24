using System;

namespace Server.Engines.Events;

public class YearlyCallbackScheduledEvent : YearlyScheduledEvent
{
    private readonly Action _callback;

    protected YearlyCallbackScheduledEvent(
        TimeOnly time,
        MonthDay yearlyStart,
        MonthDay yearlyEnd,
        Action callback,
        IRecurrencePattern recurrence
    ) : this(time, yearlyStart, yearlyEnd, DateTime.MaxValue, callback, recurrence)
    {
    }

    protected YearlyCallbackScheduledEvent(
        TimeOnly time,
        MonthDay yearlyStart,
        MonthDay yearlyEnd,
        DateTime endOn,
        Action callback,
        IRecurrencePattern recurrence
    ) : base(time, yearlyStart, yearlyEnd, endOn, recurrence) => _callback = callback;

    public override void OnEvent() => _callback();
}
