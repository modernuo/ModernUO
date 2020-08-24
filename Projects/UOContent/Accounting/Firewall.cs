using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Server
{
    public class Firewall
    {
        static Firewall()
        {
            List = new List<IFirewallEntry>();

            string path = "firewall.cfg";

            if (File.Exists(path))
            {
                using StreamReader ip = new StreamReader(path);
                string line;

                while ((line = ip.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.Length == 0)
                        continue;

                    List.Add(ToFirewallEntry(line));

                    /*
                      object toAdd;
          
                      IPAddress addr;
                      if (IPAddress.TryParse( line, out addr ))
                        toAdd = addr;
                      else
                        toAdd = line;
          
                      m_Blocked.Add( toAdd.ToString() );
                       * */
                }
            }
        }

        public static List<IFirewallEntry> List { get; }

        public static IFirewallEntry ToFirewallEntry(object entry)
        {
            if (entry is IFirewallEntry firewallEntry)
                return firewallEntry;
            if (entry is IPAddress address)
                return new IPFirewallEntry(address);
            if (entry is string s)
                return ToFirewallEntry(s);

            return null;
        }

        public static IFirewallEntry ToFirewallEntry(string entry)
        {
            if (IPAddress.TryParse(entry, out IPAddress addr))
                return new IPFirewallEntry(addr);

            // Try CIDR parse
            string[] str = entry.Split('/');

            if (str.Length == 2)
                if (IPAddress.TryParse(str[0], out IPAddress cidrPrefix))
                    if (int.TryParse(str[1], out int cidrLength))
                        return new CIDRFirewallEntry(cidrPrefix, cidrLength);

            return new WildcardIPFirewallEntry(entry);
        }

        public static void RemoveAt(int index)
        {
            List.RemoveAt(index);
            Save();
        }

        public static void Remove(object obj)
        {
            IFirewallEntry entry = ToFirewallEntry(obj);

            if (entry != null)
            {
                List.Remove(entry);
                Save();
            }
        }

        public static void Add(object obj)
        {
            if (obj is IPAddress address)
                Add(address);
            else if (obj is string s)
                Add(s);
            else if (obj is IFirewallEntry entry)
                Add(entry);
        }

        public static void Add(IFirewallEntry entry)
        {
            if (!List.Contains(entry))
                List.Add(entry);

            Save();
        }

        public static void Add(string pattern)
        {
            IFirewallEntry entry = ToFirewallEntry(pattern);

            if (!List.Contains(entry))
                List.Add(entry);

            Save();
        }

        public static void Add(IPAddress ip)
        {
            IFirewallEntry entry = new IPFirewallEntry(ip);

            if (!List.Contains(entry))
                List.Add(entry);

            Save();
        }

        public static void Save()
        {
            string path = "firewall.cfg";

            using StreamWriter op = new StreamWriter(path);
            for (int i = 0; i < List.Count; ++i)
                op.WriteLine(List[i]);
        }

        public static bool IsBlocked(IPAddress ip)
        {
            for (int i = 0; i < List.Count; i++)
                if (List[i].IsBlocked(ip))
                    return true;

            return false;
            /*
            bool contains = false;
      
            for ( int i = 0; !contains && i < m_Blocked.Count; ++i )
            {
              if (m_Blocked[i] is IPAddress)
                contains = ip.Equals( m_Blocked[i] );
                      else if (m_Blocked[i] is String)
                      {
                          string s = (string)m_Blocked[i];
      
                          contains = Utility.IPMatchCIDR( s, ip );
      
                          if (!contains)
                              contains = Utility.IPMatch( s, ip );
                      }
            }
      
            return contains;
             * */
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
                if (obj is IPAddress)
                    return obj.Equals(m_Address);
                if (obj is string s)
                {
                    if (IPAddress.TryParse(s, out IPAddress otherAddress))
                        return otherAddress.Equals(m_Address);
                }
                else if (obj is IPFirewallEntry entry)
                {
                    return m_Address.Equals(entry.m_Address);
                }

                return false;
            }

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
                    string[] str = entry.Split('/');

                    if (str.Length == 2)
                        if (IPAddress.TryParse(str[0], out IPAddress cidrPrefix))
                            if (int.TryParse(str[1], out int cidrLength))
                                return m_CIDRPrefix.Equals(cidrPrefix) && m_CIDRLength.Equals(cidrLength);
                }
                else if (obj is CIDRFirewallEntry cidrEntry)
                {
                    return m_CIDRPrefix.Equals(cidrEntry.m_CIDRPrefix) && m_CIDRLength.Equals(cidrEntry.m_CIDRLength);
                }

                return false;
            }

            public override int GetHashCode() => m_CIDRPrefix.GetHashCode() ^ m_CIDRLength.GetHashCode();
        }

        public class WildcardIPFirewallEntry : IFirewallEntry
        {
            private readonly string m_Entry;

            private bool m_Valid;

            public WildcardIPFirewallEntry(string entry) => m_Entry = entry;

            public bool IsBlocked(IPAddress address)
            {
                if (!m_Valid)
                    return false; // Why process if it's invalid?  it'll return false anyway after processing it.

                bool matched = Utility.IPMatch(m_Entry, address, out bool valid);
                m_Valid = valid;
                return matched;
            }

            public override string ToString() => m_Entry;

            public override bool Equals(object obj)
            {
                if (obj is string)
                    return obj.Equals(m_Entry);

                return obj is WildcardIPFirewallEntry entry && m_Entry.Equals(entry.m_Entry);
            }

            public override int GetHashCode() => m_Entry.GetHashCode();
        }
    }
}
