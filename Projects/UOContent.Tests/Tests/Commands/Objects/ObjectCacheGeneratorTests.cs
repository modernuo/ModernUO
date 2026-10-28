using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

[Collection("Sequential UOContent Tests")]
public class ObjectCacheGeneratorTests
{
    [Fact]
    public void Generate_builds_index_and_chunks_from_extracted_objects()
    {
        var discovered = new List<Type> { typeof(Runebook), typeof(Katana) };
        var categorization = new List<CAGJson>(); // empty -> both land in Items.Uncategorized

        var result = ObjectCacheGenerator.Generate(categorization, discovered);

        Assert.Equal(2, result.Index.Objects.Count);
        Assert.NotEmpty(result.Chunks);

        var runebook = Assert.Single(result.Index.Objects, o => o.Type == "Runebook");
        Assert.True(runebook.ItemID > 0);
        Assert.Equal(1041267, runebook.Cliloc);
        Assert.Equal("items.uncategorized", runebook.Chunk);

        Assert.Contains("Runebook", result.Report.Appended);
        Assert.Contains("items.uncategorized", result.Chunks.Keys);
        Assert.Contains("Runebook", result.Chunks["items.uncategorized"].Keys);
    }
}
