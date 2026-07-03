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
    public void EmitsMageArmorAndSelfRepair_DoesNotReadLowerStatReqOrDurabilityFromContainer()
    {
        var attrs = new AosArmorAttributes(null)
        {
            MageArmor = 1,
            SelfRepair = 4,
            LowerStatReq = 50,     // container value must NOT be auto-emitted (it's passed in by the consumer)
            DurabilityBonus = 10   // never emitted by this method
        };

        var opl = new ObjectPropertyList(null);
        attrs.GetProperties(opl); // no lowerStatReq arg
        var map = Decode(opl);

        Assert.Equal("", map[1060437]);  // MageArmor (no-arg)
        Assert.Equal("4", map[1060450]); // SelfRepair
        Assert.False(map.ContainsKey(1060435)); // LowerStatReq NOT read from container
        Assert.False(map.ContainsKey(1060410)); // DurabilityBonus excluded
    }

    [Fact]
    public void EmitsLowerStatReqWhenPassed()
    {
        var attrs = new AosArmorAttributes(null) { MageArmor = 1, LowerStatReq = 50 };

        var opl = new ObjectPropertyList(null);
        attrs.GetProperties(opl); // computed value passed by the consumer, not the raw 50
        var map = Decode(opl);

        Assert.Equal("77", map[1060435]); // emitted from the param, not the container's 50
        Assert.Equal("", map[1060437]);   // MageArmor still emitted
    }
}
