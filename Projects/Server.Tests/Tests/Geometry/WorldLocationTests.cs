using System;
using Xunit;

namespace Server.Tests;

public sealed class WorldLocationTests : IClassFixture<ServerFixture>
{
    private static Map CreateMap(string name) => new(0, 0, 0, 1, 1, 0, name, MapRules.Internal);

    private const string _nullMap = "(-null-)";

    private const string _superLongMapName =
        "Some super long map name that nobody should be using, but probably someone will just because you can!";

    [Fact]
    public void TestWorldLocationToString()
    {
        const string mapName = "Internal Map";
        var map = CreateMap(mapName);

        const int max = int.MaxValue;
        const int min = int.MinValue;
        Assert.Equal($"(0, 0, 0) [{mapName}]", new WorldLocation(0, 0, 0, map).ToString());
        Assert.Equal($"(1, 1, 1) [{mapName}]", new WorldLocation(1, 1, 1, map).ToString());

        Assert.Equal($"(0, 0, 0) [{_nullMap}]", new WorldLocation(0, 0, 0, null).ToString());
        Assert.Equal($"(1, 1, 1) [{_nullMap}]", new WorldLocation(1, 1, 1, null).ToString());

        Assert.Equal($"({max}, {max}, {max}) [{mapName}]", new WorldLocation(max, max, max, map).ToString());
        Assert.Equal($"({min}, {min}, {min}) [{mapName}]", new WorldLocation(min, min, min, map).ToString());

        Assert.Equal($"({max}, {max}, {max}) [{_nullMap}]", new WorldLocation(max, max, max, null).ToString());
        Assert.Equal($"({min}, {min}, {min}) [{_nullMap}]", new WorldLocation(min, min, min, null).ToString());

        map = CreateMap(_superLongMapName);

        Assert.Equal($"({max}, {max}, {max}) [{_superLongMapName}]", new WorldLocation(max, max, max, map).ToString());
        Assert.Equal($"({min}, {min}, {min}) [{_superLongMapName}]", new WorldLocation(min, min, min, map).ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Internal Map")]
    [InlineData(_superLongMapName)]
    public void TestWorldLocationTryFormatSucceeds(string mapName)
    {
        var map = mapName != null ? CreateMap(mapName) : null;
        var mapNameLength = mapName?.Length ?? _nullMap.Length;

        const int max = int.MaxValue;
        const int min = int.MinValue;
        char[] array = new char[128];

        var p1 = new WorldLocation(0, 0, 0, map);
        Assert.True(p1.TryFormat(array, out var cp1, null, null));
        Assert.Equal(9 + 3 + mapNameLength, cp1);
        Array.Clear(array);

        var p2 = new WorldLocation(1, 1, 1, map);
        Assert.True(p2.TryFormat(array, out var cp2, null, null));
        Assert.Equal(9 + 3 + mapNameLength, cp2);
        Array.Clear(array);

        array = new char[256]; // We need a bigger buffer!
        var p3 = new WorldLocation(max, max, max, map);
        Assert.True(p3.TryFormat(array, out var cp3, null, null));
        Assert.Equal(9 + 30 + mapNameLength, cp3);
        Array.Clear(array);

        var p4 = new WorldLocation(min, min, min, map);
        Assert.True(p4.TryFormat(array, out var cp4, null, null));
        Assert.Equal(9 + 33 + mapNameLength, cp4);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Internal Map")]
    public void TestWorldLocationTryFormatFails(string mapName)
    {
        var map = mapName != null ? CreateMap(mapName) : null;

        const int max = int.MaxValue;
        const int min = int.MinValue;
        char[] array = new char[1];

        var p1 = new WorldLocation(0, 0, 0, map);
        Assert.False(p1.TryFormat(array, out var cp1, null, null));
        Assert.Equal(0, cp1);
        Array.Clear(array);

        var p2 = new WorldLocation(1, 1, 1, map);
        Assert.False(p2.TryFormat(array, out var cp2, null, null));
        Assert.Equal(0, cp2);
        Array.Clear(array);

        var p3 = new WorldLocation(max, max, max, map);
        Assert.False(p3.TryFormat(array, out var cp3, null, null));
        Assert.Equal(0, cp3);
        Array.Clear(array);

        var p4 = new WorldLocation(min, min, min, map);
        Assert.False(p4.TryFormat(array, out var cp4, null, null));
        Assert.Equal(0, cp4);
    }

    [Fact]
    public void TestWorldLocationTryParse()
    {
        var validMap = Map.Felucca;
        // Happy Path
        Assert.True(WorldLocation.TryParse("(101, 23, 55) [Felucca]", null, out var p));
        Assert.Equal(new WorldLocation(101, 23, 55, validMap), p);
        Assert.Equal(new WorldLocation(101, 23, 55, validMap), WorldLocation.Parse("(101, 23, 55) [Felucca]", null));

        // Trimming
        Assert.True(WorldLocation.TryParse(" (101,23 ,55)   [Felucca]   ", null, out p));
        Assert.Equal(new WorldLocation(101, 23, 55, validMap), p);
        Assert.Equal(new WorldLocation(101, 23, 55, validMap), WorldLocation.Parse(" (101,23 ,55)   [Felucca]   ", null));

        // Null Map
        Assert.True(WorldLocation.TryParse(" (101,23 ,55)  [(-null-)]", null, out p));
        Assert.Equal(new WorldLocation(101, 23, 55, null), p);
        Assert.Equal(new WorldLocation(101, 23, 55, null), WorldLocation.Parse(" (101,23 ,55)  [(-null-)]", null));

        // No Brackets
        Assert.False(WorldLocation.TryParse("(101, 23 55) Felucca]", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(101, 23, 55) Felucca]", null));

        Assert.False(WorldLocation.TryParse("(101, 23 55) [Felucca", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(101, 23, 55) [Felucca", null));

        // No parenthesis
        Assert.False(WorldLocation.TryParse("101, 23 55) [Felucca]", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("101, 23, 55) [Felucca]", null));

        Assert.False(WorldLocation.TryParse("(101, 23, 55 [Felucca]", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(101, 23, 55 [Felucca]", null));

        // No Map - We don't support maps with no name, sorry.
        Assert.False(WorldLocation.TryParse("(101, 23, 55) []", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(101, 23, 55) []", null));

        // No numbers
        Assert.False(WorldLocation.TryParse("()", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("()", null));

        Assert.False(WorldLocation.TryParse("(,)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(,)", null));

        Assert.False(WorldLocation.TryParse("(|)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(|)", null));

        Assert.False(WorldLocation.TryParse("() []", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("() []", null));

        Assert.False(WorldLocation.TryParse("(101) [Felucca]", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(101) [Felucca]", null));

        Assert.False(WorldLocation.TryParse("(101,) [Felucca]", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(101,) [Felucca]", null));

        Assert.False(WorldLocation.TryParse("(,23) [Felucca]", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(,23) [Felucca]", null));

        Assert.False(WorldLocation.TryParse("(,23,55) [Felucca]", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(,23,55) [Felucca]", null));

        Assert.False(WorldLocation.TryParse("(,23,) [Felucca]", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => WorldLocation.Parse("(,23,) [Felucca]", null));
    }
}
