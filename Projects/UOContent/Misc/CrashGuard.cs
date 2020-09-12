using System;
using System.Diagnostics;
using System.IO;
using Server.Accounting;
using Server.Network;

namespace Server.Misc
{
    public static class CrashGuard
    {
        private static bool Enabled;
        private static bool SaveBackup;
        private static bool RestartServer; // Disable this if using a daemon/service
        private static bool GenerateReport;

        public static void Configure()
        {
            Enabled = ServerConfiguration.GetOrUpdateSetting("crashGuard.enabled", true);
            SaveBackup = ServerConfiguration.GetOrUpdateSetting("crashGuard.saveBackup", true);
            RestartServer = ServerConfiguration.GetOrUpdateSetting("crashGuard.restartServer", true);
            GenerateReport = ServerConfiguration.GetOrUpdateSetting("crashGuard.generateReport", true);
        }

        public static void Initialize()
        {
            if (Enabled)
            {
                EventSink.ServerCrashed += CrashGuard_OnCrash;
            }
        }

        public static void CrashGuard_OnCrash(ServerCrashedEventArgs e)
        {
            if (GenerateReport)
            {
                GenerateCrashReport(e);
            }

            World.WaitForWriteCompletion();

            if (SaveBackup)
            {
                Backup();
            }

            if (RestartServer)
            {
                Restart(e);
            }
        }

        private static void SendEmail(string filePath)
        {
            Console.Write("Crash: Sending email...");

            Email.SendCrashEmail(filePath);
        }

        private static void Restart(ServerCrashedEventArgs e)
        {
            Console.Write("Crash: Restarting...");

            try
            {
                Process.Start(Core.Assembly.Location, Core.Arguments);
                Console.WriteLine("done");

                e.Close = true;
            }
            catch
            {
                Console.WriteLine("failed");
            }
        }

        private static void CopyFile(string rootOrigin, string rootBackup, string path)
        {
            var originPath = Path.Combine(rootOrigin, path);
            if (!File.Exists(originPath))
            {
                return;
            }

            var backupPath = Path.Combine(rootBackup, path);
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath));

            try
            {
                File.Copy(originPath, backupPath);
            }
            catch
            {
                // ignored
            }
        }

        private static void Backup()
        {
            Console.Write("Crash: Backing up...");

            try
            {
                var timeStamp = GetTimeStamp();

                var root = Core.BaseDirectory;
                var rootBackup = Path.Combine(root, $"Backups/Crashed/{timeStamp}/");
                var rootOrigin = Path.Combine(root, "Saves/");

                // Copy files
                CopyFile(rootOrigin, rootBackup, "Accounts/Accounts.xml");

                CopyFile(rootOrigin, rootBackup, "Items/Items.bin");
                CopyFile(rootOrigin, rootBackup, "Items/Items.idx");
                CopyFile(rootOrigin, rootBackup, "Items/Items.tdb");

                CopyFile(rootOrigin, rootBackup, "Mobiles/Mobiles.bin");
                CopyFile(rootOrigin, rootBackup, "Mobiles/Mobiles.idx");
                CopyFile(rootOrigin, rootBackup, "Mobiles/Mobiles.tdb");

                CopyFile(rootOrigin, rootBackup, "Guilds/Guilds.bin");
                CopyFile(rootOrigin, rootBackup, "Guilds/Guilds.idx");

                CopyFile(rootOrigin, rootBackup, "Regions/Regions.bin");
                CopyFile(rootOrigin, rootBackup, "Regions/Regions.idx");

                Console.WriteLine("done");
            }
            catch
            {
                Console.WriteLine("failed");
            }
        }

        private static void GenerateCrashReport(ServerCrashedEventArgs e)
        {
            Console.Write("Crash: Generating report...");

            try
            {
                var timeStamp = GetTimeStamp();
                var fileName = $"Crash {timeStamp}.log";

                var root = Core.BaseDirectory;
                var filePath = Path.Combine(root, fileName);

                using (var op = new StreamWriter(filePath))
                {
                    var ver = Core.Version;

                    op.WriteLine("Server Crash Report");
                    op.WriteLine("===================");
                    op.WriteLine();
                    op.WriteLine($"ModernUO Version {ver.Major}.{ver.Minor}, Build {ver.Build}.{ver.Revision}");
                    op.WriteLine("Operating System: {0}", Environment.OSVersion);
                    op.WriteLine(".NET Framework: {0}", Environment.Version);
                    op.WriteLine("Time: {0}", timeStamp);

                    try
                    {
                        op.WriteLine("Mobiles: {0}", World.Mobiles.Count);
                    }
                    catch
                    {
                        // ignored
                    }

                    try
                    {
                        op.WriteLine("Items: {0}", World.Items.Count);
                    }
                    catch
                    {
                        // ignored
                    }

                    op.WriteLine("Exception:");
                    op.WriteLine(e.Exception);
                    op.WriteLine();

                    op.WriteLine("Clients:");

                    try
                    {
                        var states = TcpServer.Instances;

                        op.WriteLine("- Count: {0}", states.Count);

                        for (var i = 0; i < states.Count; ++i)
                        {
                            var state = states[i];

                            op.Write("+ {0}:", state);

                            if (state.Account is Account a)
                            {
                                op.Write(" (account = {0})", a.Username);
                            }

                            var m = state.Mobile;

                            if (m != null)
                            {
                                op.Write(" (mobile = 0x{0:X} '{1}')", m.Serial.Value, m.Name);
                            }

                            op.WriteLine();
                        }
                    }
                    catch
                    {
                        op.WriteLine("- Failed");
                    }
                }

                Console.WriteLine("done");

                SendEmail(filePath);
            }
            catch
            {
                Console.WriteLine("failed");
            }
        }

        private static string GetTimeStamp()
        {
            var now = DateTime.UtcNow;

            return $"{now.Day}-{now.Month}-{now.Year}-{now.Hour}-{now.Minute}-{now.Second}";
        }
    }
}
