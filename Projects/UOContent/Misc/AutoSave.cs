using System;
using System.IO;

namespace Server.Misc
{
  public class AutoSave : Timer
  {
    private static readonly TimeSpan m_Delay = TimeSpan.FromMinutes(5.0);
    private static readonly TimeSpan m_Warning = TimeSpan.Zero;

    private static readonly string[] m_Backups =
    {
      "Third Backup",
      "Second Backup",
      "Most Recent"
    };

    public AutoSave() : base(m_Delay - m_Warning, m_Delay) => Priority = TimerPriority.OneMinute;

    public static bool SavesEnabled { get; set; } = true;
    // private static TimeSpan m_Warning = TimeSpan.FromSeconds( 15.0 );

    public static void Initialize()
    {
      new AutoSave().Start();
      CommandSystem.Register("SetSaves", AccessLevel.Administrator, SetSaves_OnCommand);
    }

    [Usage("SetSaves <true | false>")]
    [Description("Enables or disables automatic shard saving.")]
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
        return;

      if (m_Warning == TimeSpan.Zero)
      {
        Save(true);
      }
      else
      {
        int s = (int)m_Warning.TotalSeconds;
        int m = s / 60;
        s %= 60;

        if (m > 0 && s > 0)
          World.Broadcast(0x35, true, "The world will save in {0} minute{1} and {2} second{3}.", m,
            m != 1 ? "s" : "", s, s != 1 ? "s" : "");
        else if (m > 0)
          World.Broadcast(0x35, true, "The world will save in {0} minute{1}.", m, m != 1 ? "s" : "");
        else
          World.Broadcast(0x35, true, "The world will save in {0} second{1}.", s, s != 1 ? "s" : "");

        DelayCall(m_Warning, Save);
      }
    }

    public static void Save()
    {
      Save(false);
    }

    public static void Save(bool permitBackgroundWrite)
    {
      if (AutoRestart.Restarting)
        return;

      World.WaitForWriteCompletion();

      try
      {
        Backup();
      }
      catch (Exception e)
      {
        Console.WriteLine("WARNING: Automatic backup FAILED: {0}", e);
      }

      World.Save(true, permitBackgroundWrite);
    }

    private static void Backup()
    {
      if (m_Backups.Length == 0)
        return;

      string root = Path.Combine(Core.BaseDirectory, "Backups/Automatic");

      if (!Directory.Exists(root))
        Directory.CreateDirectory(root);

      string[] existing = Directory.GetDirectories(root);

      for (int i = 0; i < m_Backups.Length; ++i)
      {
        DirectoryInfo dir = Match(existing, m_Backups[i]);

        if (dir == null)
          continue;

        if (i > 0)
        {
          string timeStamp = FindTimeStamp(dir.Name);

          if (timeStamp != null)
            try
            {
              dir.MoveTo(FormatDirectory(root, m_Backups[i - 1], timeStamp));
            }
            catch
            {
              // ignored
            }
        }
        else
        {
          try
          {
            dir.Delete(true);
          }
          catch
          {
            // ignored
          }
        }
      }

      string saves = Path.Combine(Core.BaseDirectory, "Saves");

      if (Directory.Exists(saves))
        Directory.Move(saves, FormatDirectory(root, m_Backups[^1], GetTimeStamp()));
    }

    private static DirectoryInfo Match(string[] paths, string match)
    {
      for (int i = 0; i < paths.Length; ++i)
      {
        DirectoryInfo info = new DirectoryInfo(paths[i]);

        if (info.Name.StartsWith(match))
          return info;
      }

      return null;
    }

    private static string FormatDirectory(string root, string name, string timeStamp) => Path.Combine(root, $"{name} ({timeStamp})");

    private static string FindTimeStamp(string input)
    {
      int start = input.IndexOf('(');

      if (start >= 0)
      {
        int end = input.IndexOf(')', ++start);

        if (end >= start)
          return input.Substring(start, end - start);
      }

      return null;
    }

    private static string GetTimeStamp()
    {
      DateTime now = DateTime.UtcNow;

      return $"{now.Day}-{now.Month}-{now.Year} {now.Hour}-{now.Minute:D2}-{now.Second:D2}";
    }
  }
}