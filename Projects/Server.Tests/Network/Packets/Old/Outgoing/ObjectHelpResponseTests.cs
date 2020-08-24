using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class ObjectHelpResponseTests
    {
        [Fact]
        public void TestObjectHelpResponse()
        {
            Serial s = 0x100;
            string text = "This is some testing text";

            Span<byte> data = new ObjectHelpResponse(s, text).Compile();

            int length = 9 + text.Length * 2;

            Span<byte> expectedData = stackalloc byte[length];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xB7); // Packet ID
            expectedData.Write(ref pos, (ushort)length); // Length
            expectedData.Write(ref pos, s);
            expectedData.WriteBigUniNull(ref pos, text);

            AssertThat.Equal(data, expectedData);
        }
    }
}
