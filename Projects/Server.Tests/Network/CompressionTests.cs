using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class CompressionTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestCompression()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var acct = new AccountPacketTests.TestAccount(new[] { null, firstMobile, null, null, null });
            var info = new[]
            {
                new CityInfo("Test City", "Test Building", 50, 100, 10, -10)
            };

            var span = new CharacterList(acct, info).Compile();

            Span<byte> expected = stackalloc byte[NetworkCompression.BufferSize];
            NetworkCompression.Compress(span, 0, span.Length, expected, out var length);
            expected = expected.Slice(0, length);

            Span<byte> actual = stackalloc byte[0x10000]; // Pipe
            span.CopyTo(actual);

            length = NetworkCompression.Compress(actual.Slice(0, span.Length), actual);
            actual = actual.Slice(0, length);

            AssertThat.Equal(actual, expected);
        }
    }
}
