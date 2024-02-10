using System;
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
        [InlineData("::ffff:192.168.100.254", "192.168.100.1", 112, true)]
        [InlineData("::ffff:192.168.100.254", "192.168.50.1", 104, true)]
        [InlineData("::ffff:192.168.100.254", "192.168.50.1", 120, false)]
        [InlineData("1234:5678:9ABC:1234:5678:9ABC:1234:5678", "1234:5677:0:0:0:0:0:0", 16, true)]
        [InlineData("1234:5678:9ABC:1234:5678:9ABC:1234:5678", "1234:5677:0:0:0:0:0:0", 64, false)]
        [InlineData("::1234:5678", "1234:5677::", 64, false)]
        [InlineData("::1234:5678", "::9ABC:5677", 112, false)]
        [InlineData("::1234:5678", "::1234:66AA", 112, true)]
        [InlineData("::1234:5678", "::1235:FFFF", 109, true)]
        [InlineData("::1234:5678", "::1238:ABAC", 109, false)]
        public void TestIPvCIDR(string cidr, string addr, int cidrLength, bool shouldMatch)
        {
            var cidrAddress = IPAddress.Parse(cidr);
            var address = IPAddress.Parse(addr);

            Assert.Equal(shouldMatch, cidrAddress.MatchCidr(cidrLength, address));
        }

        [Theory]
        [InlineData("::ffff:192.168.1.1", 0UL, 0xffffc0a80101UL)]
        [InlineData("192.168.1.1", 0UL, 0xffffc0a80101UL)]
        [InlineData("4cce:1490:4577:d72f:693e:b42e:3465:f3db", 0x4cce14904577d72fUL, 0x693eb42e3465f3db)]
        public void TestIPAddressToUInt128(string ipString, ulong upper, ulong lower)
        {
            var ip = IPAddress.Parse(ipString);
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            var actual = ip.ToUInt128();
            Assert.Equal(new UInt128(upper, lower), actual);
            var actual2 = actual.ToIpAddress();
            Assert.Equal(ip, actual2);
        }
    }
}
