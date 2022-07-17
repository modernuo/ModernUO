using System.IO;
using System.Text;
using Server.Accounting;

namespace Server.Commands
{
    public static class CommandLogging
    {
        private static readonly char[] m_NotSafe = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
        public static bool Enabled { get; set; } = true;

        public static StreamWriter Output { get; private set; }

        public static void Initialize()
        {
            EventSink.Command += EventSink_Command;

            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }

            var directory = "Logs/Commands";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                Output = new StreamWriter(Path.Combine(directory, $"{Core.Now.ToLongDateString()}.log"), true);
                Output.AutoFlush = true;

                Output.WriteLine("##############################");
                Output.WriteLine("Log started on {0}", Core.Now);
                Output.WriteLine();
            }
            catch
            {
                // ignored
            }
        }

        public static object Format(object o) =>
            o switch
            {
                Mobile m  => m.Account == null ? $"{m} (no account)" : $"{m} ('{m.Account.Username}')",
                Item item => $"{item.Serial} ({item.GetType().Name})",
                _         => o
            };

        public static void WriteLine(Mobile from, string format, params object[] args)
        {
            if (!Enabled)
            {
                return;
            }

            WriteLine(from, string.Format(format, args));
        }

        public static void WriteLine(Mobile from, string text)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                Output.WriteLine("{0}: {1}: {2}", Core.Now, from.NetState, text);

                var path = Core.BaseDirectory;

                var name = from.Account is not Account acct ? from.Name : acct.Username;

                AppendPath(ref path, "Logs");
                AppendPath(ref path, "Commands");
                AppendPath(ref path, from.AccessLevel.ToString());
                path = Path.Combine(path, $"{name}.log");

                using var sw = new StreamWriter(path, true);
                sw.WriteLine("{0}: {1}: {2}", Core.Now, from.NetState, text);
            }
            catch
            {
                // ignored
            }
        }

        public static void AppendPath(ref string path, string toAppend)
        {
            path = Path.Combine(path, toAppend);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static string Safe(string ip)
        {
            if (ip == null)
            {
                return "null";
            }

            ip = ip.Trim().DefaultIfNullOrEmpty("empty");

            var isSafe = true;

            for (var i = 0; isSafe && i < m_NotSafe.Length; ++i)
            {
                isSafe = !ip.ContainsOrdinal(m_NotSafe[i]);
            }

            if (isSafe)
            {
                return ip;
            }

            var sb = new StringBuilder(ip);

            for (var i = 0; i < m_NotSafe.Length; ++i)
            {
                sb.Replace(m_NotSafe[i], '_');
            }

            return sb.ToString();
        }

        public static void EventSink_Command(CommandEventArgs e)
        {
            WriteLine(
                e.Mobile,
                "{0} {1} used command '{2} {3}'",
                e.Mobile.AccessLevel,
                Format(e.Mobile),
                e.Command,
                e.ArgString
            );
        }

        public static void LogChangeProperty(Mobile from, object o, string name, string value)
        {
            WriteLine(
                from,
                "{0} {1} set property '{2}' of {3} to '{4}'",
                from.AccessLevel,
                Format(from),
                name,
                Format(o),
                value
            );
        }
    }
}
