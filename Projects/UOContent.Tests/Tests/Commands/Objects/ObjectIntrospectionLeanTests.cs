using Server.Commands;
using Server.Items;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

[Collection("Sequential UOContent Tests")]
public class ObjectIntrospectionLeanTests
{
    [SkippableFact]
    public void ExtractLean_reads_item_id_from_a_weapon()
    {
        // Requires client TileData: ExtractLean clamps itemID > TileData.MaxItemValue to 1, and
        // MaxItemValue is 0 when tiledata.mul is absent (CI), so the real 0x13FF only survives with data.
        TileDataRequirement.SkipIfMissing();

        // Katana ctor is base(0x13FF) — era-independent.
        var lean = ObjectIntrospection.ExtractLean(typeof(Katana));
        Assert.Equal(0x13FF, lean.ItemID);
    }

    [Fact]
    public void ExtractLean_reads_hue_and_cliloc_from_a_runebook()
    {
        // Runebook sets Hue = 0x461 and LabelNumber 1041267 regardless of era.
        var lean = ObjectIntrospection.ExtractLean(typeof(Runebook));
        Assert.Equal(0x461, lean.Hue);
        Assert.Equal(1041267, lean.Cliloc);
    }
}
