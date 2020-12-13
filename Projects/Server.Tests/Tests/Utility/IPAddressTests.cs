using System.Net;
using Xunit;

namespace Server.Tests
{
    public class IPAddressTests
    {
        [Theory]
        [InlineData("192.168.100.254", "192.168.100.1", 16, true)]
        [InlineData("192.168.100.254", "192.168.50.1", 24, false)]
        [InlineData("192.168.100.1", "192.168.50.1", 32, false)]
        [InlineData("192.168.50.1", "192.168.50.1", 32, true)]
        [InlineData("192.168.50.4", "192.168.50.7", 30, true)]
        [InlineData("192.168.50.4", "192.168.50.9", 30, false)]
        public void TestIPv4CIDR(string cidr, string addr, int cidrLength, bool shouldMatch)
        {
            var cidrAddress = IPAddress.Parse(cidr);
            var address = IPAddress.Parse(addr);

            Assert.Equal(shouldMatch, Utility.IPMatchCIDR(cidrAddress, address, cidrLength));
        }

        [Theory]
        [InlineData("::ffff:192.168.100.254", "192.168.100.1", 112, true)]
        [InlineData("::ffff:192.168.100.254", "192.168.50.1", 104, false)]
        public void TestIPv4MixedCIDR(string cidr, string addr, int cidrLength, bool shouldMatch)
        {
            var cidrAddress = IPAddress.Parse(cidr);
            var address = IPAddress.Parse(addr);

            Assert.Equal(shouldMatch, Utility.IPMatchCIDR(cidrAddress, address, cidrLength));
        }

        [Theory]
        [InlineData("1234:5678:9ABC:1234:5678:9ABC:1234:5678", "1234:5677:0:0:0:0:0:0", 16, true)]
        [InlineData("1234:5678:9ABC:1234:5678:9ABC:1234:5678", "1234:5677:0:0:0:0:0:0", 64, false)]
        [InlineData("::1234:5678", "1234:5677::", 64, false)]
        [InlineData("::1234:5678", "::9ABC:5677", 112, false)]
        [InlineData("::1234:5678", "::1234:66AA", 112, true)]
        [InlineData("::1234:5678", "::1235:FFFF", 109, true)]
        [InlineData("::1234:5678", "::1238:ABAC", 109, false)]
        public void TestIPv6CIDR(string cidr, string addr, int cidrLength, bool shouldMatch)
        {
            var cidrAddress = IPAddress.Parse(cidr);
            var address = IPAddress.Parse(addr);

            Assert.Equal(shouldMatch, Utility.IPMatchCIDR(cidrAddress, address, cidrLength));
        }
    }
}
