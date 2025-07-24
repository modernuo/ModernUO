using System;
using System.Collections.Generic;
using Server.Items;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class ContainerTests
{
    [Theory]
    [InlineData(typeof(Container))]
    [InlineData(typeof(Item))]
    public void TestFindItemsByType(Type itemType)
    {
        var staticSerial = (Serial)0x3;

        var container = itemType.CreateInstance<Item>((Serial)0x1);
        container.AddItem(new Item((Serial)0x2));
        container.AddItem(new Static(staticSerial));

        Static staticItem = null;
        foreach (var item in container.FindItemsByType<Static>())
        {
            staticItem = item;
        }

        Assert.NotNull(staticItem);
        Assert.Equal(staticSerial, staticItem.Serial);
    }

    [Theory]
    [InlineData(typeof(Container))]
    [InlineData(typeof(Item))]
    public void TestFindItemsByTypeNested(Type itemType)
    {
        var static1 = new Static((Serial)0x3);
        var static2 = new Static((Serial)0x6);

        var container = itemType.CreateInstance<Item>((Serial)0x1);
        container.AddItem(new Item((Serial)0x2));
        var container2 = itemType.CreateInstance<Item>((Serial)0x4);
        container.AddItem(container2);

        var container3 = itemType.CreateInstance<Item>((Serial)0x5);
        container2.AddItem(container3);
        container3.AddItem(static2);

        container2.AddItem(static1);

        List<Static> statics = new List<Static>();
        foreach (var item in container.FindItemsByType<Static>())
        {
            statics.Add(item);
        }

        Assert.Equal(2, statics.Count);
        Assert.Equal(static1, statics[0]);
        Assert.Equal(static2, statics[1]);
    }

    [Theory]
    [InlineData(typeof(Container))]
    [InlineData(typeof(Item))]
    public void TestFindItemsByTypeNotMatching(Type itemType)
    {
        var container = itemType.CreateInstance<Item>((Serial)0x1);
        container.AddItem(new Item((Serial)0x2));
        var container2 = itemType.CreateInstance<Item>((Serial)0x4);
        container.AddItem(container2);
        container2.AddItem(new Item((Serial)0x5));

        Static staticItem = null;
        foreach (var item in container.FindItemsByType<Static>())
        {
            staticItem = item;
        }

        Assert.Null(staticItem);
    }

    [Theory]
    [InlineData(typeof(Container))]
    [InlineData(typeof(Item))]
    public void TestFindItemsByTypeShouldThrowWhenModified(Type itemType)
    {
        var container = itemType.CreateInstance<Item>((Serial)0x1);
        container.AddItem(new Item((Serial)0x2));
        var staticItem = new Static((Serial)0x3);
        container.AddItem(staticItem);
        container.AddItem(new Item((Serial)0x4));

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                foreach (var item in container.FindItemsByType<Static>())
                {
                    if (item == staticItem)
                    {
                        container.RemoveItem(staticItem);
                    }
                }
            }
        );
    }

    [Theory]
    [InlineData(typeof(Container))]
    [InlineData(typeof(Item))]
    public void TestEnumerateItemsByTypeWhenModified(Type itemType)
    {
        var container = itemType.CreateInstance<Item>((Serial)0x1);

        var item1 = new Item((Serial)0x2);
        container.AddItem(item1);
        var item2 = new Static((Serial)0x3);
        container.AddItem(item2);
        var item3 = new Item((Serial)0x4);
        container.AddItem(item3);

        foreach (var item in container.EnumerateItemsByType<Static>())
        {
            if (item == item2)
            {
                container.RemoveItem(item2);
            }
        }

        Assert.Equal(2, container.Items.Count);
        Assert.Collection(container.Items,
            item => Assert.Equal(item1, item),
            item => Assert.Equal(item3, item)
        );
    }
}
