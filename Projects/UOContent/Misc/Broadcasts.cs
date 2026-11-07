using System.Threading;

namespace Server.Misc
{
    public static class Broadcasts
    {
        public static void Initialize()
        {
            EventSink.ServerCrashed += EventSink_Crashed;
            EventSink.Shutdown += EventSink_Shutdown;
        }

        public static void EventSink_Crashed(ServerCrashedEventArgs e)
        {
            // A crash the loop could not take runs here off the loop, where NetState is not ours to touch.
            if (Thread.CurrentThread != Core.Thread)
            {
                return;
            }

            try
            {
                World.Broadcast(0x35, true, "The server has crashed.");
            }
            catch
            {
                // ignored
            }
        }

        public static void EventSink_Shutdown()
        {
            try
            {
                World.Broadcast(0x35, true, "The server has shut down.");
            }
            catch
            {
                // ignored
            }
        }
    }
}
