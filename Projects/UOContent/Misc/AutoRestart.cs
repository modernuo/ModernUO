using System;

namespace Server.Misc
{
    public class AutoRestart : Timer
    {
        public static bool Enabled; // is the script enabled?

        private static readonly TimeSpan RestartTime = TimeSpan.FromHours(2.0); // time of day at which to restart

        private static readonly TimeSpan
            RestartDelay =
                TimeSpan.Zero; // how long the server should remain active before restart (period of 'server wars')

        private static readonly TimeSpan
            WarningDelay = TimeSpan.FromMinutes(1.0); // at what interval should the shutdown message be displayed?

        private static DateTime m_RestartTime;

        public AutoRestart() : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
        {
            Priority = TimerPriority.FiveSeconds;

            m_RestartTime = DateTime.UtcNow.Date + RestartTime;

            if (m_RestartTime < DateTime.UtcNow)
            {
                m_RestartTime += TimeSpan.FromDays(1.0);
            }
        }

        public static bool Restarting { get; private set; }

        public static void Initialize()
        {
            CommandSystem.Register("Restart", AccessLevel.Administrator, Restart_OnCommand);
            new AutoRestart().Start();
        }

        public static void Restart_OnCommand(CommandEventArgs e)
        {
            if (Restarting)
            {
                e.Mobile.SendMessage("The server is already restarting.");
            }
            else
            {
                e.Mobile.SendMessage("You have initiated server shutdown.");
                Enabled = true;
                m_RestartTime = DateTime.UtcNow;
            }
        }

        private static void Warning_Callback()
        {
            World.Broadcast(0x22, true, "The server is going down shortly.");
        }

        private static void Restart_Callback()
        {
            Core.Kill(true);
        }

        protected override void OnTick()
        {
            if (Restarting || !Enabled)
            {
                return;
            }

            if (DateTime.UtcNow < m_RestartTime)
            {
                return;
            }

            if (WarningDelay > TimeSpan.Zero)
            {
                Warning_Callback();
                DelayCall(WarningDelay, WarningDelay, Warning_Callback);
            }

            AutoSave.Save();

            Restarting = true;

            DelayCall(RestartDelay, Restart_Callback);
        }
    }
}
