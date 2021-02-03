using System;
using System.Buffers.Binary;
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

            Assert.Equal(shouldMatch, Utility.IPMatchCIDR(cidrAddress, address, cidrLength));
        }

        [Theory]
        [InlineData("192.168.1.*", "192.168.1.1", true, true)]
        [InlineData("192.168.1.100", "192.168.1.1", false, true)]
        [InlineData("192.168.*.100", "192.168.1.100", true, true)]
        [InlineData("192.168.20-60.100", "192.168.37.100", true, true)]
        [InlineData("192.168.20-60.100", "192.168.85.100", false, true)]
        [InlineData("192.168.-.100", "192.168.85.100", false, false)]
        [InlineData("192.168.x-.100", "192.168.85.100", false, false)]
        [InlineData("192.168.x*.100", "192.168.85.100", false, false)]
        [InlineData("192.168.**.100", "192.168.85.100", false, false)]
        [InlineData("::1234:*", "::1234:5678", true, true)]
        [InlineData("::1234-1238:1000", "::1236:1000", true, true)]
        [InlineData("::1234-1238:1000", "::1239:1000", false, true)]
        [InlineData("::10:*:1234-1238:1000", "::10:55A1:1235:1000", true, true)]
        [InlineData("1024:*:1234::", "1024:8A13:1234::", true, true)]
        [InlineData("::1024:*:1234::", "1024:8A13:1234::", false, false)]
        [InlineData("::1024:*:1234:-", "::1024:8A13:1234", false, false)]
        [InlineData("::1024:*1:1234", "::1024:8A13:1234", false, false)]
        [InlineData("::1024:*-:1234", "::1024:8A13:1234", false, false)]
        [InlineData("::1024:?1:1234", "::1024:8A13:1234", false, false)]
        [InlineData("::1024:1_2:1234", "::1024:8A13:1234", false, false)]
        [InlineData("172.16-31.*", "172.16.17.2", true, true)]
        public void TestIPMatch(string val, string addr, bool shouldMatch, bool shouldBeValid)
        {
            var address = IPAddress.Parse(addr);
            bool match = Utility.IPMatch(val, address, out var valid);

            Assert.Equal(shouldMatch, match);
            Assert.Equal(shouldBeValid, valid);
        }

        [Fact]
        public void TestMixedIPv4Address()
        {
            var ip = IPAddress.Parse("::ffff:192.168.1.1");
            var expected = IPAddress.Parse("192.168.1.1");

            Span<byte> integer = stackalloc byte[4];
            expected.TryWriteBytes(integer, out _);

            Assert.Equal(BinaryPrimitives.ReadUInt32BigEndian(integer), Utility.IPv4ToAddress(ip));
        }
    }
}
