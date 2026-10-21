using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

[Collection("Sequential UOContent Tests")]
public class ObjectDiscoveryTests
{
    [Fact]
    public void Discover_includes_concrete_constructibles_and_excludes_abstract()
    {
        var types = ObjectIntrospection.DiscoverConstructibleTypes();

        Assert.Contains(typeof(Katana), types);
        Assert.Contains(typeof(Runebook), types);
        Assert.DoesNotContain(typeof(BaseWeapon), types); // abstract
    }
}
