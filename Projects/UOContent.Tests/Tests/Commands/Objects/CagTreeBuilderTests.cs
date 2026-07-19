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
}
