namespace Server.Engines.Events
{
  public class BroadcastEvent : IEvent
  {
    private readonly int m_Hue;
    private readonly string m_Text;

    public static void Initialize()
    {
      /*
      EventScheduler.Instance.ScheduleEvent(new BroadcastEvent(22, "Test Message Please Ignore 2min"), 0, 0, TimeSpan.FromMinutes(2.0));
      EventScheduler.Instance.ScheduleEvent(new BroadcastEvent(33, "Test Message Please Ignore 3min"), 0, 0, TimeSpan.FromMinutes(3.0));
      EventScheduler.Instance.ScheduleEvent(new BroadcastEvent(44, "Test Message Please Ignore 4min"), 0, 0, TimeSpan.FromMinutes(4.0));
      */
    }

    public BroadcastEvent(int hue, string text)
    {
      m_Hue = hue;
      m_Text = text;
    }

    public override string ToString() => $"Broadcast: {m_Text}";

    public void OnEventScheduled()
    {
      World.Broadcast(m_Hue, true, m_Text);
    }
  }
}
