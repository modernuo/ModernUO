using System;
using System.IO;
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
        public static string BackupPath { get; private set; }

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
            var defaultBackupPath = Path.Combine(Core.BaseDirectory, "Backups/Automatic");
            BackupPath = ServerConfiguration.GetOrUpdateSetting("autosave.backupPath", defaultBackupPath);

            Delay = ServerConfiguration.GetOrUpdateSetting("autosave.saveDelay", TimeSpan.FromMinutes(5.0));
            Warning = ServerConfiguration.GetOrUpdateSetting("autosave.warningDelay", TimeSpan.Zero);
        }

        public static void Initialize()
        {
            EventSink.WorldSavePostSnapshot += Backup;
            SavesEnabled = true;
        }

        private static void Backup(WorldSavePostSnapshotEventArgs args)
        {
            if (!Directory.Exists(args.OldSavePath))
            {
                return;
            }

            var backupPath = Path.Combine(BackupPath, Utility.GetTimeStamp());
            AssemblyHandler.EnsureDirectory(BackupPath);
            Directory.Move(args.OldSavePath, backupPath);

            logger.Information($"Created backup at {backupPath}");

            ArchiveSaves.Archive?.Invoke(Core.Now);
        }

        public static void ResetAutoSave(TimeSpan saveDelay, TimeSpan warningDelay)
        {
            Delay = saveDelay;
            Warning = warningDelay;

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

            if (SavesEnabled && !AutoRestart.Restarting && !Core.Closing && World.Running)
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

            _nextSave += Delay - Warning;
        }

        private static void BroadcastWarning()
        {
            var s = (int)Warning.TotalSeconds;
            var m = s / 60;
            s %= 60;

            if (m > 0 && s > 0)
            {
                World.Broadcast(
                    0x35,
                    true,
                    "The world will save in {0} minute{1} and {2} second{3}.",
                    m,
                    m != 1 ? "s" : "",
                    s,
                    s != 1 ? "s" : ""
                );
            }
            else if (m > 0)
            {
                World.Broadcast(0x35, true, "The world will save in {0} minute{1}.", m, m != 1 ? "s" : "");
            }
            else
            {
                World.Broadcast(0x35, true, "The world will save in {0} second{1}.", s, s != 1 ? "s" : "");
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
