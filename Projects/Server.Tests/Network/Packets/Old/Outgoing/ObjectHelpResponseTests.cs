using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class ObjectHelpResponseTests
    {
        [Fact]
        public void TestObjectHelpResponse()
        {
            Serial s = 0x100;
            var text = "This is some testing text";

            var data = new ObjectHelpResponse(s, text).Compile();

            var length = 9 + text.Length * 2;

            Span<byte> expectedData = stackalloc byte[length];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xB7);     // Packet ID
            expectedData.Write(ref pos, (ushort)length); // Length
            expectedData.Write(ref pos, s);
            expectedData.WriteBigUniNull(ref pos, text);

            AssertThat.Equal(data, expectedData);
        }
    }
}
