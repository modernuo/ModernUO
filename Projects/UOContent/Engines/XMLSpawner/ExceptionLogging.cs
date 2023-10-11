using System;
using System.IO;

namespace Server.Diagnostics;

// TODO: Replace this with serilog
public class ExceptionLogging
{
    public static string LogDirectory { get; set; }

    private static StreamWriter _Output;

    public static StreamWriter Output
    {
        get
        {
            if (_Output == null)
            {
                _Output = new StreamWriter(Path.Combine(LogDirectory, $"{Core.Now.ToLongDateString()}.log"), true)
                {
                    AutoFlush = true
                };

                _Output.WriteLine("##############################");
                _Output.WriteLine("Exception log started on {0}", Core.Now);
                _Output.WriteLine();
            }

            return _Output;
        }
    }

    static ExceptionLogging()
    {
        var directory = Path.Combine(Core.BaseDirectory, "Logs/Exceptions");

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        LogDirectory = directory;
    }

    public static void LogException(Exception e)
    {
        Utility.PushColor(ConsoleColor.Red);
        Console.WriteLine("Caught Exception:");
        Utility.PopColor();

        Utility.PushColor(ConsoleColor.DarkRed);
        Console.WriteLine(e);
        Utility.PopColor();

        Output.WriteLine("Exception Caught: {0}", Core.Now);
        Output.WriteLine(e);
        Output.WriteLine();
    }

    public static void LogException(Exception e, string arg)
    {
        Utility.PushColor(ConsoleColor.Red);
        Console.WriteLine("Caught Exception: {0}", arg);
        Utility.PopColor();

        Utility.PushColor(ConsoleColor.DarkRed);
        Console.WriteLine(e);
        Utility.PopColor();

        Output.WriteLine("Exception Caught: {0}", Core.Now);
        Output.WriteLine(e);
        Output.WriteLine();
    }
}
