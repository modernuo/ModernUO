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
    public TimeSpan Interval { get; }
    private TimeSpan m_Offset;
    private readonly IEvent m_Event;

    public EventScheduleEntry(IEvent e, DateTime firstSpawn, TimeSpan interval, TimeSpan offset)
    {
      m_Offset = offset;
      m_Event = e;
      Interval = interval;
      NextOccurrence = firstSpawn;
    }

    public void Occur()
    {
      NextOccurrence += Interval;

      m_Event?.OnEventScheduled();
    }

    public override string ToString()
    {
      return m_Event?.ToString();
    }
  }
  public class EventScheduler : Timer
  {
    private readonly List<EventScheduleEntry> m_Schedule = new List<EventScheduleEntry>();

    public static EventScheduler Instance => new EventScheduler();
    public static List<IEvent> AvailableEvents => new List<IEvent>();

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

      ScheduleEvent(new EventScheduleEntry(e, firstRun, interval, TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(min)));
    }

    public void ScheduleEvent(EventScheduleEntry e)
    {
      m_Schedule.Add(e);
    }

    public void RemoveEvent(EventScheduleEntry entry)
    {
      m_Schedule.Remove(entry);
    }

    protected override void OnTick()
    {
      foreach (EventScheduleEntry entry in m_Schedule)
        if (entry.NextOccurrence <= DateTime.UtcNow)
          entry.Occur();
    }
  }
}
