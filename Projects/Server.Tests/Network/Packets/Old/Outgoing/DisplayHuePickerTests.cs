using System;
using System.Buffers;
using Server.HuePickers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class DisplayHuePickerTests
    {
        [Fact]
        public void TestDisplayHuePicker()
        {
            const ushort itemID = 0xFF01;
            var huePicker = new HuePicker(itemID);

            Span<byte> data = new DisplayHuePicker(huePicker).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x95);
            expectedData.Write(ref pos, huePicker.Serial);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (ushort)0);
#else
            pos += 2;
#endif
            expectedData.Write(ref pos, itemID);

            AssertThat.Equal(data, expectedData);
        }
    }
}
