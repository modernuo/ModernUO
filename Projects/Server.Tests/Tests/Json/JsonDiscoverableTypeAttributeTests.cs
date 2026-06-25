using Server.Json;
using Xunit;

namespace Server.Tests.Json;

public class JsonDiscoverableTypeAttributeTests
{
    [JsonDiscoverableType]
    private sealed class DefaultName { }

    [JsonDiscoverableType("custom")]
    private sealed class Overridden { }

    [Fact]
    public void DefaultDiscriminator_IsNull()
    {
        var attr = (JsonDiscoverableTypeAttribute)System.Attribute.GetCustomAttribute(
            typeof(DefaultName), typeof(JsonDiscoverableTypeAttribute), false);
        Assert.NotNull(attr);
        Assert.Null(attr.Discriminator);
    }

    [Fact]
    public void OverrideDiscriminator_IsReturned()
    {
        var attr = (JsonDiscoverableTypeAttribute)System.Attribute.GetCustomAttribute(
            typeof(Overridden), typeof(JsonDiscoverableTypeAttribute), false);
        Assert.Equal("custom", attr.Discriminator);
    }
}
