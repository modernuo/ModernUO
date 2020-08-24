namespace Server.Engines.Events
{
    public class BroadcastEvent : IEvent
    {
        private readonly int _hue = 0;
        private readonly string _text = "";

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
            _hue = hue;
            _text = text;
        }

        public override string ToString()
        {
            return $"Broadcast: {_text}";
        }

        public void OnEventScheduled()
        {
            World.Broadcast(_hue, true, _text);
        }
    }
}
