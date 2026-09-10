using Server;
using Server.Engines.AdvancedSearch;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AdvancedSearchTypesTests
{
    public class Subject
    {
        public Poison Venom { get; set; }
    }

    [Fact]
    public void Poison_ReferenceTypeParsedViaTypes()
    {
        PoisonKinds.Configure(); // idempotent; registers Lesser..Lethal now that Core.Expansion is set

        // Poison is a reference type implementing ISpanParsable; its value routes through the shared
        // Server.Types converter. Poison.Parse returns the registered singleton, so "= Lethal" is a
        // reference-equality match — this is the case that previously compared a Poison against the
        // raw string and always failed.
        var subject = new Subject { Venom = Poison.Lethal };

        Assert.True(AdvancedSearchConditions.Compile(typeof(Subject), "Venom=Lethal")(subject));
        Assert.False(AdvancedSearchConditions.Compile(typeof(Subject), "Venom=Lesser")(subject));

        var ex = Record.Exception(() =>
            Assert.False(AdvancedSearchConditions.Compile(typeof(Subject), "Venom=notapoison")(subject)));
        Assert.Null(ex);
    }
}
