using System;
using System.IO;
using Server.Accounting;
using Server.Commands;
using Server.Network;

namespace Server.RemoteAdmin
{
  public class RemoteAdminLogging
  {
    private const string LogBaseDirectory = "Logs";
    private const string LogSubDirectory = "RemoteAdmin";

    private static bool Initialized;

    public static bool Enabled{ get; set; } = true;

    public static StreamWriter Output{ get; private set; }

    public static void LazyInitialize()
    {
      if (Initialized || !Enabled) return;
      Initialized = true;

      if (!Directory.Exists(LogBaseDirectory))
        Directory.CreateDirectory(LogBaseDirectory);

      string directory = Path.Combine(LogBaseDirectory, LogSubDirectory);

      if (!Directory.Exists(directory))
        Directory.CreateDirectory(directory);

      try
      {
        Output = new StreamWriter(
          Path.Combine(directory,
            string.Format(LogSubDirectory + "{0}.log", DateTime.UtcNow.ToString("yyyyMMdd"))), true) { AutoFlush = true };


        Output.WriteLine("##############################");
        Output.WriteLine("Log started on {0}", DateTime.UtcNow);
        Output.WriteLine();
      }
      catch
      {
        Utility.PushColor(ConsoleColor.Red);
        Console.WriteLine("RemoteAdminLogging: Failed to initialize LogWriter.");
        Utility.PopColor();
        Enabled = false;
      }
    }

    public static object Format(object o)
    {
      o = CommandLogging.Format(o);
      if (o == null)
        return "(null)";

      return o;
    }

    public static void WriteLine(NetState state, string format, params object[] args)
    {
      for (int i = 0; i < args.Length; i++)
        args[i] = CommandLogging.Format(args[i]);

      WriteLine(state, string.Format(format, args));
    }

    public static void WriteLine(NetState state, string text)
    {
      LazyInitialize();

      if (!Enabled)
        return;

      try
      {
        Account acct = state?.Account as Account;
        string name = acct == null ? "(UNKNOWN)" : acct.Username;
        string accesslevel = acct == null ? "NoAccount" : acct.AccessLevel.ToString();
        string statestr = state == null ? "NULLSTATE" : state.ToString();

        Output.WriteLine("{0}: {1}: {2}: {3}", DateTime.UtcNow, statestr, name, text);

        string path = Core.BaseDirectory;

        CommandLogging.AppendPath(ref path, LogBaseDirectory);
        CommandLogging.AppendPath(ref path, LogSubDirectory);
        CommandLogging.AppendPath(ref path, accesslevel);
        path = Path.Combine(path, $"{name}.log");

        using (StreamWriter sw = new StreamWriter(path, true))
        {
          sw.WriteLine("{0}: {1}: {2}", DateTime.UtcNow, statestr, text);
        }
      }
      catch
      {
        // ignored
      }
    }
  }
}