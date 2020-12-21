using System;
using System.Net;
using Xunit;

namespace Server.Tests
{
    public class FirewallEntryTests
    {
        [Theory]
        [InlineData("192.168.1.1/24", typeof(Firewall.CIDRFirewallEntry), "192.168.1.50")]
        [InlineData("192.168.*.100-200", typeof(Firewall.WildcardIPFirewallEntry), "192.168.30.150")]
        [InlineData("::1234:*:1000-1234", typeof(Firewall.WildcardIPFirewallEntry), "::1234:5678:1150")]
        public void TestFirewallEntry(string entry, Type entryType, string ipToMatch)
        {
            var firewallEntry = Firewall.ToFirewallEntry(entry);
            Assert.IsType(entryType, firewallEntry);
            Assert.True(firewallEntry.IsBlocked(IPAddress.Parse(ipToMatch)));
        }
    }
}
