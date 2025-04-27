using System;

namespace Server.Engines.Events;

public sealed class CallbackScheduledEvent : ScheduledEvent
{
    private readonly Action _callback;

    public CallbackScheduledEvent(
        DateTime afterUtc,
        Action callback,
        IRecurrencePattern recurrencePattern = null,
        TimeZoneInfo timeZone = null
    ) : base(afterUtc, recurrencePattern, timeZone) =>
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));

    public CallbackScheduledEvent(
        DateTime afterUtc,
        DateTime endDate,
        Action callback,
        IRecurrencePattern recurrencePattern = null,
        TimeZoneInfo timeZone = null
    ) : base(afterUtc, endDate, recurrencePattern, timeZone) =>
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));


    public override void OnEvent() => _callback();
}
