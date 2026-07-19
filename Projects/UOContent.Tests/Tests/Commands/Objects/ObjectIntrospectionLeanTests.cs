using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

[Collection("Sequential UOContent Tests")]
public class ObjectIntrospectionLeanTests
{
    [Fact]
    public void ExtractLean_reads_item_id_from_a_weapon()
    {
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
