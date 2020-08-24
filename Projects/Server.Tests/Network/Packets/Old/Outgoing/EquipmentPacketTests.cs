using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class EquipmentPacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestDisplayEquipmentInfo()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var item = new Item(Serial.LastItem + 1);

            var info = new EquipmentInfo(
                500000,
                m,
                false,
                new[]
                {
                    new EquipInfoAttribute(500001, 1),
                    new EquipInfoAttribute(500002, 2),
                    new EquipInfoAttribute(500002, 3)
                }
            );

            Span<byte> data = new DisplayEquipmentInfo(item, info).Compile();

            var attrs = info.Attributes;

            int length = 17 + (info.Unidentified ? 4 : 0) + attrs.Length * 6;
            if (info.Crafter != null) length += 6 + (info.Crafter.Name?.Length ?? 0);

            Span<byte> expectedData = stackalloc byte[length];

            int pos = 0;

            expectedData.Write(ref pos, (byte)0xBF); // Packet ID
            expectedData.Write(ref pos, (ushort)length); // Length
            expectedData.Write(ref pos, (ushort)0x10); // Subcommand
            expectedData.Write(ref pos, item.Serial);
            expectedData.Write(ref pos, info.Number);
            if (info.Crafter != null)
            {
                var name = info.Crafter.Name ?? "";
                expectedData.Write(ref pos, -3);
                expectedData.Write(ref pos, (ushort)name.Length);
                expectedData.WriteAscii(ref pos, name);
            }

            if (info.Unidentified) expectedData.Write(ref pos, -4);

            for (var i = 0; i < attrs.Length; i++)
            {
                var attr = attrs[i];
                expectedData.Write(ref pos, attr.Number);
                expectedData.Write(ref pos, (ushort)attr.Charges);
            }

            expectedData.Write(ref pos, (-1));

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestEquipUpdate()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var item = new Item(Serial.LastItem + 1) { Parent = m };

            Span<byte> data = new EquipUpdate(item).Compile();

            Span<byte> expectedData = stackalloc byte[15];
            int pos = 0;
            expectedData.Write(ref pos, (byte)0x2E); // Packet ID
            expectedData.Write(ref pos, item.Serial);
            expectedData.Write(ref pos, (ushort)item.ItemID);

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#else
            pos++;
#endif

            expectedData.Write(ref pos, (byte)item.Layer);
            expectedData.Write(ref pos, item.Parent.Serial);
            expectedData.Write(ref pos, (ushort)item.Hue);

            AssertThat.Equal(data, expectedData);
        }
    }
}
