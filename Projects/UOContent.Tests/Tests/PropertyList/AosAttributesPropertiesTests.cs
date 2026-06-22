using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AosAttributesPropertiesTests
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
    public void EmitsRawAttributesInCanonicalOrder()
    {
        var attrs = new AosAttributes(null)
        {
            DefendChance = 5,
            BonusStr = 10,
            NightSight = 1,
            SpellChanneling = 1,
            WeaponSpeed = 7
        };

        var opl = new ObjectPropertyList(null);
        attrs.GetProperties(opl);
        var map = Decode(opl);

        Assert.Equal("5", map[1060408]);   // DefendChance
        Assert.Equal("10", map[1060485]);  // BonusStr
        Assert.Equal("", map[1060441]);    // NightSight (no-arg)
        Assert.Equal("", map[1060482]);    // SpellChanneling (no-arg)
        Assert.Equal("7", map[1060486]);   // WeaponSpeed
        Assert.False(map.ContainsKey(1060401)); // WeaponDamage not set
    }

    [Fact]
    public void AppliesComputedBonuses()
    {
        var attrs = new AosAttributes(null) { WeaponDamage = 10, AttackChance = 4, Luck = 100 };

        var opl = new ObjectPropertyList(null);
        attrs.GetProperties(opl, damageBonus: 5, hitChanceBonus: 3, luckBonus: 50);
        var map = Decode(opl);

        Assert.Equal("15", map[1060401]);  // WeaponDamage + damageBonus
        Assert.Equal("7", map[1060415]);   // AttackChance + hitChanceBonus
        Assert.Equal("150", map[1060436]); // Luck + luckBonus
    }
}
