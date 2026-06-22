using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Server.Tests.Regions;

// Data-only validation of the vendor-shop regions added for issue #1052.
// Reads Distribution/Data/regions.json (copied next to the test assembly by the
// CopyData build target) and verifies the new shop regions without requiring
// client map files, so it runs cleanly in CI. Mirrors the original Node.js
// reproduction: known vendor coordinates should now resolve to a shop-specific
// region nested under the town instead of only the broad town region.
public class VendorShopRegionTests
{
    private static readonly string[] ShopTowns = { "Britain", "Trinsic", "Vesper", "Minoc" };

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static List<RegionData> LoadRegions()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "regions.json");
        Assert.True(File.Exists(path), $"regions.json not found at {path}");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<RegionData>>(json, Options) ?? new List<RegionData>();
    }

    private static List<RegionData> VendorShops(List<RegionData> all) =>
        all.Where(
            r => r.Type == "NoHousingRegion" && r.Name != null && r.Parent != null &&
                 ShopTowns.Contains(r.Parent.Name)
        ).ToList();

    private static bool Contains(Rect r, int x, int y) => x >= r.X1 && x <= r.X2 && y >= r.Y1 && y <= r.Y2;

    private static bool ContainsAny(IEnumerable<Rect> rects, int x, int y) => rects.Any(r => Contains(r, x, y));

    private static bool Overlaps(Rect a, Rect b) => a.X1 <= b.X2 && b.X1 <= a.X2 && a.Y1 <= b.Y2 && b.Y1 <= a.Y2;

    // map, town, x, y, expected shop region name
    public static IEnumerable<object[]> ShopSamples() => new[]
    {
        new object[] { "Trammel", "Britain", 1450, 1617, "the Britain Bakery" },
        new object[] { "Trammel", "Britain", 1418, 1547, "the Britain Blacksmith" },
        new object[] { "Trammel", "Britain", 1467, 1686, "the Britain Tailor" },
        new object[] { "Trammel", "Britain", 1425, 1690, "the Britain Bank" },
        new object[] { "Felucca", "Britain", 1450, 1617, "the Britain Bakery" },
        new object[] { "Trammel", "Trinsic", 1880, 2802, "the Trinsic Bakery" },
        new object[] { "Trammel", "Trinsic", 1897, 2684, "the Trinsic Bank" },
        new object[] { "Trammel", "Vesper", 2998, 760, "the Vesper Bakery" },
        new object[] { "Trammel", "Vesper", 2881, 684, "the Vesper Bank" },
        new object[] { "Trammel", "Minoc", 2503, 552, "the Minoc Bank" },
        new object[] { "Trammel", "Minoc", 2471, 564, "the Minoc Blacksmith" },
        new object[] { "Felucca", "Minoc", 2503, 552, "the Minoc Bank" }
    };

    [Theory]
    [MemberData(nameof(ShopSamples))]
    public void VendorCoordinate_ResolvesToShopRegion_NestedUnderTown(
        string map, string town, int x, int y, string expectedShop
    )
    {
        var all = LoadRegions();

        // Exactly one new vendor-shop region covers the vendor coordinate, and it is the expected one.
        var matches = VendorShops(all)
            .Where(r => r.Map == map && ContainsAny(r.Area, x, y))
            .Select(r => r.Name)
            .ToList();

        Assert.Single(matches);
        Assert.Equal(expectedShop, matches[0]);

        // The shop is genuinely nested under the town: the parent town region also contains the point.
        var townRegion = all.Single(r => r.Name == town && r.Map == map && r.Parent == null);
        Assert.True(
            ContainsAny(townRegion.Area, x, y),
            $"The {town} ({map}) town region should also contain {x},{y}"
        );
    }

    [Fact]
    public void VendorShops_HaveValidStructure()
    {
        var all = LoadRegions();
        var shops = VendorShops(all);

        Assert.NotEmpty(shops);

        foreach (var shop in shops)
        {
            Assert.NotNull(shop.Area);
            Assert.NotEmpty(shop.Area);

            // Parent reference resolves to a region defined on the same map.
            Assert.Contains(all, r => r.Name == shop.Parent.Name && r.Map == shop.Parent.Map);
        }

        // Shop region names are unique per map.
        var duplicates = shops
            .GroupBy(s => (s.Map, s.Name))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Map}:{g.Key.Name}")
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void VendorShops_DoNotOverlapOtherRegions()
    {
        var all = LoadRegions();
        var shops = VendorShops(all);

        foreach (var shop in shops)
        {
            foreach (var other in all)
            {
                if (ReferenceEquals(other, shop) || other.Map != shop.Map || other.Area == null)
                {
                    continue;
                }

                // Overlapping the parent town region is expected (the shop is nested inside it).
                if (other.Name == shop.Parent.Name && other.Parent == null)
                {
                    continue;
                }

                foreach (var a in shop.Area)
                {
                    foreach (var b in other.Area)
                    {
                        Assert.False(
                            Overlaps(a, b),
                            $"{shop.Name} overlaps {other.Name ?? "(unnamed)"} on {shop.Map}"
                        );
                    }
                }
            }
        }
    }

    private sealed class RegionData
    {
        [JsonPropertyName("$type")]
        public string Type { get; set; }

        public string Name { get; set; }

        public string Map { get; set; }

        public ParentRef Parent { get; set; }

        public Rect[] Area { get; set; }
    }

    private sealed class ParentRef
    {
        public string Name { get; set; }

        public string Map { get; set; }
    }

    private sealed class Rect
    {
        public int X1 { get; set; }

        public int Y1 { get; set; }

        public int X2 { get; set; }

        public int Y2 { get; set; }
    }
}
