using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Server;
using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;
using WindowsFirewallHelper.FirewallRules;

public class WindowsFirewallManager : IFirewallManager
{
    private const string RuleNamePrefix = "ModernUO Blocklist";
    private const int MaxAddressesPerRule = 1000; // Windows supports ~1000 addresses per rule

    public void AddIPAddress(IPAddress ip, out ISet<IPAddress> newSet)
    {
        var firewallRules = GetFirewallRules();

        newSet = new HashSet<IPAddress>();
        // Just in case the rules were modified outside of this class, let's build a new set

        // Find a rule with space
        var added = false;
        foreach (var rule in firewallRules)
        {
            for (var i = 0; i < rule.RemoteAddresses.Length; i++)
            {
                if (IPAddress.TryParse(rule.RemoteAddresses[i].ToString(), out var address))
                {
                    newSet.Add(address);
                }
            }

            if (!added && rule.RemoteAddresses.Length < MaxAddressesPerRule)
            {
                rule.RemoteAddresses = [..rule.RemoteAddresses, NetworkAddress.Parse(ip.ToString())];
                newSet.Add(Utility.Intern(ip));
                added = true;
            }
        }

        if (added)
        {
            return;
        }

        // If no existing rule has space, create a new one
        string newRuleName = $"{RuleNamePrefix} {firewallRules.Count + 1}";
        var newRule = FirewallWAS.Instance.CreatePortRule(
            FirewallProfiles.Public,
            newRuleName,
            FirewallAction.Block,
            FirewallDirection.Inbound,
            (ushort)ServerConfiguration.Listeners[0].Port,
            FirewallProtocol.TCP
        );
        newRule.IsEnable = true;
        newRule.RemoteAddresses = [NetworkAddress.Parse(ip.ToString())];
        newSet.Add(Utility.Intern(ip));

        FirewallWAS.Instance.Rules.Add(newRule);
    }

    public void RemoveIPAddress(IPAddress ip, out ISet<IPAddress> newSet)
    {
        var firewallRules = GetFirewallRules();

        newSet = new HashSet<IPAddress>();
        // Just in case the rules were modified outside of this class, let's build a new set

        // Find a rule with space
        foreach (var rule in firewallRules)
        {
            var foundIndex = -1;
            for (var i = 0; i < rule.RemoteAddresses.Length; i++)
            {
                if (IPAddress.TryParse(rule.RemoteAddresses[i].ToString(), out var address))
                {
                    address = Utility.Intern(address);
                    if (foundIndex == -1 && address.Equals(ip)) // Exclude it
                    {
                        foundIndex = i;
                    }
                    else
                    {
                        newSet.Add(address);
                    }
                }
            }

            if (foundIndex != -1)
            {
                var rules = rule.RemoteAddresses.AsSpan();
                rule.RemoteAddresses = [..rules[..foundIndex], ..rules[(foundIndex + 1)..]];
            }
        }
    }

    public ISet<IPAddress> GetBlockedIPs()
    {
        Console.WriteLine("GetBlockedIPs");
        HashSet<IPAddress> blacklistedIps = [];

        foreach (var rule in GetFirewallRules())
        {
            Console.WriteLine("Rule: {0}", rule.Name);
            foreach (var address in rule.RemoteAddresses)
            {
                Console.WriteLine("IP: {0}", address.ToString());
                if (IPAddress.TryParse(address.ToString(), out var ip))
                {
                    blacklistedIps.Add(Utility.Intern(ip));
                }
            }
        }

        return blacklistedIps;
    }

    private static List<FirewallWASRule> GetFirewallRules()
    {
        return FirewallWAS.Instance.Rules
            .Where(rule => rule.Name.InsensitiveStartsWith(RuleNamePrefix))
            .ToList();
    }
}
