using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace Server.Tests.Mobiles;

[Collection("Sequential UOContent Tests")]
public class VendorBuybackTests : IDisposable
{
    private readonly MobileTestHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private sealed class BandageSB : SBInfo
    {
        private readonly GenericSellInfo _sellInfo = new();

        public BandageSB() => _sellInfo.Add(typeof(Bandage), 2);

        public override IShopSellInfo SellInfo => _sellInfo;

        public override List<GenericBuyInfo> BuyInfo { get; } = [];
    }

    private sealed class BandageVendor : TestVendor
    {
        protected override void AddSBInfos(List<SBInfo> sbInfos) => sbInfos.Add(new BandageSB());
    }

    // Same sell price as BandageSB, but a buy list at the 250-entry limit so buyback capacity is 0.
    private sealed class FullStockSB : SBInfo
    {
        private readonly GenericSellInfo _sellInfo = new();
        private readonly List<GenericBuyInfo> _buyInfo = [];

        public FullStockSB()
        {
            _sellInfo.Add(typeof(Bandage), 2);

            for (var i = 0; i < 250; i++)
            {
                _buyInfo.Add(new GenericBuyInfo(typeof(Bandage), 5, 20, 0xE21, 0));
            }
        }

        public override IShopSellInfo SellInfo => _sellInfo;

        public override List<GenericBuyInfo> BuyInfo => _buyInfo;
    }

    private sealed class FullStockVendor : TestVendor
    {
        protected override void AddSBInfos(List<SBInfo> sbInfos) => sbInfos.Add(new FullStockSB());
    }

    private BandageVendor NewVendor() => _harness.Track(new BandageVendor());

    private FullStockVendor NewFullStockVendor() => _harness.Track(new FullStockVendor());

    private Mobile NewSeller()
    {
        var seller = _harness.Track(new Mobile(World.NewMobile));
        seller.DefaultMobileInit();
        seller.AddItem(new Backpack());
        return seller;
    }

    // A fresh seller offers a stack of 10 bandages and sells <paramref name="amount"/> of them.
    private Bandage SellBandages(BaseVendor vendor, int amount, out Mobile seller)
    {
        seller = NewSeller();
        var bandages = new Bandage(10);
        seller.Backpack.DropItem(bandages);

        Assert.True(vendor.OnSellItems(seller, [new SellItemResponse(bandages, amount)]));
        return bandages;
    }

    private Bandage SellBandages(BaseVendor vendor, int amount) => SellBandages(vendor, amount, out _);

    [Theory]
    [InlineData(0.25, 1.0)] // restocked 15m ago: next boundary is the regular one
    [InlineData(5.0, 6.0)]  // exactly on a boundary: strictly after now
    [InlineData(5.5, 6.0)]  // lazy restock hasn't run for hours: stay on the grid, don't fire now
    public void NextBuybackPurge_IsTheNextRestockBoundaryAfterNow(double hoursSinceRestock, double expectedHours)
    {
        var lastRestock = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = lastRestock + TimeSpan.FromHours(hoursSinceRestock);

        var next = BaseVendor.GetNextBuybackPurge(lastRestock, TimeSpan.FromHours(1), now);

        Assert.Equal(lastRestock + TimeSpan.FromHours(expectedHours), next);
    }

    // Backwards clock adjustment: LastRestock ahead of now clamps to now.
    [Fact]
    public void NextBuybackPurge_FutureAnchor_IsClampedToNow()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastRestock = now + TimeSpan.FromHours(4);

        var next = BaseVendor.GetNextBuybackPurge(lastRestock, TimeSpan.FromHours(1), now);

        Assert.Equal(now + TimeSpan.FromHours(1), next);
    }

    [Fact]
    public void PartialStackSale_SplitGoesToBuyback_SellerKeepsRemainder()
    {
        var vendor = NewVendor();
        var bandages = SellBandages(vendor, 4, out var seller);

        Assert.Equal(6, bandages.Amount);
        Assert.Same(seller.Backpack, bandages.Parent);

        var buyback = vendor.BuyPack.Items;
        Assert.Single(buyback);
        Assert.IsType<Bandage>(buyback[0]);
        Assert.Equal(4, buyback[0].Amount);
        Assert.True(buyback[0].SkipSerialization);
        Assert.True(vendor.BuybackPurgeScheduled);
    }

    [Fact]
    public void Restock_PurgesBuyback()
    {
        var vendor = NewVendor();
        var bandages = SellBandages(vendor, 10);
        Assert.Single(vendor.BuyPack.Items);

        vendor.Restock();

        Assert.Empty(vendor.BuyPack.Items);
        Assert.True(bandages.Deleted);
    }

    [Fact]
    public void Restock_ClearsPurgeSchedule_AndASubsequentSaleRearmsIt()
    {
        var vendor = NewVendor();
        SellBandages(vendor, 10);
        Assert.True(vendor.BuybackPurgeScheduled);

        vendor.Restock();

        Assert.False(vendor.BuybackPurgeScheduled);

        SellBandages(vendor, 10);

        Assert.True(vendor.BuybackPurgeScheduled);
    }

    [Fact]
    public void Delete_CancelsBuybackPurgeTimer()
    {
        var vendor = NewVendor();
        SellBandages(vendor, 10);
        Assert.True(vendor.BuybackPurgeScheduled);

        vendor.Delete();

        Assert.False(vendor.BuybackPurgeScheduled);
    }

    [Fact]
    public void LegacyBuyPack_IsReplaced_AndItsContentsDeleted()
    {
        var vendor = NewVendor();
        vendor.FindItemOnLayer(Layer.ShopBuy).Delete();

        var legacy = new Backpack { Layer = Layer.ShopBuy, Movable = false, Visible = false };
        vendor.AddItem(legacy);

        // Several children, so a delete loop that skips entries as the list shrinks would show.
        var children = new Item[3];
        for (var i = 0; i < children.Length; i++)
        {
            children[i] = new Item(0x1234);
            legacy.DropItem(children[i]);
        }

        var pack = vendor.BuyPack;

        Assert.IsType<VendorBuybackPack>(pack);
        Assert.Same(pack, vendor.FindItemOnLayer(Layer.ShopBuy));
        Assert.True(legacy.Deleted);
        for (var i = 0; i < children.Length; i++)
        {
            Assert.True(children[i].Deleted);
        }
    }

    [Fact]
    public void SaleWithNoBuybackCapacity_ConsumesItem_AndStillPays()
    {
        var vendor = NewFullStockVendor();
        var bandages = SellBandages(vendor, 10, out var seller);

        Assert.True(bandages.Deleted);
        Assert.Empty(vendor.BuyPack.Items);
        Assert.False(vendor.BuybackPurgeScheduled);

        var gold = seller.Backpack.FindItemByType<Gold>();
        Assert.NotNull(gold);
        Assert.Equal(20, gold.Amount);
    }
}
