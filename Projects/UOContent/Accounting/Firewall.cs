using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;

namespace Server
{
    public static class Firewall
    {
        private const string firewallConfigPath = "firewall.cfg";

        static Firewall()
        {
            Set = new HashSet<IFirewallEntry>();

            if (File.Exists(firewallConfigPath))
            {
                using var ip = new StreamReader(firewallConfigPath);
                string line;

                while ((line = ip.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.Length == 0)
                    {
                        continue;
                    }

                    Set.Add(ToFirewallEntry(line));
                }
            }
        }

        public static HashSet<IFirewallEntry> Set { get; }

        public static IFirewallEntry ToFirewallEntry(object entry)
        {
            return entry switch
            {
                IFirewallEntry firewallEntry => firewallEntry,
                IPAddress address            => new IPFirewallEntry(address),
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

            if (IPAddress.TryParse(entry, out var addr))
            {
                return new IPFirewallEntry(addr);
            }

            // Try CIDR parse
            var tokenizer = entry.Tokenize('/');
            var ip = tokenizer.MoveNext() ? tokenizer.Current : null;
            var length = tokenizer.MoveNext() ? tokenizer.Current : null;

            if (
                length != null &&
                IPAddress.TryParse(ip, out var cidrPrefix) &&
                int.TryParse(length, out var cidrLength)
            )
            {
                return new CIDRFirewallEntry(cidrPrefix, cidrLength);
            }

            return new WildcardIPFirewallEntry(entry);
        }

        public static void Remove(object obj)
        {
            var entry = ToFirewallEntry(obj);

            if (entry != null)
            {
                Set.Remove(entry);
                Save();
            }
        }

        public static void Add(object obj)
        {
            Add(ToFirewallEntry(obj));
        }

        public static void Add(IFirewallEntry entry)
        {
            Set.Add(entry);
            Save();
        }

        public static void Add(string pattern)
        {
            Add(ToFirewallEntry(pattern));
        }

        public static void Add(IPAddress ip)
        {
            Add(ToFirewallEntry(ip));
        }

        public static void Save()
        {
            using var op = new StreamWriter(firewallConfigPath);
            foreach (var entry in Set)
            {
                op.WriteLine(entry);
            }
        }

        public static bool IsBlocked(IPAddress ip)
        {
            foreach (var entry in Set)
            {
                if (entry.IsBlocked(ip))
                {
                    return true;
                }
            }

            return false;
        }

        public interface IFirewallEntry
        {
            bool IsBlocked(IPAddress address);
        }

        public class IPFirewallEntry : IFirewallEntry
        {
            private readonly IPAddress m_Address;

            public IPFirewallEntry(IPAddress address) => m_Address = address;

            public bool IsBlocked(IPAddress address) => m_Address.Equals(address);

            public override string ToString() => m_Address.ToString();

            public override bool Equals(object obj)
            {
                return obj switch
                {
                    IPAddress                                                 => obj.Equals(m_Address),
                    string s when IPAddress.TryParse(s, out var otherAddress) => otherAddress.Equals(m_Address),
                    IPFirewallEntry entry                                     => m_Address.Equals(entry.m_Address),
                    _                                                         => false
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() => m_Address.GetHashCode();
        }

        public class CIDRFirewallEntry : IFirewallEntry
        {
            private readonly int m_CIDRLength;
            private readonly IPAddress m_CIDRPrefix;

            public CIDRFirewallEntry(IPAddress cidrPrefix, int cidrLength)
            {
                m_CIDRPrefix = cidrPrefix;
                m_CIDRLength = cidrLength;
            }

            public bool IsBlocked(IPAddress address) => Utility.IPMatchCIDR(m_CIDRPrefix, address, m_CIDRLength);

            public override string ToString() => $"{m_CIDRPrefix}/{m_CIDRLength}";

            public override bool Equals(object obj)
            {
                if (obj is string entry)
                {
                    var str = entry.Split('/');

                    if (str.Length == 2)
                    {
                        if (IPAddress.TryParse(str[0], out var cidrPrefix))
                        {
                            if (int.TryParse(str[1], out var cidrLength))
                            {
                                return m_CIDRPrefix.Equals(cidrPrefix) && m_CIDRLength.Equals(cidrLength);
                            }
                        }
                    }
                }
                else if (obj is CIDRFirewallEntry cidrEntry)
                {
                    return m_CIDRPrefix.Equals(cidrEntry.m_CIDRPrefix) && m_CIDRLength.Equals(cidrEntry.m_CIDRLength);
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() => HashCode.Combine(m_CIDRPrefix, m_CIDRLength);
        }

        public class WildcardIPFirewallEntry : IFirewallEntry
        {
            private readonly string m_Entry;

            private bool m_Valid = true;

            public WildcardIPFirewallEntry(string entry) => m_Entry = entry;

            public bool IsBlocked(IPAddress address) => m_Valid && Utility.IPMatch(m_Entry, address, out m_Valid);

            public override string ToString() => m_Entry;

            public override bool Equals(object obj)
            {
                if (obj is string)
                {
                    return obj.Equals(m_Entry);
                }

                return obj is WildcardIPFirewallEntry entry && m_Entry == entry.m_Entry;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() => m_Entry.GetHashCode(StringComparison.Ordinal);
        }
    }
}
