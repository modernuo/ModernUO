using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.BulkOrders;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Multis;
using Server.Prompts;
using Server.Systems.FeatureFlags;
using Server.Targeting;

namespace Server.Mobiles;

[AttributeUsage(AttributeTargets.Class)]
public class PlayerVendorTargetAttribute : Attribute;

/**
 * Note: When upgrading player vendors to the new system (Expansion enter AOS+), bump the serialization version up by 1.
 * Next, uncomment the MigrateFrom function and change the `V3Content` type to match the serialization version
 * before it was bumped. Then run publish.cmd to generate the migration file.
 */
[SerializationGenerator(3, false)]
public partial class PlayerVendor : Mobile
{
    private Timer _payTimer;

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _shopName;

    [DeltaDateTime]
    [SerializableField(1, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextPayTime;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _bankAccount;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _holdGold;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Dictionary<Item, VendorItem> _sellItems;

    public PlayerVendor(Mobile owner, BaseHouse house)
    {
        Owner = owner;
        House = house;

        if (BaseHouse.NewVendorSystem)
        {
            BankAccount = 0;
            HoldGold = 4;
        }
        else
        {
            BankAccount = 1000;
            HoldGold = 0;
        }

        ShopName = "Shop Not Yet Named";

        _sellItems = new Dictionary<Item, VendorItem>();

        CantWalk = true;

        if (!Core.AOS)
        {
            NameHue = 0x35;
        }

        InitStats(100, 100, 25);
        InitBody();
        InitOutfit();

        var delay = PayTimer.GetInterval();

        _payTimer = new PayTimer(this, delay);
        _payTimer.Start();

        NextPayTime = Core.Now + delay;
    }

    public PlayerVendorPlaceholder Placeholder { get; set; }

    [SerializableProperty(2)]
    public BaseHouse House
    {
        get => _house;
        set
        {
            _house?.PlayerVendors.Remove(this);
            value?.PlayerVendors.Add(this);

            _house = value;
            this.MarkDirty();
        }
    }

    public int ChargePerDay
    {
        get
        {
            if (BaseHouse.NewVendorSystem)
            {
                return ChargePerRealWorldDay / 12;
            }

            long total = 0;
            foreach (var value in _sellItems.Values)
            {
                total += value.Price;
            }

            return (int)(20 + Math.Max(total - 500, 0) / 500);
        }
    }

    public int ChargePerRealWorldDay
    {
        get
        {
            if (BaseHouse.NewVendorSystem)
            {
                long total = 0;
                foreach (var value in _sellItems.Values)
                {
                    total += value.Price;
                }

                return (int)(60 + total / 500 * 3);
            }

            return ChargePerDay * 12;
        }
    }

    /*
     // Uncomment this to migrate to the new vendor system.
     // Make sure to update V3Content to match the serialization version before the bump.
     private void MigrateFrom(V3Content content)
     {
         MigrateToNewVendorSystem();
     }
     */

    private void MigrateToNewVendorSystem()
    {
        if (BaseHouse.NewVendorSystem)
        {
            Timer.StartTimer(FixDresswear);

            NextPayTime = Core.Now + PayTimer.GetInterval();
            HoldGold += BankAccount;
            BankAccount = 0;
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        reader.ReadBool(); // New vendor system?
        _shopName = reader.ReadString();
        _nextPayTime = reader.ReadDeltaTime();
        House = reader.ReadEntity<BaseHouse>();
        _owner = reader.ReadEntity<Mobile>();
        _bankAccount = reader.ReadInt();
        _holdGold = reader.ReadInt();
        var count = reader.ReadInt();

        _sellItems = new Dictionary<Item, VendorItem>(count);
        for (var i = 0; i < count; i++)
        {
            var item = reader.ReadEntity<Item>();
            var vi = new VendorItem();
            vi.Deserialize(reader);
            _sellItems[item] = vi;
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        var delay = _nextPayTime - Core.Now;

        _payTimer = new PayTimer(this, delay > TimeSpan.Zero ? delay : TimeSpan.Zero);
        _payTimer.Start();

        Blessed = false;

        if (Core.AOS && NameHue == 0x35)
        {
            NameHue = -1;
        }

        // Do we have a vendor that may have been orphaned? Let's try to recover them and attach them to their house.
        if (_house == null)
        {
            if (_owner == null)
            {
                Timer.DelayCall(Delete); // Don't try to dismiss, no owner.
            }
            Timer.DelayCall(() =>
                {
                    var house = BaseHouse.FindHouseAt(this);

                    if (house != null)
                    {
                        House = house;
                    }
                    else if (_owner.AccessLevel == AccessLevel.Player)
                    {
                        // If we can't find a house, dismiss the vendor.
                        Dismiss(_owner);
                    }
                }
            );
        }
        else
        {
            _house.PlayerVendors.Add(this);
        }
    }

    public void InitBody()
    {
        Hue = Race.Human.RandomSkinHue();
        SpeechHue = 0x3B2;

        if (!Core.AOS)
        {
            NameHue = 0x35;
        }

        if (Female = Utility.RandomBool())
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

    public virtual void InitOutfit()
    {
        AddItem(new FancyShirt(Utility.RandomNeutralHue())
        {
            Layer = Layer.InnerTorso
        });
        AddItem(new LongPants(Utility.RandomNeutralHue()));
        AddItem(new BodySash(Utility.RandomNeutralHue()));
        AddItem(new Boots(Utility.RandomNeutralHue()));
        AddItem(new Cloak(Utility.RandomNeutralHue()));

        Utility.AssignRandomHair(this);

        Container pack = new VendorBackpack();
        pack.Movable = false;
        AddItem(pack);
    }

    public virtual bool IsOwner(Mobile m)
    {
        if (m.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        if (BaseHouse.NewVendorSystem && House != null)
        {
            return House.IsOwner(m);
        }

        return m == Owner;
    }

    protected List<Item> GetItems()
    {
        var list = new List<Item>();

        foreach (var item in Items)
        {
            if (item.Movable && item != Backpack && item.Layer != Layer.Hair && item.Layer != Layer.FacialHair)
            {
                list.Add(item);
            }
        }

        if (Backpack != null)
        {
            list.AddRange(Backpack.Items);
        }

        return list;
    }

    public virtual void Destroy(bool toBackpack)
    {
        Return();

        if (!BaseHouse.NewVendorSystem)
        {
            FixDresswear();
        }

        /* Possible cases regarding item return:
         *
         * 1. No item must be returned
         *       -> do nothing.
         * 2. ( toBackpack is false OR the vendor is in the internal map ) AND the vendor is associated with a AOS house
         *       -> put the items into the moving crate or a vendor inventory,
         *          depending on whether the vendor owner is also the house owner.
         * 3. ( toBackpack is true OR the vendor isn't associated with any AOS house ) AND the vendor isn't in the internal map
         *       -> put the items into a backpack.
         * 4. The vendor isn't associated with any house AND it's in the internal map
         *       -> do nothing (we can't do anything).
         */

        var list = GetItems();

        if (list.Count > 0 || HoldGold > 0) // No case 1
        {
            if ((!toBackpack || Map == Map.Internal) && House?.IsAosRules == true) // Case 2
            {
                if (House.IsOwner(Owner)) // Move to moving crate
                {
                    House.MovingCrate ??= new MovingCrate(House);

                    if (HoldGold > 0)
                    {
                        Banker.Deposit(House.MovingCrate, HoldGold);
                    }

                    foreach (var item in list)
                    {
                        House.MovingCrate.DropItem(item);
                    }
                }
                else // Move to vendor inventory
                {
                    var inventory = new VendorInventory(House, Owner, Name, ShopName);
                    inventory.Gold = HoldGold;

                    foreach (var item in list)
                    {
                        inventory.AddItem(item);
                    }

                    House.VendorInventories.Add(inventory);
                }
            }
            else if ((toBackpack || House?.IsAosRules != true) && Map != Map.Internal) // Case 3 - Move to backpack
            {
                var backpack = new Backpack();

                if (HoldGold > 0)
                {
                    Banker.Deposit(backpack, HoldGold);
                }

                foreach (var item in list)
                {
                    backpack.DropItem(item);
                }

                backpack.MoveToWorld(Location, Map);
            }
        }

        Delete();
    }

    private void FixDresswear()
    {
        for (var i = 0; i < Items.Count; ++i)
        {
            var item = Items[i];

            item.Layer = item switch
            {
                BaseHat         => Layer.Helm,
                BaseMiddleTorso => Layer.MiddleTorso,
                BaseOuterLegs   => Layer.OuterLegs,
                BaseOuterTorso  => Layer.OuterTorso,
                BasePants       => Layer.Pants,
                BaseShirt       => Layer.Shirt,
                BaseWaist       => Layer.Waist,
                BaseShoes       => Layer.Shoes,
                _               => item.Layer
            };

            if (item is Sandals)
            {
                item.Hue = 0;
            }
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _payTimer.Stop();

        House = null;

        Placeholder?.Delete();
    }

    public override bool IsSnoop(Mobile from) => false;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (BaseHouse.NewVendorSystem)
        {
            list.Add(1062449, ShopName); // Shop Name: ~1_NAME~
        }
    }

    public VendorItem GetVendorItem(Item item) => _sellItems.GetValueOrDefault(item);

    private VendorItem SetVendorItem(Item item, int price, string description) =>
        SetVendorItem(item, price, description, Core.Now);

    private VendorItem SetVendorItem(Item item, int price, string description, DateTime created)
    {
        RemoveVendorItem(item);

        var vi = new VendorItem(item, price, description, created);
        ReplaceInSellItems(item, vi);

        item.InvalidateProperties();

        return vi;
    }

    private void RemoveVendorItem(Item item)
    {
        var vi = GetVendorItem(item);

        if (vi != null)
        {
            vi.Invalidate();
            RemoveFromSellItems(item);

            foreach (var subItem in item.Items)
            {
                RemoveVendorItem(subItem);
            }

            item.InvalidateProperties();
        }
    }

    private bool CanBeVendorItem(Item item)
    {
        var parent = item.Parent as Item;

        if (parent == Backpack)
        {
            return true;
        }

        if (parent is Container)
        {
            var parentVI = GetVendorItem(parent);

            if (parentVI != null)
            {
                return !parentVI.IsForSale;
            }
        }

        return false;
    }

    public override void OnSubItemAdded(Item item)
    {
        base.OnSubItemAdded(item);

        if (GetVendorItem(item) == null && CanBeVendorItem(item))
        {
            SetVendorItem(item, 999, "");
        }
    }

    public override void OnSubItemRemoved(Item item)
    {
        base.OnSubItemRemoved(item);

        if (item.GetBounce() == null)
        {
            RemoveVendorItem(item);
        }
    }

    public override void OnSubItemBounceCleared(Item item)
    {
        base.OnSubItemBounceCleared(item);

        if (!CanBeVendorItem(item))
        {
            RemoveVendorItem(item);
        }
    }

    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);

        if (item == Backpack)
        {
            foreach (var subItem in item.Items)
            {
                RemoveVendorItem(subItem);
            }
        }
    }

    public override bool OnDragDrop(Mobile from, Item item)
    {
        if (!IsOwner(from))
        {
            SayTo(from, 503209); // I can only take item from the shop owner.
            return false;
        }

        if (item is Gold)
        {
            if (BaseHouse.NewVendorSystem)
            {
                if (HoldGold < 1000000)
                {
                    SayTo(from, 503210); // I'll take that to fund my services.

                    HoldGold += item.Amount;
                    item.Delete();

                    return true;
                }

                from.SendLocalizedMessage(
                    1062493
                ); // Your vendor has sufficient funds for operation and cannot accept this gold.

                return false;
            }

            if (BankAccount < 1000000)
            {
                SayTo(from, 503210); // I'll take that to fund my services.

                BankAccount += item.Amount;
                item.Delete();

                return true;
            }

            from.SendLocalizedMessage(
                1062493
            ); // Your vendor has sufficient funds for operation and cannot accept this gold.

            return false;
        }

        var newItem = GetVendorItem(item) == null;

        if (Backpack?.TryDropItem(from, item, false) == true)
        {
            if (newItem)
            {
                OnItemGiven(from, item);
            }

            return true;
        }

        SayTo(from, 503211); // I can't carry any more.
        return false;
    }

    public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
    {
        if (IsOwner(from))
        {
            if (GetVendorItem(item) == null)
            {
                Timer.StartTimer(() => OnItemGiven(from, item));
            }

            return true;
        }

        SayTo(from, 503209); // I can only take item from the shop owner.
        return false;
    }

    private void OnItemGiven(Mobile from, Item item)
    {
        var vi = GetVendorItem(item);

        if (vi == null)
        {
            return;
        }

        var name = item.Name.DefaultIfNullOrEmpty($"#{item.LabelNumber}");

        from.SendLocalizedMessage(1043303, name); // Type in a price and description for ~1_ITEM~ (ESC=not for sale)
        from.Prompt = new VendorPricePrompt(this, vi);
    }

    public override bool AllowEquipFrom(Mobile from) =>
        BaseHouse.NewVendorSystem && IsOwner(from) || base.AllowEquipFrom(from);

    public override bool CheckNonlocalLift(Mobile from, Item item)
    {
        if (item.IsChildOf(Backpack))
        {
            if (IsOwner(from))
            {
                return true;
            }

            SayTo(from, 503223); // If you'd like to purchase an item, just ask.
            return false;
        }

        if (BaseHouse.NewVendorSystem && IsOwner(from))
        {
            return true;
        }

        return base.CheckNonlocalLift(from, item);
    }

    public bool CanInteractWith(Mobile from, bool ownerOnly)
    {
        if (!from.CanSee(this) || !Utility.InUpdateRange(from.Location, Location) || !from.CheckAlive())
        {
            return false;
        }

        if (ownerOnly)
        {
            return IsOwner(from);
        }

        if (House?.IsBanned(from) == true && !IsOwner(from))
        {
            from.SendLocalizedMessage(
                1062674
            ); // You can't shop from this home as you have been banned from this establishment.
            return false;
        }

        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsOwner(from))
        {
            SendOwnerGump(from);
        }
        else if (CanInteractWith(from, false))
        {
            OpenBackpack(from);
        }
    }

    public override void DisplayPaperdollTo(Mobile m)
    {
        if (BaseHouse.NewVendorSystem)
        {
            base.DisplayPaperdollTo(m);
        }
        else if (CanInteractWith(m, false))
        {
            OpenBackpack(m);
        }
    }

    public void SendOwnerGump(Mobile to)
    {
        var gumps = to.GetGumps();

        if (BaseHouse.NewVendorSystem)
        {
            gumps.Close<NewPlayerVendorCustomizeGump>();
            gumps.Close<NewPlayerVendorOwnerGump>();
            gumps.Send(new NewPlayerVendorOwnerGump(this));
        }
        else
        {
            gumps.Close<PlayerVendorCustomizeGump>();
            gumps.Close<PlayerVendorOwnerGump>();
            gumps.Send(new PlayerVendorOwnerGump(this));
        }
    }

    public void OpenBackpack(Mobile from)
    {
        if (Backpack != null)
        {
            SayTo(from, IsOwner(from) ? 1010642 : 503208); // Take a look at my/your goods.

            Backpack.DisplayTo(from);
        }
    }

    public static void TryToBuy(Item item, Mobile from)
    {
        if (item.RootParent is not PlayerVendor vendor || !vendor.CanInteractWith(from, false))
        {
            return;
        }

        if (!ContentFeatureFlags.PlayerVendors && from.AccessLevel < AccessLevel.Administrator)
        {
            from.SendMessage(0x22, "Player vendor transactions are temporarily disabled.");
            return;
        }

        if (vendor.IsOwner(from))
        {
            vendor.SayTo(from, 503212); // You own this shop, just take what you want.
            return;
        }

        var vi = vendor.GetVendorItem(item);

        if (vi == null)
        {
            vendor.SayTo(from, 503216); // You can't buy that.
        }
        else if (!vi.IsForSale)
        {
            vendor.SayTo(from, 503202); // This item is not for sale.
        }
        else if (vi.Created + TimeSpan.FromMinutes(1.0) > Core.Now)
        {
            from.SendMessage("You cannot buy this item right now.  Please wait one minute and try again.");
        }
        else
        {
            from.SendGump(new PlayerVendorBuyGump(vendor, vi));
        }
    }

    public void CollectGold(Mobile to)
    {
        if (HoldGold > 0)
        {
            SayTo(to, $"How much of the {HoldGold} that I'm holding would you like?");
            to.SendMessage("Enter the amount of gold you wish to withdraw (ESC = CANCEL):");

            to.Prompt = new CollectGoldPrompt(this);
        }
        else
        {
            SayTo(to, 503215); // I am holding no gold for you.
        }
    }

    public int GiveGold(Mobile to, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        if (amount > HoldGold)
        {
            SayTo(to, $"I'm sorry, but I'm only holding {HoldGold} gold for you.");
            return 0;
        }

        var amountGiven = Banker.DepositUpTo(to, amount);
        HoldGold -= amountGiven;

        if (amountGiven > 0)
        {
            to.SendLocalizedMessage(
                1060397,
                amountGiven.ToString()
            ); // ~1_AMOUNT~ gold has been deposited into your bank box.
        }

        if (amountGiven == 0)
        {
            SayTo(
                to,
                1070755
            ); // Your bank box cannot hold the gold you are requesting.  I will keep the gold until you can take it.
        }
        else if (amount > amountGiven)
        {
            SayTo(
                to,
                1070756
            ); // I can only give you part of the gold now, as your bank box is too full to hold the full amount.
        }
        else if (HoldGold > 0)
        {
            SayTo(to, 1042639); // Your gold has been transferred.
        }
        else
        {
            SayTo(to, 503234); // All the gold I have been carrying for you has been deposited into your bank account.
        }

        return amountGiven;
    }

    public void Dismiss(Mobile from)
    {
        var pack = Backpack;

        if (pack?.Items.Count > 0)
        {
            SayTo(from, 1038325); // You cannot dismiss me while I am holding your goods.
            return;
        }

        if (HoldGold > 0)
        {
            GiveGold(from, HoldGold);

            if (HoldGold > 0)
            {
                return;
            }
        }

        Destroy(true);
    }

    public void Rename(Mobile from)
    {
        from.SendLocalizedMessage(1062494); // Enter a new name for your vendor (20 characters max):

        from.Prompt = new VendorNamePrompt(this);
    }

    public void RenameShop(Mobile from)
    {
        from.SendLocalizedMessage(1062433); // Enter a new name for your shop (20 chars max):

        from.Prompt = new ShopNamePrompt(this);
    }

    public bool CheckTeleport(Mobile to)
    {
        if (Deleted || !IsOwner(to) || House == null || Map == Map.Internal)
        {
            return false;
        }

        if (House.IsInside(to) || to.Map != House.Map || !House.InRange(to.Location, 5))
        {
            return false;
        }

        if (Placeholder == null)
        {
            Placeholder = new PlayerVendorPlaceholder(this);
            Placeholder.MoveToWorld(Location, Map);

            MoveToWorld(to.Location, to.Map);

            to.SendLocalizedMessage(
                1062431
            ); // This vendor has been moved out of the house to your current location temporarily.  The vendor will return home automatically after two minutes have passed once you are done managing its inventory or customizing it.
        }
        else
        {
            Placeholder.RestartTimer();

            to.SendLocalizedMessage(
                1062430
            ); // This vendor is currently temporarily in a location outside its house.  The vendor will return home automatically after two minutes have passed once you are done managing its inventory or customizing it.
        }

        return true;
    }

    public void Return()
    {
        Placeholder?.Delete();
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        if (from.Alive && Placeholder != null && IsOwner(from))
        {
            list.Add(new ReturnVendorEntry());
        }

        base.GetContextMenuEntries(from, ref list);
    }

    public override bool HandlesOnSpeech(Mobile from) => from.Alive && from.GetDistanceToSqrt(this) <= 3;

    public bool WasNamed(string speech) => Name != null && speech.InsensitiveStartsWith(Name);

    public override void OnSpeech(SpeechEventArgs e)
    {
        var from = e.Mobile;

        if (e.Handled || !from.Alive || from.GetDistanceToSqrt(this) > 3)
        {
            return;
        }

        if (e.HasKeyword(0x3C) || e.HasKeyword(0x171) && WasNamed(e.Speech)) // vendor buy, *buy*
        {
            if (IsOwner(from))
            {
                SayTo(from, 503212); // You own this shop, just take what you want.
            }
            else if (House?.IsBanned(from) != true)
            {
                from.SendLocalizedMessage(503213); // Select the item you wish to buy.
                from.Target = new PVBuyTarget();

                e.Handled = true;
            }
        }
        else if (e.HasKeyword(0x3D) || e.HasKeyword(0x172) && WasNamed(e.Speech)) // vendor browse, *browse
        {
            if (House?.IsBanned(from) == true && !IsOwner(from))
            {
                SayTo(from, 1062674); // You can't shop from this home as you have been banned from this establishment.
            }
            else
            {
                if (WasNamed(e.Speech))
                {
                    OpenBackpack(from);
                }
                else
                {
                    foreach (var m in e.Mobile.GetMobilesInRange<PlayerVendor>(2))
                    {
                        if (m.CanSee(e.Mobile) && m.InLOS(e.Mobile))
                        {
                            m.OpenBackpack(from);
                        }
                    }
                }

                e.Handled = true;
            }
        }
        else if (e.HasKeyword(0x3E) || e.HasKeyword(0x173) && WasNamed(e.Speech)) // vendor collect, *collect
        {
            if (IsOwner(from))
            {
                CollectGold(from);

                e.Handled = true;
            }
        }
        else if (e.HasKeyword(0x3F) || e.HasKeyword(0x174) && WasNamed(e.Speech)) // vendor status, *status
        {
            if (IsOwner(from))
            {
                SendOwnerGump(from);

                e.Handled = true;
            }
            else
            {
                SayTo(from, 503226); // What do you care? You don't run this shop.
            }
        }
        else if (e.HasKeyword(0x40) || e.HasKeyword(0x175) && WasNamed(e.Speech)) // vendor dismiss, *dismiss
        {
            if (IsOwner(from))
            {
                Dismiss(from);

                e.Handled = true;
            }
        }
        else if (e.HasKeyword(0x41) || e.HasKeyword(0x176) && WasNamed(e.Speech)) // vendor cycle, *cycle
        {
            if (IsOwner(from))
            {
                Direction = GetDirectionTo(from);

                e.Handled = true;
            }
        }
    }

    public override bool CanBeDamaged() => false;

    private class ReturnVendorEntry : ContextMenuEntry
    {
        public ReturnVendorEntry() : base(6214)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from.CheckAlive() && target is PlayerVendor { Deleted: false } vendor && vendor.IsOwner(from))
            {
                vendor.Return();
            }
        }
    }

    private class PayTimer : Timer
    {
        private readonly PlayerVendor m_Vendor;

        public PayTimer(PlayerVendor vendor, TimeSpan delay) : base(delay, GetInterval())
        {
            m_Vendor = vendor;
        }

        public static TimeSpan GetInterval()
        {
            if (BaseHouse.NewVendorSystem)
            {
                return TimeSpan.FromDays(1.0);
            }

            return TimeSpan.FromMinutes(Clock.MinutesPerUODay);
        }

        protected override void OnTick()
        {
            m_Vendor.NextPayTime = Core.Now + Interval;

            int pay;
            int totalGold;
            if (BaseHouse.NewVendorSystem)
            {
                pay = m_Vendor.ChargePerRealWorldDay;
                totalGold = m_Vendor.HoldGold;
            }
            else
            {
                pay = m_Vendor.ChargePerDay;
                totalGold = m_Vendor.BankAccount + m_Vendor.HoldGold;
            }

            if (pay > totalGold)
            {
                m_Vendor.Destroy(!BaseHouse.NewVendorSystem);
            }
            else
            {
                if (!BaseHouse.NewVendorSystem)
                {
                    if (m_Vendor.BankAccount >= pay)
                    {
                        m_Vendor.BankAccount -= pay;
                        pay = 0;
                    }
                    else
                    {
                        pay -= m_Vendor.BankAccount;
                        m_Vendor.BankAccount = 0;
                    }
                }

                m_Vendor.HoldGold -= pay;
            }
        }
    }

    [PlayerVendorTarget]
    private class PVBuyTarget : Target
    {
        public PVBuyTarget() : base(3, false, TargetFlags.None) => AllowNonlocal = true;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Item item)
            {
                TryToBuy(item, from);
            }
        }
    }

    private class VendorPricePrompt : Prompt
    {
        private readonly PlayerVendor m_Vendor;
        private readonly VendorItem m_VI;

        public VendorPricePrompt(PlayerVendor vendor, VendorItem vi)
        {
            m_Vendor = vendor;
            m_VI = vi;
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (!m_VI.Valid || !m_Vendor.CanInteractWith(from, true))
            {
                return;
            }

            string firstWord;

            var sep = text.IndexOfAny(new[] { ' ', ',' });
            firstWord = sep >= 0 ? text[..sep] : text;

            string description;

            if (int.TryParse(firstWord, out var price))
            {
                description = sep >= 0 ? text[(sep + 1)..].Trim() : "";
            }
            else
            {
                price = -1;
                description = text.Trim();
            }

            SetInfo(from, price, description.FixHtml());
        }

        public override void OnCancel(Mobile from)
        {
            if (!m_VI.Valid || !m_Vendor.CanInteractWith(from, true))
            {
                return;
            }

            SetInfo(from, -1, "");
        }

        private void SetInfo(Mobile from, int price, string description)
        {
            var item = m_VI.Item;

            var setPrice = false;

            if (price < 0) // Not for sale
            {
                price = -1;

                if (item is Container)
                {
                    if (item is LockableContainer container && container.Locked)
                    {
                        m_Vendor.SayTo(from, 1043298); // Locked items may not be made not-for-sale.
                    }
                    else if (item.Items.Count > 0)
                    {
                        m_Vendor.SayTo(from, 1043299); // To be not for sale, all items in a container must be for sale.
                    }
                    else
                    {
                        setPrice = true;
                    }
                }
                else if (item is BaseBook or BulkOrderBook)
                {
                    setPrice = true;
                }
                else
                {
                    m_Vendor.SayTo(
                        from,
                        1043301
                    ); // Only the following may be made not-for-sale: books, containers, keyrings, and items in for-sale containers.
                }
            }
            else
            {
                if (price > 100000000)
                {
                    price = 100000000;
                    from.SendMessage("You cannot price items above 100,000,000 gold.  The price has been adjusted.");
                }

                setPrice = true;
            }

            if (setPrice)
            {
                m_Vendor.SetVendorItem(item, price, description);
            }
            else
            {
                m_VI.Description = description;
            }
        }
    }

    private class CollectGoldPrompt : Prompt
    {
        private readonly PlayerVendor m_Vendor;

        public CollectGoldPrompt(PlayerVendor vendor) => m_Vendor = vendor;

        public override void OnResponse(Mobile from, string text)
        {
            if (!m_Vendor.CanInteractWith(from, true))
            {
                return;
            }

            text = text.Trim();

            if (!int.TryParse(text, out var amount))
            {
                amount = 0;
            }

            GiveGold(from, amount);
        }

        public override void OnCancel(Mobile from)
        {
            if (!m_Vendor.CanInteractWith(from, true))
            {
                return;
            }

            GiveGold(from, 0);
        }

        private void GiveGold(Mobile to, int amount)
        {
            if (amount <= 0)
            {
                m_Vendor.SayTo(to, "Very well. I will hold on to the money for now then.");
            }
            else
            {
                m_Vendor.GiveGold(to, amount);
            }
        }
    }

    private class VendorNamePrompt : Prompt
    {
        private readonly PlayerVendor m_Vendor;

        public VendorNamePrompt(PlayerVendor vendor) => m_Vendor = vendor;

        public override void OnResponse(Mobile from, string text)
        {
            if (!m_Vendor.CanInteractWith(from, true))
            {
                return;
            }

            var name = text.AsSpan().Trim();

            if (!NameVerification.ValidateVendorName(name))
            {
                m_Vendor.SayTo(from, "That name is unacceptable.");
                return;
            }

            m_Vendor.Name = name.FixHtml();

            from.SendLocalizedMessage(1062496); // Your vendor has been renamed.

            from.SendGump(new NewPlayerVendorOwnerGump(m_Vendor));
        }
    }

    private class ShopNamePrompt : Prompt
    {
        private readonly PlayerVendor m_Vendor;

        public ShopNamePrompt(PlayerVendor vendor) => m_Vendor = vendor;

        public override void OnResponse(Mobile from, string text)
        {
            if (!m_Vendor.CanInteractWith(from, true))
            {
                return;
            }

            var name = text.AsSpan().Trim();

            if (!NameVerification.ValidateVendorName(name))
            {
                m_Vendor.SayTo(from, "That name is unacceptable.");
                return;
            }

            m_Vendor.ShopName = name.FixHtml();

            from.SendGump(new NewPlayerVendorOwnerGump(m_Vendor));
        }
    }
}
