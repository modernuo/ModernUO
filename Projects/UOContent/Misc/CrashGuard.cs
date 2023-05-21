using System;
using System.Diagnostics;
using System.IO;
using Server.Accounting;
using Server.Logging;
using Server.Network;
using Server.Saves;

namespace Server.Misc
{
    public static class CrashGuard
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(CrashGuard));

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
            logger.Information("Sending crash email");

            Email.SendCrashEmail(filePath);
        }

        private static void Restart(ServerCrashedEventArgs e)
        {
            logger.Information("Restarting");

            try
            {
                Process.Start(Core.Assembly.Location, Core.Arguments);
                logger.Information("Restart done");

                e.Close = true;
            }
            catch
            {
                logger.Error("Restart failed");
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath);
            }
        }

        private static void Backup()
        {
            logger.Information("Backing up");

            try
            {
                var timeStamp = Utility.GetTimeStamp();

                var backupPath = PathUtility.EnsureDirectory(Path.Combine(AutoArchive.BackupPath, "Crashed", timeStamp));
                var savePath = World.SavePath;
                if (Directory.Exists(savePath))
                {
                    DirectoryCopy(savePath, backupPath);
                }

                logger.Information("Backup {Status}", "done");
            }
            catch
            {
                logger.Error("Backup {Status}", "failed");
            }
        }

        private static void GenerateCrashReport(ServerCrashedEventArgs e)
        {
            logger.Information("Generating crash report");

            try
            {
                var timeStamp = Utility.GetTimeStamp();
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
                        op.WriteLine($"Accounts: {Accounts.Count}");
                    }
                    catch
                    {
                        // ignored
                    }

                    try
                    {
                        op.WriteLine($"Mobiles: {World.Mobiles.Count}");
                    }
                    catch
                    {
                        // ignored
                    }

                    try
                    {
                        op.WriteLine($"Items: {World.Items.Count}");
                    }
                    catch
                    {
                        // ignored
                    }

                    op.WriteLine($"Exception: {e.Exception}");
                    op.WriteLine();

                    op.WriteLine("Clients:");

                    try
                    {
                        var states = TcpServer.Instances;

                        op.WriteLine($"- Count: {states.Count}");

                        foreach (var ns in TcpServer.Instances)
                        {
                            op.Write($"+ {ns}:");

                            if (ns.Account is Account a)
                            {
                                op.Write($" (account = {a.Username})");
                            }

                            var m = ns.Mobile;

                            if (m != null)
                            {
                                op.Write($" (mobile = {m.Serial} '{m.Name}')");
                            }

                            op.WriteLine();
                        }
                    }
                    catch
                    {
                        op.WriteLine("- Failed");
                    }
                }

                logger.Information("Crash report generated");

                SendEmail(filePath);
            }
            catch
            {
                logger.Error("Crash report generation failed");
            }
        }
    }
}
