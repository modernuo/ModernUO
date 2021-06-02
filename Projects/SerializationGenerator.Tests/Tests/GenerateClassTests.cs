using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using SerializationGenerator;
using Xunit;

namespace SerializationGeneratorTests
{
    public class GenerateClassTests
    {
        [Fact]
        public void Test1()
        {
            var source = new StringBuilder();
            source.GenerateClassStart(
                "TestClass",
                ImmutableArray<ITypeSymbol>.Empty
            );

            Assert.NotEmpty(source.ToString());
        }
    }
}
