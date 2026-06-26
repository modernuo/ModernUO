using System.Text.Json;
using Server.Json;
using Xunit;

namespace Server.Tests.Json;

public class Rectangle3DConverterTests
{
    private static Rectangle3D RoundTrip(Rectangle3D value)
    {
        var json = JsonSerializer.Serialize(value, JsonConfig.DefaultOptions);
        return JsonSerializer.Deserialize<Rectangle3D>(json, JsonConfig.DefaultOptions);
    }

    [Theory]
    // homeRange-style full vertical range (z1=-128, depth 256 -> z2=128): must survive losslessly.
    [InlineData(100, 100, -128, 11, 11, 256)]
    // The omitted-z sentinel itself (z1=-128, z2=127 -> depth 255).
    [InlineData(100, 100, -128, 11, 11, 255)]
    // Ordinary bounds with a meaningful z.
    [InlineData(100, 100, 0, 11, 11, 5)]
    [InlineData(100, 100, 5, 11, 11, 122)] // z2 == 127 but z1 != -128
    [InlineData(100, 100, -128, 11, 11, 200)] // z1 == -128 but z2 != 127
    public void RoundTrips(int x, int y, int z, int w, int h, int d)
    {
        var rect = new Rectangle3D(x, y, z, w, h, d);
        Assert.Equal(rect, RoundTrip(rect));
    }

    [Fact]
    public void FullRangeSentinel_OmitsZ()
    {
        // z1=-128, z2=127 is the only case z is omitted.
        var json = JsonSerializer.Serialize(new Rectangle3D(0, 0, -128, 10, 10, 255), JsonConfig.DefaultOptions);
        Assert.DoesNotContain("z1", json);
        Assert.DoesNotContain("z2", json);
    }

    [Fact]
    public void HomeRangeBounds_WritesZ()
    {
        var json = JsonSerializer.Serialize(new Rectangle3D(0, 0, -128, 10, 10, 256), JsonConfig.DefaultOptions);
        Assert.Contains("\"z1\":", json);
        Assert.Contains("\"z2\":", json);
    }
}
