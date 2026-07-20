using Server;
using Server.Engines.AdvancedSearch;
using Xunit;

namespace UOContent.Tests;

// Fixture-backed: parsing a reference type through Server.Types (Poison) needs the poison registry
// populated, which requires Core.Expansion (AOS+, set by the fixture) and PoisonKinds.Configure().
[Collection("Sequential UOContent Tests")]
public class AdvancedSearchTypesTests
{
    [Fact]
    public void CompareValues_Poison_ReferenceTypeParsedViaTypes()
    {
        PoisonKinds.Configure(); // idempotent; registers Lesser..Lethal now that Core.Expansion is set

        // Poison is a reference type implementing ISpanParsable; it can't use the compile-time span
        // path and routes through the shared Server.Types converter. Poison.Parse returns the
        // registered singleton, so "= Lethal" is a reference-equality match — this is the case that
        // previously compared a Poison against the raw string and always failed.
        var prop = Poison.Lethal;
        Assert.True(AdvancedSearchUtilities.CompareValues(typeof(Poison), prop, "Lethal", "="));
        Assert.False(AdvancedSearchUtilities.CompareValues(typeof(Poison), prop, "Lesser", "="));

        var ex = Record.Exception(() =>
            Assert.False(AdvancedSearchUtilities.CompareValues(typeof(Poison), prop, "notapoison", "=")));
        Assert.Null(ex);
    }
}
