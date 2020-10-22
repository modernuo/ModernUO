using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class EncodingTest
    {
        [Fact]
        public void TestsEncoding()
        {
            var p1 = new ScreenEffect(ScreenEffectType.DarkFlash).Compile();
            var p2 = new DamagePacketOld(0x1024, 100).Compile();

            Span<byte> compressed1 = stackalloc byte[0x1000];
            Span<byte> compressed2 = stackalloc byte[0x1000];

            NetworkCompression.Compress(p1, 0, p1.Length, compressed1, out var p1Length);
            compressed1 = compressed1.Slice(0, p1Length);
            NetworkCompression.Compress(p2, 0, p2.Length, compressed2, out var p2Length);
            compressed2 = compressed2.Slice(0, p2Length);


            Span<byte> combined = stackalloc byte[p1.Length + p2.Length];
            p1.CopyTo(combined);
            p2.CopyTo(combined.Slice(p1.Length));

            Span<byte> compressed3 = stackalloc byte[0x1000];
            NetworkCompression.Compress(combined, 0, combined.Length, compressed3, out var p3Length);
            compressed3 = compressed3.Slice(0, p3Length);

            Span <byte> p1p2 = stackalloc byte[compressed1.Length + compressed2.Length];
            compressed1.CopyTo(p1p2);
            compressed2.CopyTo(p1p2.Slice(compressed1.Length));

            AssertThat.Equal(compressed3, p1p2);
        }
    }
}
