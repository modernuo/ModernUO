using System;
using Server.Collections;
using Xunit;

namespace Server.Tests;

public class ValueLinkListTests
{
    public class TestEntity : IValueLinkListNode<TestEntity>
    {
        public int Serial { get; set; }
        public TestEntity Next { get; set; }
        public TestEntity Previous { get; set; }
        public bool OnLinkList { get; set; }

        public TestEntity(int serial) => Serial = serial;
    }

    [Fact]
    public void TestDefaultListIsEmpty()
    {
        var linkList = new ValueLinkList<TestEntity>();
        Assert.Equal(0, linkList.Count);
    }

    [Fact]
    public void TestAddingElementsFirst()
    {
        var linkList = new ValueLinkList<TestEntity>();

        var entity1 = new TestEntity(1);
        linkList.AddFirst(entity1);

        Assert.True(entity1.OnLinkList);
        Assert.Equal(1, linkList.Count);

        Assert.Collection(linkList.ToArray(), item =>
            {
                Assert.Equal(entity1, item);
                Assert.Null(item.Previous);
                Assert.Null(item.Next);
            }
        );

        var entity2 = new TestEntity(2);
        linkList.AddFirst(entity2);

        Assert.True(entity2.OnLinkList);
        Assert.Equal(2, linkList.Count);

        Assert.Collection(linkList.ToArray(),
            item =>
            {
                Assert.Equal(entity2, item);
                Assert.Null(item.Previous);
                Assert.Equal(item.Next, entity1);
            },
            item =>
            {
                Assert.Equal(entity1, item);
                Assert.Equal(item.Previous, entity2);
                Assert.Null(item.Next);
            }
        );
    }

    [Fact]
    public void TestAddingElementsLast()
    {
        var linkList = new ValueLinkList<TestEntity>();

        var entity1 = new TestEntity(1);
        linkList.AddLast(entity1);

        Assert.True(entity1.OnLinkList);
        Assert.Equal(1, linkList.Count);

        Assert.Collection(linkList.ToArray(), item =>
            {
                Assert.Equal(entity1, item);
                Assert.Null(item.Previous);
                Assert.Null(item.Next);
            }
        );

        var entity2 = new TestEntity(2);

        linkList.AddLast(entity2);

        Assert.True(entity2.OnLinkList);
        Assert.Equal(2, linkList.Count);

        Assert.Collection(linkList.ToArray(),
            item =>
            {
                Assert.Equal(entity1, item);
                Assert.Null(item.Previous);
                Assert.Equal(item.Next, entity2);
            },
            item =>
            {
                Assert.Equal(entity2, item);
                Assert.Equal(item.Previous, entity1);
                Assert.Null(item.Next);
            }
        );
    }

    [Fact]
    public void TestAddingBefore()
    {
        var linkList = new ValueLinkList<TestEntity>();

        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);
        linkList.AddFirst(entity1);
        linkList.AddLast(entity2);

        Assert.True(entity1.OnLinkList);
        Assert.True(entity2.OnLinkList);
        Assert.Equal(2, linkList.Count);

        Assert.Collection(linkList.ToArray(),
            item =>
            {
                Assert.Equal(entity1, item);
                Assert.Null(item.Previous);
                Assert.Equal(item.Next, entity2);
            },
            item =>
            {
                Assert.Equal(entity2, item);
                Assert.Equal(item.Previous, entity1);
                Assert.Null(item.Next);
            }
        );


        var entity3 = new TestEntity(3);
        linkList.AddBefore(entity2, entity3);
        Assert.True(entity3.OnLinkList);
        Assert.Equal(3, linkList.Count);

