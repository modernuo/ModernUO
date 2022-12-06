using System;
using System.Collections.Generic;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.BulkOrders;
using Server.Factions;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.Mobiles
{
    public enum VendorShoeType
    {
        None,
        Shoes,
        Boots,
        Sandals,
        ThighBoots
    }

    public abstract class BaseVendor : BaseCreature, IVendor
    {
        private const int MaxSell = 500;

        private static readonly TimeSpan InventoryDecayTime = TimeSpan.FromHours(1.0);

        private readonly List<IBuyItemInfo> _buyInfo = new();
        private readonly List<IShopSellInfo> _sellInfo = new();

        private static bool EnableVendorBuyOPL;

        public static void Configure()
        {
            // Turn off to remove tooltips while buying items
            // CUO is not compatible with this turned off
            // Also items may require a string description for their name to show up properly.
            // See SBAnimalTrainer for an example
            EnableVendorBuyOPL = ServerConfiguration.GetSetting("opl.enableForVendorBuy", true);
        }

        public static void Initialize()
        {
            // This is technically more work than making timers, but we don't to deplete the timer pool immediately.
            foreach (var m in World.Mobiles.Values)
            {
                if (m is BaseVendor bv)
                {
                    bv.CheckMorph();
                }
            }
        }

        public BaseVendor(string title = null) : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            LoadSBInfo();
            Title = title;
            InitBody();
            InitOutfit();
            SetSpeed(0.5, 2.0);

            // these packs MUST exist, or the client will crash when the packets are sent
            Container pack = new Backpack { Layer = Layer.ShopBuy, Movable = false, Visible = false };
            AddItem(pack);

            pack = new Backpack { Layer = Layer.ShopResale, Movable = false, Visible = false };
            AddItem(pack);

            LastRestock = Core.Now;
        }

        public BaseVendor(Serial serial)
            : base(serial)
        {
        }

        protected abstract List<SBInfo> SBInfos { get; }

        public override bool CanTeach => true;

        public override bool BardImmune => true;

        public override bool PlayerRangeSensitive => true;

        public virtual bool IsActiveVendor => true;
        public virtual bool IsActiveBuyer => IsActiveVendor;  // response to vendor SELL
        public virtual bool IsActiveSeller => IsActiveVendor; // response to vendor BUY

        public virtual NpcGuild NpcGuild => NpcGuild.None;

        public override bool IsInvulnerable => true;

        public virtual DateTime NextTrickOrTreat { get; set; }

        public override bool ShowFameTitle => false;

        public Container BuyPack
        {
            get
            {
                if (FindItemOnLayer(Layer.ShopBuy) is not Container pack)
                {
                    pack = new Backpack { Layer = Layer.ShopBuy, Visible = false };
                    AddItem(pack);
                }

                return pack;
            }
        }

        public virtual bool IsTokunoVendor => Map == Map.Tokuno;

        public virtual VendorShoeType ShoeType => VendorShoeType.Shoes;

        public DateTime LastRestock { get; set; }

        public virtual TimeSpan RestockDelay => TimeSpan.FromHours(1);

        public virtual void Restock()
        {
            LastRestock = Core.Now;

            var buyInfo = GetBuyInfo();

            foreach (var bii in buyInfo)
            {
                bii.OnRestock();
            }
        }

        public virtual bool OnBuyItems(Mobile buyer, List<BuyItemResponse> list)
        {
            if (!IsActiveSeller)
            {
                return false;
            }

            if (!buyer.CheckAlive())
            {
                return false;
            }

            if (!CheckVendorAccess(buyer))
            {
                Say(501522); // I shall not treat with scum like thee!
                return false;
            }

            UpdateBuyInfo();

            var info = GetSellInfo();
            var totalCost = 0;
            var validBuy = new List<BuyItemResponse>(list.Count);
            var fromBank = false;
            var fullPurchase = true;
            var controlSlots = buyer.FollowersMax - buyer.Followers;

            foreach (var buy in list)
            {
                var ser = buy.Serial;
                var amount = buy.Amount;

                if (ser.IsItem)
                {
                    var item = World.FindItem(ser);

                    if (item == null)
                    {
                        continue;
                    }

                    var gbi = LookupDisplayObject(item);

                    if (gbi != null)
                    {
                        ProcessSinglePurchase(buy, gbi, validBuy, ref controlSlots, ref fullPurchase, ref totalCost);
                    }
                    else if (item != BuyPack && item.IsChildOf(BuyPack))
                    {
                        if (amount > item.Amount)
                        {
                            amount = item.Amount;
                        }

                        if (amount <= 0)
                        {
                            continue;
                        }

                        foreach (var ssi in info)
                        {
                            if (ssi.IsSellable(item))
                            {
                                if (ssi.IsResellable(item))
                                {
                                    totalCost += ssi.GetBuyPriceFor(item) * amount;
                                    validBuy.Add(buy);
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (ser.IsMobile)
                {
                    var mob = World.FindMobile(ser);

                    if (mob == null)
                    {
                        continue;
                    }

                    var gbi = LookupDisplayObject(mob);

                    if (gbi != null)
                    {
                        ProcessSinglePurchase(buy, gbi, validBuy, ref controlSlots, ref fullPurchase, ref totalCost);
                    }
                }
            } // foreach

            if (fullPurchase && validBuy.Count == 0)
            {
                SayTo(buyer, 500190); // Thou hast bought nothing!
            }
            else if (validBuy.Count == 0)
            {
                SayTo(buyer, 500187); // Your order cannot be fulfilled, please try again.
            }

            if (validBuy.Count == 0)
            {
                return false;
            }

            var bought = buyer.AccessLevel >= AccessLevel.GameMaster;

            var cont = buyer.Backpack;
            if (!bought && cont != null)
            {
                if (cont.ConsumeTotal(typeof(Gold), totalCost))
                {
                    bought = true;
                }
                else if (totalCost < 2000)
                {
                    SayTo(buyer, 500192); // Begging thy pardon, but thou canst not afford that.
                }
            }

            if (!bought && totalCost >= 2000)
            {
                if (Banker.Withdraw(buyer, totalCost))
                {
                    bought = true;
                    fromBank = true;
                }
                else
                {
                    SayTo(buyer, 500191); // Begging thy pardon, but thy bank account lacks these funds.
                }
            }

            if (!bought)
            {
                return false;
            }

            buyer.PlaySound(0x32);

            cont = buyer.Backpack ?? buyer.BankBox;

            foreach (var buy in validBuy)
            {
                var ser = buy.Serial;
                var amount = buy.Amount;

                if (amount < 1)
                {
                    continue;
                }

                if (ser.IsItem)
                {
                    var item = World.FindItem(ser);

                    if (item == null)
                    {
                        continue;
                    }

                    var gbi = LookupDisplayObject(item);

                    if (gbi != null)
                    {
                        ProcessValidPurchase(amount, gbi, buyer, cont);
                    }
                    else
                    {
                        if (amount > item.Amount)
                        {
                            amount = item.Amount;
                        }

                        foreach (var ssi in info)
                        {
                            if (ssi.IsSellable(item))
                            {
                                if (ssi.IsResellable(item))
                                {
                                    Item buyItem;
                                    if (amount >= item.Amount)
                                    {
                                        buyItem = item;
                                    }
                                    else
                                    {
                                        buyItem = LiftItemDupe(item, item.Amount - amount) ?? item;
                                    }

                                    if (cont?.TryDropItem(buyer, buyItem, false) != true)
                                    {
                                        buyItem.MoveToWorld(buyer.Location, buyer.Map);
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
                else if (ser.IsMobile)
                {
                    var mob = World.FindMobile(ser);

                    if (mob == null)
                    {
                        continue;
                    }

                    var gbi = LookupDisplayObject(mob);

                    if (gbi != null)
                    {
                        ProcessValidPurchase(amount, gbi, buyer, cont);
                    }
                }
            } // foreach

            if (fullPurchase)
            {
                if (buyer.AccessLevel >= AccessLevel.GameMaster)
                {
                    SayTo(buyer, true, "I would not presume to charge thee anything.  Here are the goods you requested.");
                }
                else if (fromBank)
                {
                    SayTo(
                        buyer,
                        1151638, // The total of your purchase is ~1_val~ gold, which has been drawn from your bank account.  My thanks for the patronage.
                        totalCost.ToString()
                    );
                }
                else
                {
                    SayTo(
                        buyer,
                        1151639, // The total of your purchase is ~1_val~ gold.  My thanks for the patronage.
                        totalCost.ToString()
                    );
                }
            }
            else
            {
                if (buyer.AccessLevel >= AccessLevel.GameMaster)
                {
                    SayTo(
                        buyer,
                        true,
                        "I would not presume to charge thee anything.  Unfortunately, I could not sell you all the goods you requested."
                    );
                }
                else if (fromBank)
                {
                    SayTo(
                        buyer,
                        true,
                        $"The total of thy purchase is {totalCost} gold, which has been withdrawn from your bank account.  My thanks for the patronage.  Unfortunately, I could not sell you all the goods you requested."
                    );
                }
                else
                {
                    SayTo(
                        buyer,
                        true,
                        $"The total of thy purchase is {totalCost} gold.  My thanks for the patronage.  Unfortunately, I could not sell you all the goods you requested."
                    );
                }
            }

            return true;
        }

        public virtual bool OnSellItems(Mobile seller, List<SellItemResponse> list)
        {
            if (!IsActiveBuyer)
            {
                return false;
            }

            if (!seller.CheckAlive())
            {
                return false;
            }

            if (!CheckVendorAccess(seller))
            {
                Say(501522); // I shall not treat with scum like thee!
                return false;
            }

            seller.PlaySound(0x32);

            var info = GetSellInfo();
            var buyInfo = GetBuyInfo();
            var GiveGold = 0;
            var Sold = 0;

            foreach (var resp in list)
            {
                if (resp.Item.RootParent != seller || resp.Amount <= 0 || !resp.Item.IsStandardLoot() ||
                    !resp.Item.Movable || resp.Item is Container container && container.Items.Count != 0)
                {
                    continue;
                }

                foreach (var ssi in info)
                {
                    if (ssi.IsSellable(resp.Item))
                    {
                        Sold++;
                        break;
                    }
                }
            }

            if (Sold > MaxSell)
            {
                SayTo(seller, true, $"You may only sell {MaxSell} items at a time!");
                return false;
            }

            if (Sold == 0)
            {
                return true;
            }

            foreach (var resp in list)
            {
                if (resp.Item.RootParent != seller || resp.Amount <= 0 || !resp.Item.IsStandardLoot() ||
                    !resp.Item.Movable || resp.Item is Container container && container.Items.Count != 0)
                {
                    continue;
                }

                foreach (var ssi in info)
                {
                    if (!ssi.IsSellable(resp.Item))
                    {
                        continue;
                    }

                    var amount = resp.Amount;

                    if (amount > resp.Item.Amount)
                    {
                        amount = resp.Item.Amount;
                    }

                    if (ssi.IsResellable(resp.Item))
                    {
                        var found = false;

                        foreach (var bii in buyInfo)
                        {
                            if (bii.Restock(resp.Item, amount))
                            {
                                resp.Item.Consume(amount);
                                found = true;

                                break;
                            }
                        }

                        if (!found)
                        {
                            var cont = BuyPack;

                            if (amount < resp.Item.Amount)
                            {
                                var item = LiftItemDupe(resp.Item, resp.Item.Amount - amount);

                                if (item != null)
                                {
                                    item.SetLastMoved();
                                    cont.DropItem(item);
                                }
                                else
                                {
                                    resp.Item.SetLastMoved();
                                    cont.DropItem(resp.Item);
                                }
                            }
                            else
                            {
                                resp.Item.SetLastMoved();
                                cont.DropItem(resp.Item);
                            }
                        }
                    }
                    else
                    {
                        if (amount < resp.Item.Amount)
                        {
                            resp.Item.Amount -= amount;
                        }
                        else
                        {
                            resp.Item.Delete();
                        }
                    }

                    GiveGold += ssi.GetSellPriceFor(resp.Item) * amount;
                    break;
                }
            }

            if (GiveGold > 0)
            {
                while (GiveGold > 60000)
                {
                    seller.AddToBackpack(new Gold(60000));
                    GiveGold -= 60000;
                }

                seller.AddToBackpack(new Gold(GiveGold));

                seller.PlaySound(0x0037); // Gold dropping sound

                if (SupportsBulkOrders(seller))
                {
                    var bulkOrder = CreateBulkOrder(seller, false);

                    if (bulkOrder is LargeBOD largeBod)
                    {
                        seller.SendGump(new LargeBODAcceptGump(seller, largeBod));
                    }
                    else if (bulkOrder is SmallBOD smallBod)
                    {
                        seller.SendGump(new SmallBODAcceptGump(seller, smallBod));
                    }
                }
            }
            // no cliloc for this?
            // SayTo( seller, true, "Thank you! I bought {0} item{1}. Here is your {2}gp.", Sold, (Sold > 1 ? "s" : ""), GiveGold );

            return true;
        }

        public virtual bool IsValidBulkOrder(Item item) => false;

        public virtual Item CreateBulkOrder(Mobile from, bool fromContextMenu) => null;

        public virtual bool SupportsBulkOrders(Mobile from) => false;

        public virtual TimeSpan GetNextBulkOrder(Mobile from) => TimeSpan.Zero;

        public virtual void OnSuccessfulBulkOrderReceive(Mobile from)
        {
        }

        public abstract void InitSBInfo();

        protected void LoadSBInfo()
        {
            LastRestock = Core.Now;

            for (var i = 0; i < _buyInfo.Count; ++i)
            {
                if (_buyInfo[i] is GenericBuyInfo buy)
                {
                    buy.DeleteDisplayEntity();
                }
            }

            SBInfos.Clear();

            InitSBInfo();

            _buyInfo.Clear();
            _sellInfo.Clear();

            for (var i = 0; i < SBInfos.Count; i++)
            {
                var sbInfo = SBInfos[i];
                _buyInfo.AddRange(sbInfo.BuyInfo);
                _sellInfo.Add(sbInfo.SellInfo);
            }
        }

        public virtual bool GetGender() => Utility.RandomBool();

        public virtual void InitBody()
        {
            InitStats(100, 100, 25);

            SpeechHue = Utility.RandomDyedHue();
            Hue = Race.Human.RandomSkinHue();

            if (Female = GetGender())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }

        public virtual int GetRandomHue()
        {
            return Utility.Random(5) switch
            {
                0 => Utility.RandomBlueHue(),
                1 => Utility.RandomGreenHue(),
                2 => Utility.RandomRedHue(),
                3 => Utility.RandomYellowHue(),
                _ => Utility.RandomNeutralHue() // 4
            };
        }

        public virtual int GetShoeHue() => Utility.RandomDouble() < 0.1 ? 0 : Utility.RandomNeutralHue();

        public virtual void CheckMorph()
        {
            if (CheckGargoyle())
            {
                return;
            }

            if (CheckNecromancer())
            {
                return;
            }

            CheckTokuno();
        }

        public virtual bool CheckTokuno()
        {
            if (Map != Map.Tokuno)
            {
                return false;
            }

            var n = NameList.GetNameList(Female ? "tokuno female" : "tokuno male");

            if (!n.ContainsName(Name))
            {
                TurnToTokuno();
            }

            return true;
        }

        public virtual void TurnToTokuno()
        {
            Name = NameList.RandomName(Female ? "tokuno female" : "tokuno male");
        }

        public virtual bool CheckGargoyle()
        {
            var map = Map;

            if (map != Map.Ilshenar)
            {
                return false;
            }

            if (!Region.IsPartOf("Gargoyle City"))
            {
                return false;
            }

            if (Body != 0x2F6 || (Hue & 0x8000) == 0)
            {
                TurnToGargoyle();
            }

            return true;
        }

        public virtual bool CheckNecromancer()
        {
            var map = Map;

            if (map != Map.Malas)
            {
                return false;
            }

            if (!Region.IsPartOf("Umbra"))
            {
                return false;
            }

            if (Hue != 0x83E8)
            {
                TurnToNecromancer();
            }

            return true;
        }

        public override void OnAfterSpawn()
        {
            CheckMorph();
        }

        protected override void OnMapChange(Map oldMap)
        {
            base.OnMapChange(oldMap);

            CheckMorph();

            LoadSBInfo();
        }

        public virtual int GetRandomNecromancerHue()
        {
            return Utility.Random(20) switch
            {
                0 => 0,
                1 => 0x4E9,
                _ => Utility.RandomList(0x485, 0x497)
            };
        }

        public virtual void TurnToNecromancer()
        {
            for (var i = 0; i < Items.Count; ++i)
            {
                var item = Items[i];
                if (item is BaseClothing or BaseWeapon or BaseArmor or BaseTool)
                {
                    item.Hue = GetRandomNecromancerHue();
                }
            }

            HairHue = 0;
            FacialHairHue = 0;

            Hue = 0x83E8;
        }

        public virtual void TurnToGargoyle()
        {
            for (var i = 0; i < Items.Count; ++i)
            {
                var item = Items[i];

                if (item is BaseClothing)
                {
                    item.Delete();
                }
            }

            HairItemID = 0;
            FacialHairItemID = 0;

            Body = 0x2F6;
            Hue = Utility.RandomBrightHue() | 0x8000;
            Name = NameList.RandomName("gargoyle vendor");

            CapitalizeTitle();
        }

        public virtual void CapitalizeTitle()
        {
            Title = Title.Capitalize();
        }

        public virtual int GetHairHue() => Race.RandomHairHue();

        public virtual void InitOutfit()
        {
            AddItem(
                Utility.Random(3) switch
                {
                    0 => new FancyShirt(GetRandomHue()),
                    1 => new Doublet(GetRandomHue()),
                    _ => new Shirt(GetRandomHue()) // 2
                }
            );

            AddItem(
                ShoeType switch
                {
                    VendorShoeType.Shoes   => new Shoes(GetShoeHue()),
                    VendorShoeType.Boots   => new Boots(GetShoeHue()),
                    VendorShoeType.Sandals => new Sandals(GetShoeHue()),
                    _                      => new ThighBoots(GetShoeHue()) // ThighBoots
                }
            );

            var hairHue = GetHairHue();

            Utility.AssignRandomHair(this, hairHue);
            Utility.AssignRandomFacialHair(this, hairHue);

            if (Female)
            {
                AddItem(
                    Utility.Random(6) switch
                    {
                        0 => new ShortPants(GetRandomHue()),
                        1 => new Kilt(GetRandomHue()),
                        2 => new Kilt(GetRandomHue()),
                        _ => new Skirt(GetRandomHue()) // 3-5
                    }
                );
            }
            else
            {
                AddItem(Utility.RandomBool() ? new LongPants(GetRandomHue()) : new ShortPants(GetRandomHue()));
            }

            PackGold(100, 200);
        }

        private static readonly Serial _COFFEE = (Serial)0x7FC0FFEE;

        public virtual void VendorBuy(Mobile from)
        {
            if (!IsActiveSeller)
            {
                return;
            }

            if (!from.CheckAlive())
            {
                return;
            }

            if (!CheckVendorAccess(from))
            {
                Say(501522); // I shall not treat with scum like thee!
                return;
            }

            if (Core.Now - LastRestock > RestockDelay)
            {
                Restock();
            }

            UpdateBuyInfo();

            var buyInfo = GetBuyInfo();
            var sellInfo = GetSellInfo();

            var list = new List<BuyItemState>(buyInfo.Length);
            var cont = BuyPack;

            using var opls = PooledRefQueue<ObjectPropertyList>.Create(EnableVendorBuyOPL ? buyInfo.Length : 0);

            for (var idx = 0; idx < buyInfo.Length; idx++)
            {
                var buyItem = buyInfo[idx];

                if (buyItem.Amount <= 0 || list.Count >= 250)
                {
                    continue;
                }

                if (buyItem is not GenericBuyInfo gbi)
                {
                    return;
                }

                var disp = gbi.GetDisplayEntity();

                list.Add(
                    new BuyItemState(
                        buyItem.Name,
                        cont.Serial,
                        disp?.Serial ?? _COFFEE,
                        buyItem.Price,
                        buyItem.Amount,
                        buyItem.ItemID,
                        buyItem.Hue
                    )
                );

                if (disp is IObjectPropertyListEntity obj)
                {
                    opls.Enqueue(obj.PropertyList);
                }
            }

            var playerItems = cont.Items;

            for (var i = playerItems.Count - 1; i >= 0; --i)
            {
                if (i >= playerItems.Count)
                {
                    continue;
                }

                var item = playerItems[i];

                if (item.LastMoved + InventoryDecayTime <= Core.Now)
                {
                    item.Delete();
                }
            }

            for (var i = 0; i < playerItems.Count; ++i)
            {
                var item = playerItems[i];

                var price = 0;
                string name = null;

                foreach (var ssi in sellInfo)
                {
                    if (ssi.IsSellable(item))
                    {
                        price = ssi.GetBuyPriceFor(item);
                        name = ssi.GetNameFor(item);
                        break;
                    }
                }

                if (name != null && list.Count < 250)
                {
                    list.Add(new BuyItemState(name, cont.Serial, item.Serial, price, item.Amount, item.ItemID, item.Hue));
                    opls.Enqueue(item.PropertyList);
                }
            }

            // one (not all) of the packets uses a byte to describe number of items in the list.  Osi = dumb.
            // if (list.Count > 255)
            // Console.WriteLine( "Vendor Warning: Vendor {0} has more than 255 buy items, may cause client errors!", this );

            if (list.Count <= 0)
            {
                return;
            }

            list.Sort(new BuyItemStateComparer());

            SendPacksTo(from);

            var ns = from.NetState;

            if (ns.CannotSendPackets())
            {
                return;
            }

            from.NetState.SendVendorBuyContent(list);
            from.NetState.SendVendorBuyList(this, list);
            from.NetState.SendDisplayBuyList(Serial);
            from.NetState.SendMobileStatus(from); // make sure their gold amount is sent

            while (opls.Count > 0)
            {
                from.NetState?.Send(opls.Dequeue().Buffer);
            }

            SayTo(from, 500186); // Greetings.  Have a look around.
        }

        public virtual void SendPacksTo(Mobile from)
        {
            var pack = FindItemOnLayer(Layer.ShopBuy);

            if (pack == null)
            {
                pack = new Backpack { Layer = Layer.ShopBuy, Movable = false, Visible = false };
                AddItem(pack);
            }

            from.NetState.SendEquipUpdate(pack);

            pack = FindItemOnLayer(Layer.ShopSell);

            if (pack != null)
            {
                from.NetState.SendEquipUpdate(pack);
            }

            pack = FindItemOnLayer(Layer.ShopResale);

            if (pack == null)
            {
                pack = new Backpack { Layer = Layer.ShopResale, Movable = false, Visible = false };
                AddItem(pack);
            }

            from.NetState.SendEquipUpdate(pack);
        }

        public virtual void VendorSell(Mobile from)
        {
            if (!IsActiveBuyer)
            {
                return;
            }

            if (!from.CheckAlive())
            {
                return;
            }

            if (!CheckVendorAccess(from))
            {
                Say(501522); // I shall not treat with scum like thee!
                return;
            }

            var pack = from.Backpack;

            if (pack == null)
            {
                return;
            }

            var info = GetSellInfo();

            var set = new HashSet<SellItemState>(new SellItemStateComparer());

            foreach (var ssi in info)
            {
                foreach (var item in pack.FindItemsByType(ssi.Types))
                {
                    if (item is Container container && container.Items.Count != 0)
                    {
                        continue;
                    }

                    if (item.IsStandardLoot() && item.Movable && ssi.IsSellable(item))
                    {
                        set.Add(new SellItemState(item, ssi.GetSellPriceFor(item), ssi.GetNameFor(item)));
                    }
                }
            }

            if (set.Count > 0)
            {
                SendPacksTo(from);

                from.NetState.SendVendorSellList(Serial, set);
            }
            else
            {
                Say(true, "You have nothing I would be interested in.");
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            /* TODO: Thou art giving me? and fame/karma for gold gifts */

            var smallBod = dropped as SmallBOD;
            var largeBod = dropped as LargeBOD;

            if (!(smallBod != null || largeBod != null))
            {
                return base.OnDragDrop(from, dropped);
            }

            var pm = from as PlayerMobile;

            if (Core.ML && pm?.NextBODTurnInTime > Core.Now)
            {
                SayTo(from, 1079976); // You'll have to wait a few seconds while I inspect the last order.
                return false;
            }

            if (!IsValidBulkOrder(dropped))
            {
                SayTo(from, 1045130); // That order is for some other shopkeeper.
                return false;
            }

            if (smallBod?.Complete == false || largeBod?.Complete == false)
            {
                SayTo(from, 1045131); // You have not completed the order yet.
                return false;
            }

            Item reward;
            int gold, fame;

            if (smallBod != null)
            {
                smallBod.GetRewards(out reward, out gold, out fame);
            }
            else
            {
                largeBod.GetRewards(out reward, out gold, out fame);
            }

            from.SendSound(0x3D);

            SayTo(from, 1045132); // Thank you so much!  Here is a reward for your effort.

            if (reward != null)
            {
                from.AddToBackpack(reward);
            }

            if (gold > 1000)
            {
                from.AddToBackpack(new BankCheck(gold));
            }
            else if (gold > 0)
            {
                from.AddToBackpack(new Gold(gold));
            }

            Titles.AwardFame(from, fame, true);

            OnSuccessfulBulkOrderReceive(from);

            if (Core.ML && pm != null)
            {
                pm.NextBODTurnInTime = Core.Now + TimeSpan.FromSeconds(10.0);
            }

            dropped.Delete();
            return true;
        }

        private GenericBuyInfo LookupDisplayObject(object obj)
        {
            var buyInfo = GetBuyInfo();

            for (var i = 0; i < buyInfo.Length; ++i)
            {
                if (buyInfo[i] is GenericBuyInfo gbi && gbi.GetDisplayEntity() == obj)
                {
                    return gbi;
                }
            }

            return null;
        }

        private void ProcessSinglePurchase(
            BuyItemResponse buy, IBuyItemInfo bii, List<BuyItemResponse> validBuy,
            ref int controlSlots, ref bool fullPurchase, ref int totalCost
        )
        {
            var amount = buy.Amount;

            if (amount > bii.Amount)
            {
                amount = bii.Amount;
            }

            if (amount <= 0)
            {
                return;
            }

            var slots = bii.ControlSlots * amount;

            if (controlSlots >= slots)
            {
                controlSlots -= slots;
            }
            else
            {
                fullPurchase = false;
                return;
            }

            totalCost += bii.Price * amount;
            validBuy.Add(buy);
        }

        private void ProcessValidPurchase(int amount, IBuyItemInfo bii, Mobile buyer, Container cont)
        {
            if (amount > bii.Amount)
            {
                amount = bii.Amount;
            }

            if (amount < 1)
            {
                return;
            }

            bii.Amount -= amount;

            var o = bii.GetEntity();

            if (o is Item item)
            {
                if (item.Stackable)
                {
                    item.Amount = amount;

                    if (cont?.TryDropItem(buyer, item, false) != true)
                    {
                        item.MoveToWorld(buyer.Location, buyer.Map);
                    }
                }
                else
                {
                    item.Amount = 1;

                    if (cont?.TryDropItem(buyer, item, false) != true)
                    {
                        item.MoveToWorld(buyer.Location, buyer.Map);
                    }

                    for (var i = 1; i < amount; i++)
                    {
                        if (bii.GetEntity() is Item newItem)
                        {
                            newItem.Amount = 1;

                            if (cont?.TryDropItem(buyer, newItem, false) != true)
                            {
                                newItem.MoveToWorld(buyer.Location, buyer.Map);
                            }
                        }
                    }
                }
            }
            else if (o is Mobile m)
            {
                m.Direction = (Direction)Utility.Random(8);
                m.MoveToWorld(buyer.Location, buyer.Map);
                m.PlaySound(m.GetIdleSound());

                if (m is BaseCreature bc)
                {
                    bc.SetControlMaster(buyer);
                    bc.ControlOrder = OrderType.Stop;
                }

                for (var i = 1; i < amount; ++i)
                {
                    if (bii.GetEntity() is Mobile newMobile)
                    {
                        newMobile.Direction = (Direction)Utility.Random(8);
                        newMobile.MoveToWorld(buyer.Location, buyer.Map);

                        if (newMobile is BaseCreature newBc)
                        {
                            newBc.SetControlMaster(buyer);
                            newBc.ControlOrder = OrderType.Stop;
                        }
                    }
                }
            }
        }

        public virtual bool CheckVendorAccess(Mobile from) =>
            Region.GetRegion<GuardedRegion>()?.CheckVendorAccess(this, from) != false ||
            Region != from.Region && from.Region.GetRegion<GuardedRegion>()?.CheckVendorAccess(this, from) != false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            var sbInfos = SBInfos;

            for (var i = 0; i < sbInfos?.Count; ++i)
            {
                var sbInfo = sbInfos[i];
                var buyInfo = sbInfo.BuyInfo;

                for (var j = 0; j < buyInfo?.Count; ++j)
                {
                    var gbi = buyInfo[j];

                    var maxAmount = gbi.MaxAmount;

                    var doubled = maxAmount switch
                    {
                        40  => 1,
                        80  => 2,
                        160 => 3,
                        320 => 4,
                        640 => 5,
                        999 => 6,
                        _   => 0
                    };

                    if (doubled > 0)
                    {
                        writer.WriteEncodedInt(1 + j * sbInfos.Count + i);
                        writer.WriteEncodedInt(doubled);
                    }
                }
            }

            writer.WriteEncodedInt(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            LoadSBInfo();

            var sbInfos = SBInfos;

            switch (version)
            {
                case 1:
                    {
                        int index;

                        while ((index = reader.ReadEncodedInt()) > 0)
                        {
                            var doubled = reader.ReadEncodedInt();

                            if (sbInfos != null)
                            {
                                index -= 1;
                                var sbInfoIndex = index % sbInfos.Count;
                                var buyInfoIndex = index / sbInfos.Count;

                                if (sbInfoIndex >= 0 && sbInfoIndex < sbInfos.Count)
                                {
                                    var sbInfo = sbInfos[sbInfoIndex];
                                    var buyInfo = sbInfo.BuyInfo;

                                    if (buyInfo != null && buyInfoIndex >= 0 && buyInfoIndex < buyInfo.Count)
                                    {
                                        var gbi = buyInfo[buyInfoIndex];

                                        var amount = doubled switch
                                        {
                                            1 => 40,
                                            2 => 80,
                                            3 => 160,
                                            4 => 320,
                                            5 => 640,
                                            6 => 999,
                                            _ => 20
                                        };

                                        gbi.Amount = gbi.MaxAmount = amount;
                                    }
                                }
                            }
                        }

                        break;
                    }
            }

            if (IsParagon)
            {
                IsParagon = false;
            }
        }

        public override void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (from.Alive && IsActiveVendor)
            {
                if (SupportsBulkOrders(from))
                {
                    list.Add(new BulkOrderInfoEntry(from, this));
                }

                if (IsActiveSeller)
                {
                    list.Add(new VendorBuyEntry(from, this));
                }

                if (IsActiveBuyer)
                {
                    list.Add(new VendorSellEntry(from, this));
                }
            }

            base.AddCustomContextEntries(from, list);
        }

        public virtual IShopSellInfo[] GetSellInfo() => _sellInfo.ToArray();

        public virtual IBuyItemInfo[] GetBuyInfo() => _buyInfo.ToArray();

        public virtual int GetPriceScalar() => 100 + Town.FromRegion(Region)?.Tax ?? 0;

        public void UpdateBuyInfo()
        {
            var priceScalar = GetPriceScalar();

            foreach (var info in _buyInfo.ToArray())
            {
                info.PriceScalar = priceScalar;
            }
        }

        private class BulkOrderInfoEntry : ContextMenuEntry
        {
            private readonly Mobile m_From;
            private readonly BaseVendor m_Vendor;

            public BulkOrderInfoEntry(Mobile from, BaseVendor vendor)
                : base(6152)
            {
                m_From = from;
                m_Vendor = vendor;
            }

            public override void OnClick()
            {
                if (m_Vendor.SupportsBulkOrders(m_From))
                {
                    var ts = m_Vendor.GetNextBulkOrder(m_From);

                    var totalSeconds = (int)ts.TotalSeconds;
                    var totalHours = (totalSeconds + 3599) / 3600;
                    var totalMinutes = (totalSeconds + 59) / 60;

                    if (Core.SE ? totalMinutes == 0 : totalHours == 0)
                    {
                        m_From.SendLocalizedMessage(1049038); // You can get an order now.

                        if (Core.AOS)
                        {
                            var bulkOrder = m_Vendor.CreateBulkOrder(m_From, true);

                            if (bulkOrder is LargeBOD bod)
                            {
                                m_From.SendGump(new LargeBODAcceptGump(m_From, bod));
                            }
                            else if (bulkOrder is SmallBOD smallBod)
                            {
                                m_From.SendGump(new SmallBODAcceptGump(m_From, smallBod));
                            }
                        }
                    }
                    else
                    {
                        var oldSpeechHue = m_Vendor.SpeechHue;
                        m_Vendor.SpeechHue = 0x3B2;

                        if (Core.SE)
                        {
                            m_Vendor.SayTo(
                                m_From,
                                1072058,
                                totalMinutes.ToString()
                            ); // An offer may be available in about ~1_minutes~ minutes.
                        }
                        else
                        {
                            m_Vendor.SayTo(
                                m_From,
                                1049039,
                                totalHours.ToString()
                            ); // An offer may be available in about ~1_hours~ hours.
                        }

                        m_Vendor.SpeechHue = oldSpeechHue;
                    }
                }
            }
        }
    }
}

namespace Server.ContextMenus
{
    public class VendorBuyEntry : ContextMenuEntry
    {
        private readonly BaseVendor m_Vendor;

        public VendorBuyEntry(Mobile from, BaseVendor vendor)
            : base(6103, 8)
        {
            m_Vendor = vendor;
            Enabled = vendor.CheckVendorAccess(from);
        }

        public override void OnClick()
        {
            m_Vendor.VendorBuy(Owner.From);
        }
    }

    public class VendorSellEntry : ContextMenuEntry
    {
        private readonly BaseVendor m_Vendor;

        public VendorSellEntry(Mobile from, BaseVendor vendor)
            : base(6104, 8)
        {
            m_Vendor = vendor;
            Enabled = vendor.CheckVendorAccess(from);
        }

        public override void OnClick()
        {
            m_Vendor.VendorSell(Owner.From);
        }
    }
}

namespace Server
{
    public interface IShopSellInfo
    {
        // What do we sell?
        Type[] Types { get; }

        // get display name for an item
        string GetNameFor(Item item);

        // get price for an item which the player is selling
        int GetSellPriceFor(Item item);

        // get price for an item which the player is buying
        int GetBuyPriceFor(Item item);

        // can we sell this item to this vendor?
        bool IsSellable(Item item);

        // does the vendor resell this item?
        bool IsResellable(Item item);
    }

    public interface IBuyItemInfo
    {
        int ControlSlots { get; }

        int PriceScalar { get; set; }

        // display price of the item
        int Price { get; }

        // display name of the item
        string Name { get; }

        // display hue
        int Hue { get; }

        // display id
        int ItemID { get; }

        // amount in stock
        int Amount { get; set; }

        // max amount in stock
        int MaxAmount { get; }

        // get a new instance of an object (we just bought it)
        IEntity GetEntity();

        // Attempt to restock with item, (return true if restock successful)
        bool Restock(Item item, int amount);

        // called when its time for the whole shop to restock
        void OnRestock();
    }
}
