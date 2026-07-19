using System.Linq;
using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

[Collection("Sequential UOContent Tests")]
public class ObjectIntrospectionPropertiesTests
{
    [Fact]
    public void ExtractProperties_includes_inherited_item_command_properties()
    {
        var props = ObjectIntrospection.ExtractProperties(typeof(Runebook));

        var hue = Assert.Single(props, p => p.Name == "Hue");
        Assert.Equal("int", hue.Type);

        var lootType = Assert.Single(props, p => p.Name == "LootType");
        Assert.NotNull(lootType.EnumValues);
        Assert.Contains("Blessed", lootType.EnumValues);
    }
}