        Assert.Collection(linkList.ToArray(),
            item =>
            {
                Assert.Equal(entity1, item);
                Assert.Null(item.Previous);
                Assert.Equal(item.Next, entity3);
            },
            item =>
            {
                Assert.Equal(entity3, item);
                Assert.Equal(item.Previous, entity1);
                Assert.Equal(item.Next, entity2);
            },
            item =>
            {
                Assert.Equal(entity2, item);
                Assert.Equal(item.Previous, entity3);
                Assert.Null(item.Next);
            }
        );
    }

    [Fact]
    public void TestAddingAfter()
    {
        var linkList = new ValueLinkList<TestEntity>();

        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);
        linkList.AddFirst(entity1);
        linkList.AddLast(entity2);

        var entity3 = new TestEntity(3);
        linkList.AddAfter(entity1, entity3);
        Assert.True(entity3.OnLinkList);
        Assert.Equal(3, linkList.Count);

        Assert.Collection(linkList.ToArray(),
            item =>
            {
                Assert.Equal(entity1, item);
                Assert.Null(item.Previous);
                Assert.Equal(item.Next, entity3);
            },
            item =>
            {
                Assert.Equal(entity3, item);
                Assert.Equal(item.Previous, entity1);
                Assert.Equal(item.Next, entity2);
            },
            item =>
            {
                Assert.Equal(entity2, item);
                Assert.Equal(item.Previous, entity3);
                Assert.Null(item.Next);
            }
        );
    }

    [Fact]
    public void TestAddingRangeElementsLast()
    {
        var linkList = new ValueLinkList<TestEntity>();

        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);
        var entity3 = new TestEntity(3);
        var entity4 = new TestEntity(4);
        linkList.AddLast(entity1);
        linkList.AddLast(entity2);
        linkList.AddLast(entity3);
        linkList.AddLast(entity4);

        var linkList2 = new ValueLinkList<TestEntity>();
        var entity5 = new TestEntity(5);
        var entity6 = new TestEntity(6);
        linkList2.AddLast(entity5);
        linkList2.AddLast(entity6);

        linkList2.AddLast(ref linkList, entity2, entity4);

        Assert.Equal(1, linkList.Count);
        Assert.Equal(5, linkList2.Count);

        Assert.Collection(linkList.ToArray(),
            item =>
            {
                Assert.Equal(entity1, item);
                Assert.Null(item.Previous);
                Assert.Null(item.Next);
            }
        );

        Assert.Collection(linkList2.ToArray(),
            item =>
            {
                Assert.Equal(entity5, item);
                Assert.Null(item.Previous);
                Assert.Equal(item.Next, entity6);
            },
            item =>
            {
                Assert.Equal(entity6, item);
                Assert.Equal(item.Previous, entity5);
                Assert.Equal(item.Next, entity2);
            },
            item =>
            {
                Assert.Equal(entity2, item);
                Assert.Equal(item.Previous, entity6);
                Assert.Equal(item.Next, entity3);
            },
            item =>
            {
                Assert.Equal(entity3, item);
                Assert.Equal(item.Previous, entity2);
                Assert.Equal(item.Next, entity4);
            },
            item =>
            {
                Assert.Equal(entity4, item);
                Assert.Equal(item.Previous, entity3);
                Assert.Null(item.Next);
            }
        );
    }

    [Fact]
    public void TestRemoveAllBefore()
    {
        var linkList = new ValueLinkList<TestEntity>();

        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);
        var entity3 = new TestEntity(3);
        var entity4 = new TestEntity(4);
        var entity5 = new TestEntity(5);
        linkList.AddLast(entity1);
        linkList.AddLast(entity2);
        linkList.AddLast(entity3);
        linkList.AddLast(entity4);
        linkList.AddLast(entity5);

        linkList.RemoveAllBefore(entity4);

        Assert.Equal(2, linkList.Count);
        Assert.Collection(linkList.ToArray(),
            item =>
            {
                Assert.Equal(entity4, item);
                Assert.Null(item.Previous);
                Assert.Equal(item.Next, entity5);
            },
            item =>
            {
                Assert.Equal(entity5, item);
                Assert.Equal(item.Previous, entity4);
                Assert.Null(item.Next);
            }
        );
    }

    [Fact]
    public void TestRemoveAllAfter()
    {
        var linkList = new ValueLinkList<TestEntity>();

        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);
        var entity3 = new TestEntity(3);
        var entity4 = new TestEntity(4);
        var entity5 = new TestEntity(5);
        linkList.AddLast(entity1);
        linkList.AddLast(entity2);
        linkList.AddLast(entity3);
        linkList.AddLast(entity4);
        linkList.AddLast(entity5);

        linkList.RemoveAllAfter(entity2);

        Assert.Equal(2, linkList.Count);
        Assert.Collection(linkList.ToArray(),
            item =>
            {
                Assert.Equal(entity1, item);
                Assert.Null(item.Previous);
                Assert.Equal(item.Next, entity2);
            },
            item =>
            {
                Assert.Equal(entity2, item);
                Assert.Equal(item.Previous, entity1);
                Assert.Null(item.Next);
            }
        );
    }

    [Fact]
    public void TestVersionIncrements()
    {
        var linkList = new ValueLinkList<TestEntity>();

        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);
        var entity3 = new TestEntity(3);
        var entity4 = new TestEntity(4);

        var version = 0;
        Assert.Equal(version, linkList.Version);

        linkList.AddFirst(entity1); // 1
        Assert.Equal(++version, linkList.Version);

        linkList.AddLast(entity2); // 1, 2
        Assert.Equal(++version, linkList.Version);

        linkList.AddBefore(entity2, entity3); // 1, 3, 2
        Assert.Equal(++version, linkList.Version);

        linkList.AddAfter(entity3, entity4); // 1, 3, 4, 2
        Assert.Equal(++version, linkList.Version);

        linkList.Remove(entity1); // 3, 4, 2
        Assert.Equal(++version, linkList.Version);

        linkList.RemoveAllAfter(entity4); // 3, 4
        Assert.Equal(++version, linkList.Version);

        linkList.RemoveAllBefore(entity4); // 4
        Assert.Equal(++version, linkList.Version);

        linkList.RemoveAll(); // None
        Assert.Equal(++version, linkList.Version);
    }

    [Fact]
    public void TestThrowsIfModifiedWhileIterating()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
            {
                var linkList = new ValueLinkList<TestEntity>();

                var entity1 = new TestEntity(1);
                var entity2 = new TestEntity(2);

                linkList.AddFirst(entity1);
                linkList.AddLast(entity2);

                foreach (var item in linkList)
                {
                    linkList.Remove(item);
                }
            }
        );
    }

    [Fact]
    public void TestThrowsIfModifiedWhileIteratingNested()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
            {
                var linkList = new ValueLinkList<TestEntity>();

                var entity1 = new TestEntity(1);
                var entity2 = new TestEntity(2);

                linkList.AddFirst(entity1);
                linkList.AddLast(entity2);

                foreach (var item in linkList)
                {
                    foreach (var nestedItem in linkList)
                    {
                        linkList.Remove(nestedItem);
                    }
                }
            }
        );
    }
}
