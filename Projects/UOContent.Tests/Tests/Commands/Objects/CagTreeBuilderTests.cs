using System.Linq;
using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

[Collection("Sequential UOContent Tests")]
public class CagTreeBuilderTests
{
    [Fact]
    public void BuildTree_creates_nested_categories_and_resolves_types()
    {
        var index = new ObjectIndexFile
        {
            Objects =
            [
                new ObjectIndexEntry
                {
                    Type = "Katana", Entity = "item", Category = "Items.Weapons.Swords",
                    Chunk = "items.weapons.swords", ItemID = 0x13FF, Hue = 0
                }
            ]
        };

        var root = CAGLoader.BuildTree(index);

        var items = Assert.IsType<CAGCategory>(FindChild(root, "Items"));
        var weapons = Assert.IsType<CAGCategory>(FindChild(items, "Weapons"));
        var swords = Assert.IsType<CAGCategory>(FindChild(weapons, "Swords"));

        var leaf = Assert.IsType<CAGObject>(swords.Nodes[0]);
        Assert.Equal(typeof(Katana), leaf.Type);
        Assert.Equal(0x13FF, leaf.ItemID);
    }

    [Fact]
    public void BuildTree_keeps_multiple_objects_in_one_category()
    {
        var index = new ObjectIndexFile
        {
            Objects =
            [
                new ObjectIndexEntry { Type = "Katana",   Entity = "item", Category = "Items.Weapons.Swords", Chunk = "items.weapons.swords", ItemID = 0x13FF, Hue = 0 },
                new ObjectIndexEntry { Type = "Longsword", Entity = "item", Category = "Items.Weapons.Swords", Chunk = "items.weapons.swords", ItemID = 0x0F5E, Hue = 0 }
            ]
        };

        var root = CAGLoader.BuildTree(index);
        var swords = (CAGCategory)FindNestedCategory(root, "Items", "Weapons", "Swords");

        var leafTypes = swords.Nodes.OfType<CAGObject>().Select(o => o.Type).ToList();
        Assert.Contains(typeof(Katana), leafTypes);
        Assert.Contains(typeof(Server.Items.Longsword), leafTypes);
    }

    private static CAGNode FindChild(CAGCategory parent, string title)
    {
        foreach (var node in parent.Nodes)
        {
            if (node.Title == title)
            {
                return node;
            }
        }

        throw new Xunit.Sdk.XunitException($"No child '{title}'.");
    }

    private static CAGNode FindNestedCategory(CAGCategory root, params string[] titles)
    {
        CAGNode current = root;
        foreach (var title in titles)
        {
            current = FindChild((CAGCategory)current, title);
        }

        return current;
    }
}
