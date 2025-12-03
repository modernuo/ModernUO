using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Server;

public static class ServerConfigurationPrompts
{
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

    internal static string GetServerName()
    {
        Console.WriteLine("Please enter the name of your shard:");

        string serverName;
        do
        {
            Console.Write("[ModernUO]> ");
            serverName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(serverName))
            {
                serverName = "ModernUO";
            }

            break;
        } while (true);

        Console.Write("Server name set to ");
        Utility.PushColor(ConsoleColor.Green);
        Console.Write(serverName);
        Utility.PopColor();
        Console.WriteLine(".");

        return serverName;
    }
}
