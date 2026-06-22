using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AosArmorAttributesPropertiesTests
{
    private static Dictionary<int, string> Decode(ObjectPropertyList opl)
    {
        opl.Terminate();
        var buffer = opl.Buffer;
        var map = new Dictionary<int, string>();
        var pos = 15;
        while (true)
        {
            var cliloc = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(pos));
            pos += 4;
            if (cliloc == 0)
            {
                break;
            }

            var byteLen = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(pos));
            pos += 2;
            map[cliloc] = Encoding.Unicode.GetString(buffer, pos, byteLen);
            pos += byteLen;
        }

        return map;
    }

    [Fact]
    public void EmitsMageArmorAndSelfRepairOnly()
    {
        var attrs = new AosArmorAttributes(null)
        {
            MageArmor = 1,
            SelfRepair = 4,
            LowerStatReq = 50,     // must NOT be emitted here
            DurabilityBonus = 10   // must NOT be emitted here
        };

        var opl = new ObjectPropertyList(null);
        attrs.GetProperties(opl);
        var map = Decode(opl);

        Assert.Equal("", map[1060437]);  // MageArmor (no-arg)
        Assert.Equal("4", map[1060450]); // SelfRepair
        Assert.False(map.ContainsKey(1060435)); // LowerStatReq excluded
        Assert.False(map.ContainsKey(1060410)); // DurabilityBonus excluded
    }
}
