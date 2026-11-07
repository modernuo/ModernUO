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
        [InlineData("2001:db8:1:2::", "2001:db8:1:2:ffff::1", 64, true)]
        [InlineData("2001:db8:1:2::", "2001:db8:1:3::", 64, false)]
        [InlineData("2001:db8:1::", "2001:db8:1:ff::1", 48, true)]
        [InlineData("2001:db8:1::", "2001:db8:2::", 48, false)]
        [InlineData("2001:db8:1::", "2001:db9::", 48, false)]
        [InlineData("fc00::", "fdff:ffff::1", 7, true)]
        [InlineData("fc00::", "fe00::1", 7, false)]
        [InlineData("::", "2001:db8::1", 0, true)]
        [InlineData("::", "::", 0, true)]
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

        [Theory]
        [InlineData("::1", true)]
        [InlineData("fd12::1", true)]
        [InlineData("fdff:ffff:ffff:ffff::1", true)]
        [InlineData("fe80::1", true)]
        [InlineData("2001:db8::1", false)]
        public void IPv6PrivateNetworks(string address, bool expected) =>
            Assert.Equal(expected, IPAddress.Parse(address).IsPrivateNetwork());

        [Theory]
        [InlineData("::ffff:192.168.1.10", "192.168.1.200", true)]
        [InlineData("::ffff:192.168.1.10", "192.168.2.1", false)]
        [InlineData("::ffff:192.168.1.10", "10.0.0.1", false)]
        [InlineData("192.168.1.10", "192.168.1.200", true)]
        [InlineData("192.168.1.10", "192.168.2.1", false)]
        public void MatchClassC_IPv4AndMapped(string first, string second, bool expected) =>
            Assert.Equal(expected, IPAddress.Parse(first).MatchClassC(IPAddress.Parse(second)));

        [Fact]
        public void ParseCidrRange_SlashZero_CoversEverything()
        {
            Assert.True(IPAddressUtility.TryParseCidrRange("::/0", out var min, out var max));
            Assert.Equal(UInt128.MinValue, min);
            Assert.Equal(UInt128.MaxValue, max);
        }

        [Theory]
        [InlineData("2001:db8:1:2::1", "2001:db8:1:2:ffff::9", true)]
        [InlineData("2001:db8:1:2::1", "2001:db8:1:3::1", false)]
        public void SubscriberKey_IPv6_SameSlash64_Equal(string a, string b, bool expected)
        {
            Assert.Equal(expected, IPAddress.Parse(a).ToSubscriberKey() == IPAddress.Parse(b).ToSubscriberKey());
        }

        [Fact]
        public void SubscriberKey_RespectsPrefix()
        {
            IPAddressUtility.SetSubscriberPrefix(56);
            try
            {
                Assert.Equal(
                    IPAddress.Parse("2001:db8:1:200::1").ToSubscriberKey(),
                    IPAddress.Parse("2001:db8:1:2ff::1").ToSubscriberKey()
                );
                Assert.NotEqual(
                    IPAddress.Parse("2001:db8:1:200::1").ToSubscriberKey(),
                    IPAddress.Parse("2001:db8:1:300::1").ToSubscriberKey()
                );
            }
            finally
            {
                IPAddressUtility.SetSubscriberPrefix(64);
            }
        }

        [Fact]
        public void MappedIPv4_IsExact()
        {
            var v4 = IPAddress.Parse("1.2.3.4").ToSubscriberKey();
            Assert.Equal(v4, IPAddress.Parse("::ffff:1.2.3.4").ToSubscriberKey());
            Assert.NotEqual(v4, IPAddress.Parse("::ffff:1.2.3.5").ToSubscriberKey());
        }

        [Theory]
        [InlineData("2001:db8:1:2::1", "2001:db8:1:2::/64")]
        [InlineData("1.2.3.4", "1.2.3.4/32")]
        [InlineData("::ffff:1.2.3.4", "1.2.3.4/32")]
        public void SubscriberNetwork_Formats(string ip, string expected)
        {
            Assert.Equal(expected, IPAddress.Parse(ip).ToSubscriberNetwork().ToString());
        }

        [Theory]
        [InlineData(0, 48)]
        [InlineData(32, 48)]
        [InlineData(47, 48)]
        [InlineData(48, 48)]
        [InlineData(56, 56)]
        [InlineData(64, 64)]
        [InlineData(65, 64)]
        [InlineData(128, 64)]
        [InlineData(-1, 48)]
        public void ClampPrefix_OutOfRange(int configured, int expected)
        {
            Assert.Equal(expected, IPAddressUtility.ClampSubscriberPrefix(configured));
        }

        [Theory]
        [InlineData("1.2.3.4", "1.2.3.200", true)]
        [InlineData("1.2.3.4", "1.2.4.1", false)]
        public void MatchClassC_IPv4Unchanged(string a, string b, bool expected)
        {
            Assert.Equal(expected, IPAddress.Parse(a).MatchClassC(IPAddress.Parse(b)));
        }

        [Theory]
        [InlineData("2001:db8:1:2::1", "2001:db8:1:2::99", true)]
        [InlineData("2001:db8:1:2::1", "2001:db8:1:3::1", false)]
        [InlineData("2001:db8:1:2::1", "2001:db8:ffff::1", false)]
        public void MatchClassC_IPv6UsesSubscriber(string a, string b, bool expected)
        {
            Assert.Equal(expected, IPAddress.Parse(a).MatchClassC(IPAddress.Parse(b)));
        }
    }
}
