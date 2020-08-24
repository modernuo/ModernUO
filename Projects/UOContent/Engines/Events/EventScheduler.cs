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
        public DateTime NextOccurrence { get; private set; }
        public TimeSpan Interval { get; private set; }
        private TimeSpan _offset;
        private readonly IEvent _event;

        public EventScheduleEntry(IEvent e, DateTime firstSpawn, TimeSpan interval, TimeSpan offset)
        {
            _offset = offset;
            _event = e;
            Interval = interval;
            NextOccurrence = firstSpawn;
        }

        public void Occur()
        {
            NextOccurrence += Interval;

            _event?.OnEventScheduled();
        }

        public override string ToString()
        {
            return _event?.ToString();
        }
    }

    public class EventScheduler : Timer
    {
        private static EventScheduler _instance;
        private readonly List<EventScheduleEntry> _schedule = new List<EventScheduleEntry>();

        public static EventScheduler Instance => _instance ??= new EventScheduler();
        public static List<IEvent> AvailableEvents { get; } = new List<IEvent>();

        private EventScheduler() : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
        {
        }

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
            DateTime now = DateTime.UtcNow;
            DateTime firstRun = new DateTime(now.Year, now.Month, now.Day, hour, min, 0);

            while (now > firstRun)
                firstRun += interval;

            ScheduleEvent(
                new EventScheduleEntry(e, firstRun, interval, TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(min)));
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
            foreach (EventScheduleEntry entry in _schedule)
                if (entry.NextOccurrence <= DateTime.UtcNow)
                    entry.Occur();
        }
    }
}
