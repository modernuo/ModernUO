using Server;
using Server.Engines.AdvancedSearch;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AdvancedSearchWorkerReadsTests
{
    private sealed class AllocatingNameItem : Item
    {
        public AllocatingNameItem() : base(0x1) { }
        public AllocatingNameItem(Serial s) : base(s) { }
        public override string DefaultName => new string('n', 1);
    }

    // The matcher reads Name through Item, so the hider never runs there.
    private sealed class AllocatingNameHiderItem : Item
    {
        public AllocatingNameHiderItem() : base(0x1) { }
        public AllocatingNameHiderItem(Serial s) : base(s) { }
        public new string Name => new string('h', 1);
    }

    private sealed class PureHiderAllocatingDefaultNameItem : Item
    {
        public PureHiderAllocatingDefaultNameItem() : base(0x1) { }
        public PureHiderAllocatingDefaultNameItem(Serial s) : base(s) { }
        public new string Name => "h";
        public override string DefaultName => new string('n', 1);
    }

    [Fact]
    public void AllocatingNameHider_IsWorkerSafe() =>
        Assert.True(AdvancedSearchWorkerReads.IsWorkerSafe(typeof(AllocatingNameHiderItem)));

    [Fact]
    public void PureNameHider_WithAllocatingDefaultName_IsNotWorkerSafe() =>
        Assert.False(AdvancedSearchWorkerReads.IsWorkerSafe(typeof(PureHiderAllocatingDefaultNameItem)));

    [Fact]
    public void PlainItem_IsWorkerSafe() => Assert.True(AdvancedSearchWorkerReads.IsWorkerSafe(typeof(Item)));

    [Fact]
    public void PlainMobile_IsWorkerSafe() => Assert.True(AdvancedSearchWorkerReads.IsWorkerSafe(typeof(Mobile)));

    [Fact]
    public void ItemWithAllocatingDefaultName_IsNotWorkerSafe() =>
        Assert.False(AdvancedSearchWorkerReads.IsWorkerSafe(typeof(AllocatingNameItem)));

    [Fact]
    public void NonEntityType_IsNotWorkerSafe() => Assert.False(AdvancedSearchWorkerReads.IsWorkerSafe(typeof(string)));
}
