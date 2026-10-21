using Server;
using Server.Engines.AdvancedSearch;
using Xunit;

namespace UOContent.Tests;

// Registers the poison table, which is process-wide, so this stays in the sequential collection.
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
        // The fixture does not run PoisonKinds.Configure, and a second registration throws.
        if (Poison.Poisons.Count == 0)
        {
            PoisonKinds.Configure();
        }

        // Poison is a reference type implementing ISpanParsable; its value routes through the shared
        // Server.Types converter. Poison.Parse returns the registered singleton, so "= Lethal" is a
        // reference-equality match — this is the case that previously compared a Poison against the
        // raw string and always failed.
        var subject = new Subject { Venom = Poison.Lethal };

        Assert.True(AdvancedSearchConditions.Compile(typeof(Subject), "Venom=Lethal")(subject));
        Assert.False(AdvancedSearchConditions.Compile(typeof(Subject), "Venom=Lesser")(subject));

        Assert.False(AdvancedSearchConditions.Compile(typeof(Subject), "Venom=notapoison")(subject));
    }
}
