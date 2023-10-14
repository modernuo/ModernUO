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
        Assert.Equal(entity1, linkList.First);
        Assert.Equal(entity1, linkList.Last);

        var entity2 = new TestEntity(2);

        linkList.AddFirst(entity2);

        Assert.True(entity2.OnLinkList);
        Assert.Equal(2, linkList.Count);
        Assert.Equal(entity1, linkList.Last);
        Assert.Equal(entity2, entity1.Previous);

        Assert.Equal(entity2, linkList.First);
        Assert.Equal(entity1, entity2.Next);
    }

    [Fact]
    public void TestAddingElementsLast()
    {
        var linkList = new ValueLinkList<TestEntity>();

        var entity1 = new TestEntity(1);
        linkList.AddLast(entity1);

        Assert.True(entity1.OnLinkList);
        Assert.Equal(1, linkList.Count);
        Assert.Equal(entity1, linkList.First);
        Assert.Equal(entity1, linkList.Last);

        var entity2 = new TestEntity(2);

        linkList.AddLast(entity2);

        Assert.True(entity2.OnLinkList);
        Assert.Equal(2, linkList.Count);
        Assert.Equal(entity1, linkList.First);
        Assert.Equal(entity2, entity1.Next);

        Assert.Equal(entity2, linkList.Last);
        Assert.Equal(entity1, entity2.Previous);
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
        Assert.Equal(entity1, linkList.First);
        Assert.Equal(entity2, linkList.Last);

        var entity3 = new TestEntity(3);
        linkList.AddBefore(entity2, entity3);
        Assert.True(entity3.OnLinkList);
        Assert.Equal(3, linkList.Count);

        Assert.Equal(entity1, entity3.Previous);
        Assert.Equal(entity2, entity3.Next);

        // First and Last should not have changed
        Assert.Equal(entity1, linkList.First);
        Assert.Equal(entity2, linkList.Last);
    }

    [Fact]
    public void TestAddingAfter()
    {
        var linkList = new ValueLinkList<TestEntity>();

        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);
        linkList.AddFirst(entity1);
        linkList.AddLast(entity2);

        Assert.True(entity1.OnLinkList);
        Assert.True(entity2.OnLinkList);
        Assert.Equal(2, linkList.Count);
        Assert.Equal(entity1, linkList.First);
        Assert.Equal(entity2, linkList.Last);

        var entity3 = new TestEntity(3);
        linkList.AddAfter(entity1, entity3);
        Assert.True(entity3.OnLinkList);
        Assert.Equal(3, linkList.Count);

        Assert.Equal(entity1, entity3.Previous);
        Assert.Equal(entity2, entity3.Next);

        // First and Last should not have changed
        Assert.Equal(entity1, linkList.First);
        Assert.Equal(entity2, linkList.Last);
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

        Assert.Equal(entity1, linkList.First);
        Assert.Equal(entity1, linkList.Last);

        Assert.Equal(entity2, entity6.Next);
        Assert.Equal(entity6, entity2.Previous);
        Assert.Equal(entity4, linkList2.Last);
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
        Assert.Equal(entity4, linkList.First);
        Assert.Equal(entity5, linkList.Last);
        Assert.Null(entity4.Previous);
        Assert.Null(entity5.Next);
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
        Assert.Equal(entity1, linkList.First);
        Assert.Equal(entity2, linkList.Last);
        Assert.Null(entity1.Previous);
        Assert.Null(entity2.Next);
    }
}
