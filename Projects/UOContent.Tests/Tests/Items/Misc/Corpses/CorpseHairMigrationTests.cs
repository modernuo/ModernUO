using System;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

// Verifies the migration read type LegacyHairInfo consumes the exact legacy on-disk
// VirtualHairInfo block the way the generated Corpse migration does:
//   [bool present] then, if present, [int version][int itemId][int hue].
// Dropping/over-reading these bytes would trip the loader's exact-length validation,
// so full consumption (no leftover bytes) is the load-bearing assertion.
public class CorpseHairMigrationTests
{
    private static byte[] WriteLegacyHairTail(bool present, int itemId, int hue)
    {
        var writer = new BufferWriter(true);
        writer.Write(present);
        if (present)
        {
            writer.Write(0); // legacy VirtualHairInfo serialization version
            writer.Write(itemId);
            writer.Write(hue);
        }

        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);
        return buffer;
    }

    [Theory]
    [InlineData(0x203B, 1102)]
    [InlineData(0x2049, 0)]
    public void LegacyHairTail_Present_RoundTrips(int itemId, int hue)
    {
        var buffer = WriteLegacyHairTail(true, itemId, hue);
        var reader = new BufferReader(buffer);

        Assert.True(reader.ReadBool());
        var hair = new LegacyHairInfo();
        hair.Deserialize(reader);

        Assert.Equal(itemId, hair.ItemId);
        Assert.Equal(hue, hair.Hue);
        // The entire block must be consumed - the loader validates exact byte length.
        Assert.Equal(buffer.Length, reader.Position);
    }

    [Fact]
    public void LegacyHairTail_Absent_ConsumesOnlyPresenceBool()
    {
        var buffer = WriteLegacyHairTail(false, 0, 0);
        var reader = new BufferReader(buffer);

        Assert.False(reader.ReadBool());
        // Absent case: nothing else to read, and the presence bool was the only byte.
        Assert.Equal(buffer.Length, reader.Position);
    }
}
