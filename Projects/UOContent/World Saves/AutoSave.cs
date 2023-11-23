using System;
using Server.Misc;

namespace Server.Saves
{
    public class AutoSave : Timer
    {
        private static Timer _autoSave;
        private static bool _enabled;
        private static DateTime _nextSave;

        public static TimeSpan Delay { get; private set; }
        public static TimeSpan Warning { get; private set; }

        public static bool SavesEnabled
        {
            get => _enabled;
            set
            {
                _enabled = value;

                if (value)
                {
                    SetNextSave(Core.Now.ToSystemLocalTime());
                    _autoSave ??= new AutoSave();
                    _autoSave.Start();
                }
                else
                {
                    _autoSave?.Stop();
                    _autoSave = null;
                }
            }
        }

        public static void Configure()
        {
            Delay = ServerConfiguration.GetOrUpdateSetting("autosave.saveDelay", TimeSpan.FromMinutes(5.0));
            Warning = ServerConfiguration.GetOrUpdateSetting("autosave.warningDelay", TimeSpan.Zero);
        }

        public static void Initialize()
        {
            SavesEnabled = true;
        }

        public static void ResetAutoSave(TimeSpan saveDelay, TimeSpan warningDelay)
        {
            if (saveDelay != Delay || warningDelay != Warning)
            {
                Delay = saveDelay;
                Warning = warningDelay;

                ServerConfiguration.SetSetting("autosave.saveDelay", Delay);
                ServerConfiguration.SetSetting("autosave.warningDelay", Warning);
            }

            SavesEnabled = true;
        }

        private static void SetNextSave(DateTime now)
        {
            var timeOfDay = now.TimeOfDay;
            var delay = Delay.Ticks;

            _nextSave = (now + TimeSpan.FromTicks(delay - timeOfDay.Ticks % delay) - Warning).ToUniversalTime();
        }

        public AutoSave() : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
        {
        }

        protected override void OnTick()
        {
            if (_nextSave > Core.Now)
            {
                return;
            }

            if (SavesEnabled && !AutoRestart.Restarting && !Core.Closing && !World.Saving)
            {
                if (Warning > TimeSpan.Zero)
                {
                    BroadcastWarning();
                    StartTimer(Warning, Save);
                }
                else
                {
                    Save();
                }
            }

            _nextSave += Delay;
        }

        private static void BroadcastWarning()
        {
            var m = Math.DivRem((int)Math.Round(Warning.TotalSeconds), 60, out var s);

            if (m > 0 && s > 0)
            {
                World.Broadcast(
                    0x35,
                    true,
                    $"The world will save in {m} minute{(m != 1 ? "s" : "")} and {s} second{(s != 1 ? "s" : "")}."
                );
            }
            else if (m > 0)
            {
                World.Broadcast(0x35, true, $"The world will save in {m} minute{(m != 1 ? "s" : "")}.");
            }
            else
            {
                World.Broadcast(0x35, true, $"The world will save in {s} second{(s != 1 ? "s" : "")}.");
            }
        }

        public static void Save()
        {
            if (!AutoRestart.Restarting && World.Running)
            {
                World.Save();
            }
        }
    }
}
