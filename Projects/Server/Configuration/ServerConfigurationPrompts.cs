using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;

namespace Server;

public static class ServerConfigurationPrompts
{
    internal static bool GetIsClient7090()
    {
        if (UOClient.ServerClientVersion != null)
        {
            return UOClient.ServerClientVersion >= ClientVersion.Version7090;
        }

        Console.WriteLine("Will you be using a client version 7.0.9.0 or newer?");

        do
        {
            Console.Write("[y] or n> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.InsensitiveStartsWith("y"))
            {
                Utility.PushColor(ConsoleColor.Yellow);
                Console.WriteLine("Client >= 7.0.9.0 chosen.");
                Utility.PopColor();
                return true;
            }

            if (input.InsensitiveStartsWith("n"))
            {
                Utility.PushColor(ConsoleColor.Yellow);
                Console.WriteLine("Client < 7.0.9.0 chosen.");
                Utility.PopColor();
                return false;
            }

            Console.Write("Invalid option ");
            Utility.PushColor(ConsoleColor.Red);
            Console.Write(input);
            Utility.PopColor();
            Console.WriteLine(". Press y for yes or n for no.");
        } while (true);
    }


    internal static bool GetIsClientPre6000()
    {
        if (UOClient.ServerClientVersion != null)
        {
            return UOClient.ServerClientVersion < ClientVersion.Version6000;
        }

        Console.WriteLine("Will you be using a client version older than 6.0.0.0?");

        do
        {
            Console.Write("y or [n]> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.InsensitiveStartsWith("n"))
            {
                Utility.PushColor(ConsoleColor.Yellow);
                Console.WriteLine("Client >= 6.0.0.0 chosen.");
                Utility.PopColor();
                return false;
            }

            if (input.InsensitiveStartsWith("y"))
            {
                Utility.PushColor(ConsoleColor.Yellow);
                Console.WriteLine("Client < 6.0.0.0 chosen.");
                Utility.PopColor();
                return true;
            }

            Console.Write("Invalid option ");
            Utility.PushColor(ConsoleColor.Red);
            Console.Write(input);
            Utility.PopColor();
            Console.WriteLine(". Press y for yes or n for no.");
        } while (true);
    }

    internal static Expansion GetExpansion()
    {
        Console.WriteLine("Please choose an expansion by typing the number or short name:");
        var expansions = ExpansionInfo.Table;

        for (int i = 0; i < expansions.Length; i++)
        {
            var info = expansions[i];
            Console.WriteLine(" - {0,2}: {1} ({2})", i, ((Expansion)info.ID).ToString(), info.Name);
        }

        var maxExpansion = (Expansion)expansions[^1].ID;
        var maxExpansionName = maxExpansion.ToString();

        do
        {
            Console.Write("[enter for {0}]> ", maxExpansionName);
            var input = Console.ReadLine();
            Expansion expansion;

            if (string.IsNullOrWhiteSpace(input))
            {
                expansion = maxExpansion;
            }
            else if (int.TryParse(input, NumberStyles.Integer, null, out var number) &&
                     number >= 0 && number < expansions.Length)
            {
                expansion = (Expansion)number;
            }
            else if (!Enum.TryParse(input, out expansion))
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.Write(input);
                Utility.PopColor();
                Console.WriteLine(" is an invalid expansion option.");
                continue;
            }

            Console.Write("Expansion set to ");
            Utility.PushColor(ConsoleColor.Green);
            Console.Write(ExpansionInfo.GetInfo(expansion).Name);
            Utility.PopColor();
            Console.WriteLine(".");
            return expansion;
        } while (true);
    }

    internal static List<string> GetDataDirectories()
    {
        Console.WriteLine("Please enter the absolute path to your ClassicUO or Ultima Online data:");

        var directories = new List<string>();

        do
        {
            Console.Write("{0}> ", directories.Count > 0 ? "[enter to finish]" : " ");
            var directory = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(directory))
            {
                break;
            }

            if (Directory.Exists(directory))
            {
                directories.Add(directory);
                Console.Write("Added ");
                Utility.PushColor(ConsoleColor.Green);
                Console.Write(directory);
                Utility.PopColor();
                Console.WriteLine(".");
            }
            else
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.Write(directory);
                Utility.PopColor();
                Console.WriteLine(" does not exist.");
            }
        } while (true);

        return directories;
    }

    internal static List<IPEndPoint> GetListeners()
    {
        Console.WriteLine("Please enter the IP and ports to listen:");
        Console.WriteLine(" - Only enter IP addresses directly bound to this machine");
        Console.WriteLine(" - To listen to all IP addresses enter 0.0.0.0");

        var ips = new List<IPEndPoint>();

        do
        {
            // IP:Port?
            Console.Write("[{0}]> ", ips.Count > 0 ? "enter to finish" : "0.0.0.0:2593");
            var ipStr = Console.ReadLine();

            IPEndPoint ip;
            if (string.IsNullOrWhiteSpace(ipStr))
            {
                if (ips.Count > 0)
                {
                    break;
                }

                ip = new IPEndPoint(IPAddress.Any, 2593);
            }
            else
            {
                if (!ipStr.ContainsOrdinal(':'))
                {
                    ipStr += ":2593";
                }

                if (!IPEndPoint.TryParse(ipStr, out ip))
                {
                    Utility.PushColor(ConsoleColor.Red);
                    Console.Write(ipStr);
                    Utility.PopColor();
                    Console.WriteLine(" is not a valid IP or port.");
                    continue;
                }
            }

            ips.Add(ip);
            Console.Write("Added ");
            Utility.PushColor(ConsoleColor.Green);
            Console.Write(ip);
            Utility.PopColor();
            Console.WriteLine(".");
        } while (true);

        return ips;
    }
}
