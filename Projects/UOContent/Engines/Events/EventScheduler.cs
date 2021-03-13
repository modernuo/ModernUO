using System;
using System.Collections.Generic;

namespace Server.Engines.Events
{
    public interface IEvent
    {
        void OnEventScheduled();
    }

    public class EventScheduleEntry
    {
        private readonly IEvent _event;
        private TimeSpan _offset;

        public EventScheduleEntry(IEvent e, DateTime firstSpawn, TimeSpan interval, TimeSpan offset)
        {
            _offset = offset;
            _event = e;
            Interval = interval;
            NextOccurrence = firstSpawn;
        }

        public DateTime NextOccurrence { get; private set; }
        public TimeSpan Interval { get; }

        public void Occur()
        {
            NextOccurrence += Interval;

            _event?.OnEventScheduled();
        }

        public override string ToString() => _event?.ToString();
    }

    public class EventScheduler : Timer
    {
        private static EventScheduler _instance;
        private readonly List<EventScheduleEntry> _schedule = new();

        private EventScheduler() : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
        {
        }

        public static EventScheduler Instance => _instance ??= new EventScheduler();
        public static List<IEvent> AvailableEvents { get; } = new();

        public static void Initialize()
        {
            Instance.Start();
        }

        public void ScheduleEvent(IEvent e, int hour, int min)
        {
            ScheduleEvent(e, hour, min, TimeSpan.FromDays(1.0));
        }

        public void ScheduleEvent(IEvent e, int hour, int min, TimeSpan interval)
        {
            var now = Core.Now;
            var firstRun = new DateTime(now.Year, now.Month, now.Day, hour, min, 0);

            while (now > firstRun)
            {
                firstRun += interval;
            }

            ScheduleEvent(
                new EventScheduleEntry(e, firstRun, interval, TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(min))
            );
        }

        public void ScheduleEvent(EventScheduleEntry e)
        {
            _schedule.Add(e);
        }

        public void RemoveEvent(EventScheduleEntry entry)
        {
            _schedule.Remove(entry);
        }

        protected override void OnTick()
        {
            foreach (var entry in _schedule)
            {
                if (entry.NextOccurrence <= Core.Now)
                {
                    entry.Occur();
                }
            }
        }
    }
}
