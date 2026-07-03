using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AosWeaponAttributesPropertiesTests
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
    public void EmitsHitEffectsUseBestSkillMageWeaponSelfRepair()
    {
        var attrs = new AosWeaponAttributes(null)
        {
            UseBestSkill = 1,
            HitFireball = 12,
            HitLeechHits = 20,
            MageWeapon = 25,
            SelfRepair = 3
        };

        var opl = new ObjectPropertyList(null);
        attrs.GetProperties(opl);
        var map = Decode(opl);

        Assert.Equal("", map[1060400]);   // UseBestSkill (no-arg)
        Assert.Equal("12", map[1060420]); // HitFireball
        Assert.Equal("20", map[1060422]); // HitLeechHits
        Assert.Equal("5", map[1060438]);  // MageWeapon => 30 - 25
        Assert.Equal("3", map[1060450]);  // SelfRepair
        Assert.False(map.ContainsKey(1060435)); // LowerStatReq not emitted without the param
    }

    [Fact]
    public void EmitsLowerStatReqWhenPassed()
    {
        var attrs = new AosWeaponAttributes(null) { MageWeapon = 25 };

        var opl = new ObjectPropertyList(null);
        attrs.GetProperties(opl); // computed value passed by the weapon
        var map = Decode(opl);

        Assert.Equal("40", map[1060435]); // lower requirements, in cliloc order before MageWeapon
        Assert.Equal("5", map[1060438]);  // MageWeapon still emitted
    }
}
