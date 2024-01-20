using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using Server.Logging;
using Server.Network;

namespace Server;

public static class AdminFirewall
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AdminFirewall));

    private static readonly HashSet<IFirewallEntry> _firewallSet = [];
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

    // Note: This is not optimized, so do not use this in hot paths
    public static IReadOnlySet<IFirewallEntry> Set => _firewallSet;

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

    public static void Remove(object obj, bool save = true)
    {
        var entry = ToFirewallEntry(obj);

        if (entry != null)
        {
            _firewallSet.Remove(entry);
            Firewall.RequestRemoveEntry(entry); // Request that the TcpServer also remove the entry

            if (save)
            {
                Save();
            }
        }
    }

    public static bool Add(object obj) => Add(ToFirewallEntry(obj));

    public static bool Add(IFirewallEntry entry, bool save = true)
    {
        var added = _firewallSet.Add(entry);
        Firewall.RequestAddEntry(entry); // Request that the TcpServer also add the entry

        if (save)
        {
            Save();
        }

        return added;
    }

    public static void Save()
    {
        using var op = new StreamWriter(firewallConfigPath);
        foreach (var entry in Set)
        {
            op.WriteLine(entry);
        }
    }
}
