using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Events;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class EventSchedulerTests
{
    // Test implementations for controlled testing
    private class TestRecurrencePattern : IRecurrencePattern
    {
        private readonly TimeSpan _interval;
        private readonly int _maxOccurrences;
        private int _currentOccurrence;

        public TestRecurrencePattern(TimeSpan interval, int maxOccurrences = int.MaxValue)
        {
            _interval = interval;
            _maxOccurrences = maxOccurrences;
            _currentOccurrence = 0;
        }

        public DateTime GetNextOccurrence(DateTime afterUtc, TimeOnly time, TimeZoneInfo timeZone)
        {
            _currentOccurrence++;

            if (_currentOccurrence > _maxOccurrences)
            {
                return DateTime.MaxValue;
            }

            return afterUtc + _interval;
        }
    }

    private class TestScheduledEvent : ScheduledEvent
    {
        public int CallCount { get; private set; }
        public Action Callback { get; }

        public TestScheduledEvent(TimeOnly time, Action callback, IRecurrencePattern recurrence = null)
            : base(time, recurrence)
        {
            CallCount = 0;
            Callback = callback;
        }

        public override void OnEvent()
        {
            CallCount++;
            Callback?.Invoke();
        }
    }

    private static void Init()
    {
        Core._now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        Timer.Init(0);
        EventScheduler.Configure();
    }

    private static void Finish()
    {
        EventScheduler.Shared.Stop();
    }

    [Fact]
    public void ScheduleEvent_ExecutesCallback_HappyPath()
    {
        Init();

        try
        {
            bool called = false;
            var evt = EventScheduler.Shared.ScheduleEvent(
                Core._now,
                TimeOnly.FromDateTime(Core._now),
                () => called = true
            );

            Assert.Equal(Core._now, evt.NextOccurrence);
            Timer.Slice(8);

            Assert.True(called);
            Assert.Equal(DateTime.MaxValue, evt.NextOccurrence);

            evt.Cancel();
        }
        finally
        {
            Finish();
        }
    }

    [Fact]
    public void Scheduler_OrdersEventsByNextOccurrence()
    {
        Init();

        try
        {
            var executionOrder = new List<int>();

            // Create events with staggered occurrences
            var evt1Start = Core._now.AddSeconds(10);
            var evt1 = new TestScheduledEvent(
                TimeOnly.FromDateTime(Core._now.AddSeconds(10)),
                () => executionOrder.Add(1)
            );

            var evt2Start = Core._now.AddSeconds(20);
            var evt2 = new TestScheduledEvent(
                TimeOnly.FromDateTime(evt2Start),
                () => executionOrder.Add(2)
            );

            var evt3Start = Core._now.AddSeconds(30);
            var evt3 = new TestScheduledEvent(
                TimeOnly.FromDateTime(evt3Start),
                () => executionOrder.Add(3)
            );

            // Add out of order
            evt3.Schedule(evt3Start);
            evt1.Schedule(evt1Start);
            evt2.Schedule(evt2Start);

            // Advance time to after all events
            Core._now = Core._now.AddSeconds(40);
            Timer.Slice(8);

            // Verify they executed in time order, not addition order
            Assert.Equal([1, 2, 3], executionOrder);

            evt1.Cancel();
            evt2.Cancel();
            evt3.Cancel();
        }
        finally
        {
            Finish();
        }
    }

    [Fact]
    public void Scheduler_HandlesRecurringEvents()
    {
        for (var i = 0; i < 100_000; i++)
        {
            Init();

            try
            {
                int callCount = 0;

                // Create a recurrence pattern that fires every 10 seconds, up to 3 times
                var recurrence = new TestRecurrencePattern(TimeSpan.FromSeconds(10), 3);

                var evt = new TestScheduledEvent(
                    TimeOnly.FromDateTime(Core._now),
                    () => callCount++,
                    recurrence
                );

                evt.Schedule(Core._now);

                // Advance time to after all occurrences should have happened
                Core._now = Core._now.AddSeconds(50);
                Timer.Slice(8);

                // Should have fired 4 times (3 recurrences)
                Assert.Equal(3, callCount);
                Assert.Equal(3, evt.CallCount);

                evt.Cancel();
            }
            finally
            {
                Finish();
            }
        }
    }

    [Fact]
    public void Scheduler_HandlesExceptionsInEvents()
    {
        Init();

        try
        {
            bool failedEventCalled = false;
            bool laterEventCalled = false;

            // Event that throws exception
            var failedEvt = EventScheduler.Shared.ScheduleEvent(
                Core._now.AddSeconds(10),
                () =>
                {
                    failedEventCalled = true;
                    throw new Exception("Test exception");
                }
            );

            // Later event that should still execute
            var laterEvt = EventScheduler.Shared.ScheduleEvent(
                Core._now.AddSeconds(20),
                () => laterEventCalled = true
            );

            // Advance time to after both events
            Core._now = Core._now.AddSeconds(30);
            Timer.Slice(8);

            // Both should have been called despite the exception
            Assert.True(failedEventCalled);
            Assert.True(laterEventCalled);

            failedEvt.Cancel();
            laterEvt.Cancel();
        }
        finally
        {
            Finish();
        }
    }

    [Fact]
    public void Scheduler_RemovesEventsCorrectly()
    {
        Init();

        try
        {
            bool eventCalled = false;

            var evt = EventScheduler.Shared.ScheduleEvent(
                Core._now.AddSeconds(10),
                () => eventCalled = true
            );

            // Remove before execution
            evt.Cancel();

            // Advance time
            Core._now = Core._now.AddSeconds(20);
            Timer.Slice(8);

            // Event should not have executed
            Assert.False(eventCalled);
        }
        finally
        {
            Finish();
        }
    }

    [Fact]
    public void Scheduler_HandlesEventsWithEndDates()
    {
        Init();

        try
        {
            int callCount = 0;

            // Create a recurrence pattern that fires every 10 seconds
            var recurrence = new TestRecurrencePattern(TimeSpan.FromSeconds(10));

            // Create a custom event with an end date
            var startTime = Core._now;
            var endTime = Core._now.AddSeconds(36);

            var customEvent = new CustomEvent(
                TimeOnly.FromDateTime(startTime),
                endTime,
                recurrence,
                () => callCount++
            );

            customEvent.Schedule(startTime);

            Core._now = Core._now.AddSeconds(50);
            Timer.Slice(8);

            // Should have fired 3 times only (initial + 2 within the timeframe)
            Assert.Equal(3, callCount);
        }
        finally
        {
            Finish();
        }
    }

    private class CustomEvent : ScheduledEvent
    {
        private readonly Action _callback;

        public CustomEvent(
            TimeOnly time,
            DateTime endOn,
            IRecurrencePattern recurrence,
            Action callback
        ) : base(time, endOn, recurrence) => _callback = callback;

        public override void OnEvent()
        {
            _callback?.Invoke();
        }
    }
}
