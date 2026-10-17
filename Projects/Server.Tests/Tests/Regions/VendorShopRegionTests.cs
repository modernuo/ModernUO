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
    private static readonly string[] ShopTowns =
    {
        "Britain", "Buccaneer's Den", "Cove", "Delucia", "Gargoyle City", "Jhelom", "Luna",
        "Magincia", "Minoc", "Moonglow", "Nujel'm", "Ocllo", "Papua", "Reg Volon", "Royal City",
        "Serpent's Hold", "Skara Brae", "Trinsic", "Umbra", "Vesper", "Wind", "Yew", "Zento"
    };

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
        new object[] { "Felucca", "Minoc", 2503, 552, "the Minoc Bank" },
        // Towns added to cover all remaining vendor shops across every facet (issue #1052).
        // Jhelom is a TownRegion nested under "Jhelom Islands" — exercises the non-null-parent path.
        new object[] { "Trammel", "Jhelom", 1354, 3754, "the Jhelom Blacksmith" },
        new object[] { "Felucca", "Jhelom", 1364, 3732, "the Jhelom Bakery" },
        new object[] { "Trammel", "Moonglow", 4409, 1111, "the Moonglow Mage" },
        new object[] { "Trammel", "Skara Brae", 562, 2148, "the Skara Brae Ranger" },
        new object[] { "Felucca", "Ocllo", 3665, 2531, "the Ocllo Bard" },
        new object[] { "Trammel", "Magincia", 3703, 2249, "the Magincia Merchant" },
        new object[] { "Felucca", "Buccaneer's Den", 2659, 2194, "the Buccaneer's Den Thief Guild" },
        new object[] { "Trammel", "Yew", 570, 969, "the Yew Bowyer" },
        new object[] { "Ilshenar", "Gargoyle City", 840, 571, "the Gargoyle City Mage" },
        new object[] { "Malas", "Luna", 976, 527, "the Luna Tailor" },
        new object[] { "Malas", "Umbra", 2045, 1397, "the Umbra Jeweler" },
        new object[] { "Tokuno", "Zento", 739, 1223, "the Zento Carpenter" },
        new object[] { "TerMur", "Royal City", 783, 3491, "the Royal City Healer" },
        new object[] { "Trammel", "Serpent's Hold", 3031, 3350, "the Serpent's Hold Warrior" }
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
        // (Matched by type, not by Parent == null — some towns, e.g. Jhelom, are themselves nested
        // under a larger region such as "Jhelom Islands".)
        var townRegion = all.Single(r => r.Name == town && r.Map == map && r.Type == "TownRegion");
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
        var parentOf = BuildParentLookup(all);

        foreach (var shop in shops)
        {
            // A shop may overlap any ANCESTOR region (its parent town and that town's own
            // parents, e.g. "Jhelom Islands"): Region.Find resolves to the deepest child, so
            // the shop still wins. Only overlaps with siblings/unrelated regions are bugs.
            var ancestors = Ancestors(parentOf, shop.Map, shop.Parent.Name);

            foreach (var other in all)
            {
                if (ReferenceEquals(other, shop) || other.Map != shop.Map || other.Area == null)
                {
                    continue;
                }

                if (other.Name != null && ancestors.Contains((other.Map, other.Name)))
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

    // (Map, Name) -> parent (Map, Name), for walking a region's ancestor chain.
    private static Dictionary<(string Map, string Name), (string Map, string Name)?> BuildParentLookup(
        List<RegionData> all
    )
    {
        var map = new Dictionary<(string Map, string Name), (string Map, string Name)?>();
        foreach (var r in all)
        {
            if (r.Name != null)
            {
                map[(r.Map, r.Name)] = r.Parent != null ? (r.Parent.Map, r.Parent.Name) : null;
            }
        }

        return map;
    }

    private static HashSet<(string Map, string Name)> Ancestors(
        Dictionary<(string Map, string Name), (string Map, string Name)?> parentOf, string map, string name
    )
    {
        var set = new HashSet<(string Map, string Name)>();
        (string Map, string Name)? cur = (map, name);
        while (cur != null && set.Add(cur.Value))
        {
            cur = parentOf.TryGetValue(cur.Value, out var p) ? p : null;
        }

        return set;
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
