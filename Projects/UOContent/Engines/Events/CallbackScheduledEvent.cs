using System;

namespace Server.Engines.Events;

public sealed class CallbackScheduledEvent : ScheduledEvent
{
    private readonly Action _callback;

    public CallbackScheduledEvent(
        DateTime after,
        TimeOnly time,
        Action callback,
        IRecurrencePattern recurrencePattern = null,
        TimeZoneInfo timeZone = null
    ) : base(after, time, recurrencePattern, timeZone) =>
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));

    public CallbackScheduledEvent(
        DateTime after,
        DateTime endOn,
        TimeOnly time,
        Action callback,
        IRecurrencePattern recurrencePattern = null,
        TimeZoneInfo timeZone = null
    ) : base(after, endOn, time, recurrencePattern, timeZone) =>
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));

    public override void OnEvent() => _callback();
}
