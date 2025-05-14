using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using Server.Logging;
using Server.Network;

namespace Server;

public static class AdminFirewall
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AdminFirewall));

    private const string firewallConfigPath = "firewall.cfg";

    public static void Configure()
    {
        if (File.Exists(firewallConfigPath))
        {
            var searchValues = SearchValues.Create("*Xx?");

            using var ip = new StreamReader(firewallConfigPath);

            while (ip.ReadLine() is { } line)
            {
                line = line.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                if (line.AsSpan().ContainsAny(searchValues))
                {
                    logger.Warning("Legacy firewall entry \"{Entry}\" ignored", line);
                    continue;
                }

                Add(ToFirewallEntry(line), false);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IFirewallEntry ToFirewallEntry(object entry)
    {
        return entry switch
        {
            IFirewallEntry firewallEntry => firewallEntry,
            IPAddress address            => new SingleIpFirewallEntry(address),
            string s                     => ToFirewallEntry(s),
            _                            => null
        };
    }

    public static IFirewallEntry ToFirewallEntry(string entry)
    {
        if (entry == null)
        {
            return null;
        }

        try
        {
            var rangeSeparator = entry.IndexOf('-');
            if (rangeSeparator > -1)
            {
                return new CidrFirewallEntry(
                    IPAddress.Parse(entry.AsSpan(0, rangeSeparator)),
                    IPAddress.Parse(entry.AsSpan(rangeSeparator + 1))
                );
            }

            // CIDR notation
            if (entry.IndexOf('/') > -1)
            {
                return new CidrFirewallEntry(entry);
            }

            return new SingleIpFirewallEntry(entry);
        }
        catch
        {
            return null;
        }
    }

    public static bool Remove(object obj, bool save = true)
    {
        var entry = ToFirewallEntry(obj);

        if (entry == null)
        {
            return false;
        }

        if (!Firewall.Remove(entry))
        {
            return false;
        }

        if (save)
        {
            Save();
        }

        return true;
    }

    public static void Add(object obj) => Add(ToFirewallEntry(obj));

    public static bool Add(IFirewallEntry entry, bool save = true)
    {
        if (!Firewall.Add(entry))
        {
            return false;
        }

        if (save)
        {
            Save();
        }

        return true;
    }

    public static void Save()
    {
        Firewall.ReadFirewallSet(firewallSet =>
        {
            using var op = new StreamWriter(firewallConfigPath);
            foreach (var entry in firewallSet)
            {
                op.WriteLine(entry);
            }
        });
    }
}
