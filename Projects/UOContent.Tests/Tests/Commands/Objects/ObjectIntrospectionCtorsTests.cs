using System.Linq;
using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

[Collection("Sequential UOContent Tests")]
public class ObjectIntrospectionCtorsTests
{
    [Fact]
    public void ExtractCtors_lists_both_constructible_runebook_overloads()
    {
        // Runebook has [Constructible] Runebook() and [Constructible] Runebook(int maxCharges).
        var ctors = ObjectIntrospection.ExtractCtors(typeof(Runebook));

        Assert.Equal(2, ctors.Count);
        Assert.Contains(ctors, c => c.Parameters.Count == 0);

        var parameterized = Assert.Single(ctors, c => c.Parameters.Count == 1);
        Assert.Equal("maxCharges", parameterized.Parameters[0].Name);
        Assert.Equal("int", parameterized.Parameters[0].Type);
    }
}
