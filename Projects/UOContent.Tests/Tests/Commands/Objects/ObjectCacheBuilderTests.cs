using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

public class ObjectCacheBuilderTests
{
    [Fact]
    public void Build_produces_index_row_and_detail_chunk()
    {
        var extracted = new ExtractedObject(
            typeof(Runebook),
            "item",
            "Items.Skill Items.Magical",
            new LeanMetadata(8901, 0x461, "runebook", 1041267),
            [new CtorDoc()],
            [new PropertyDoc { Name = "Hue", Type = "int" }],
            [new OplLine { Cliloc = 1041267 }],
            "Item"
        );

        var (index, chunks) = ObjectCacheBuilder.Build([extracted], "2026-07-19T00:00:00Z");

        var row = Assert.Single(index.Objects);
        Assert.Equal("Runebook", row.Type);
        Assert.Equal("items.skill-items.magical", row.Chunk);
        Assert.Equal(8901, row.ItemID);

        var chunk = Assert.Contains("items.skill-items.magical", chunks);
        Assert.Contains("Runebook", chunk.Keys);
        Assert.Equal("Item", chunk["Runebook"].BaseType);
    }
}
