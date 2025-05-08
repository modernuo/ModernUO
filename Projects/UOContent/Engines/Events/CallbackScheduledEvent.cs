using System;

namespace Server.Engines.Events;

public sealed class CallbackScheduledEvent : ScheduledEvent
{
    private readonly Action _callback;

    public CallbackScheduledEvent(
        TimeOnly time,
        Action callback,
        IRecurrencePattern recurrencePattern = null
    ) : base(time, recurrencePattern) => _callback = callback ?? throw new ArgumentNullException(nameof(callback));

    public CallbackScheduledEvent(
        TimeOnly time,
        DateTime endOn,
        Action callback,
        IRecurrencePattern recurrencePattern = null
    ) : base(time, endOn, recurrencePattern) => _callback = callback ?? throw new ArgumentNullException(nameof(callback));

    public override void OnEvent() => _callback();
}
