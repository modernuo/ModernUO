using System;
using System.IO;
using Server.Logging;

namespace Server.Misc
{
    public class AutoSave : Timer
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(Persistence));

        public static TimeSpan Delay { get; private set; }
        public static TimeSpan Warning { get; private set; }
        public static string BackupPath { get; private set; }

        public AutoSave() : base(Delay - Warning, Delay) => Priority = TimerPriority.OneMinute;

        public static bool SavesEnabled { get; set; } = true;

        public static void Configure()
        {
            BackupPath = ServerConfiguration.GetOrUpdateSetting("autosave.backupPath", "Backups/Automatic");
            Delay = ServerConfiguration.GetOrUpdateSetting("autosave.saveDelay", TimeSpan.FromMinutes(5.0));
            Warning = ServerConfiguration.GetOrUpdateSetting("autosave.warningDelay", TimeSpan.Zero);
        }

        public static void Initialize()
        {
            new AutoSave().Start();
            CommandSystem.Register("SetSaves", AccessLevel.Administrator, SetSaves_OnCommand);

            EventSink.WorldSavePostSnapshot += Backup;
        }

        [Usage("SetSaves <true | false>"), Description("Enables or disables automatic shard saving.")]
        public static void SetSaves_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
            {
                SavesEnabled = e.GetBoolean(0);
                e.Mobile.SendMessage("Saves have been {0}.", SavesEnabled ? "enabled" : "disabled");
            }
            else
            {
                e.Mobile.SendMessage("Format: SetSaves <true | false>");
            }
        }

        protected override void OnTick()
        {
            if (!SavesEnabled || AutoRestart.Restarting)
            {
                return;
            }

            if (Warning == TimeSpan.Zero)
            {
                Save();
            }
            else
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

                DelayCall(Warning, Save);
            }
        }

        public static void Save()
        {
            if (!AutoRestart.Restarting && World.WorldState == WorldState.Running)
            {
                World.Save();
            }
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
        }
    }
}
