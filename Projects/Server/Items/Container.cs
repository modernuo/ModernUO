using System;
using System.Collections.Generic;
using System.IO;
using Server.Collections;
using Server.Logging;
using Server.Network;

namespace Server.Items;

public delegate void OnItemConsumed(Item item, int amount);

public delegate int CheckItemGroup(Item a, Item b);

public delegate void ContainerSnoopHandler(Container cont, Mobile from);

public partial class Container : Item
{
    private ContainerData m_ContainerData;

    private int m_DropSound;
    private int m_GumpID;

    internal List<Item> m_Items;
    private int m_MaxItems;
    private int m_TotalGold;

    private int m_TotalItems;
    private int m_TotalWeight;
    private int _version;

    public Container(int itemID) : base(itemID)
    {
        m_GumpID = -1;
        m_DropSound = -1;
        m_MaxItems = -1;

        UpdateContainerData();
    }

    public Container(Serial serial) : base(serial)
    {
    }

    public static ContainerSnoopHandler SnoopHandler { get; set; }

    public ContainerData ContainerData
    {
        get
        {
            if (m_ContainerData == null)
            {
                UpdateContainerData();
            }

            return m_ContainerData;
        }
        set => m_ContainerData = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public override int ItemID
    {
        get => base.ItemID;
        set
        {
            var oldID = ItemID;

            base.ItemID = value;

            if (ItemID != oldID)
            {
                UpdateContainerData();
            }
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int GumpID
    {
        get => m_GumpID == -1 ? DefaultGumpID : m_GumpID;
        set => m_GumpID = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int DropSound
    {
        get => m_DropSound == -1 ? DefaultDropSound : m_DropSound;
        set => m_DropSound = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxItems
    {
        get => m_MaxItems == -1 ? DefaultMaxItems : m_MaxItems;
        set
        {
            m_MaxItems = value;
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual int MaxWeight
    {
        get
        {
            if (Parent is Container container && container.MaxWeight == 0)
            {
                return 0;
            }

            return DefaultMaxWeight;
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool LiftOverride { get; set; }

    public virtual Rectangle2D Bounds => ContainerData.Bounds;

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual int DefaultGumpID => ContainerData.GumpID;

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual int DefaultDropSound => ContainerData.DropSound;

    public virtual int DefaultMaxItems => GlobalMaxItems;
    public virtual int DefaultMaxWeight => GlobalMaxWeight;

    public virtual bool IsDecoContainer => !Movable && !IsLockedDown && !IsSecure && Parent == null && !LiftOverride;

    public static int GlobalMaxItems { get; set; } = 125;

    public static int GlobalMaxWeight { get; set; } = 400;

    public virtual bool DisplaysContent => true;

    public List<Mobile> Openers { get; set; }

    public virtual bool IsPublicContainer => false;

    public virtual void UpdateContainerData()
    {
        ContainerData = ContainerData.GetData(ItemID);
    }

    public virtual int GetDroppedSound(Item item)
    {
        var dropSound = item.GetDropSound();

        return dropSound != -1 ? dropSound : DropSound;
    }

    public override void OnSnoop(Mobile from)
    {
        SnoopHandler?.Invoke(this, from);
    }

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
    {
        if (from.AccessLevel < AccessLevel.GameMaster && IsDecoContainer)
        {
            reject = LRReason.CannotLift;
            return false;
        }

        return base.CheckLift(from, item, ref reject);
    }

    public override bool CheckItemUse(Mobile from, Item item)
    {
        if (item != this && from.AccessLevel < AccessLevel.GameMaster && IsDecoContainer)
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return false;
        }

        return base.CheckItemUse(from, item);
    }

    public bool CheckHold(Mobile m, Item item, bool message) => CheckHold(m, item, message, true, 0, 0);

    public bool CheckHold(Mobile m, Item item, bool message, bool checkItems) =>
        CheckHold(m, item, message, checkItems, 0, 0);

    public virtual bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
    {
        if (m == null || m.AccessLevel < AccessLevel.GameMaster)
        {
            if (IsDecoContainer)
            {
                if (message)
                {
                    SendCantStoreMessage(m, item);
                }

                return false;
            }

            var maxItems = MaxItems;

            if (checkItems && maxItems != 0 &&
                TotalItems + plusItems + item.TotalItems + (item.IsVirtualItem ? 0 : 1) > maxItems)
            {
                if (message)
                {
                    SendFullItemsMessage(m, item);
                }

                return false;
            }

            if (MaxWeight != 0 && TotalWeight + plusWeight + item.TotalWeight + item.PileWeight > MaxWeight)
            {
                if (message)
                {
                    SendFullWeightMessage(m, item);
                }

                return false;
            }
        }

        var parent = Parent;

        while (parent != null)
        {
            if (parent is Container container)
            {
                return container.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
            }

            if (parent is not Item parentItem)
            {
                break;
            }

            parent = parentItem.Parent;
        }

        return true;
    }

    public virtual void SendFullItemsMessage(Mobile to, Item item)
    {
        // That container cannot hold more items.
        to.SendLocalizedMessage(1080017);
    }

    public virtual void SendFullWeightMessage(Mobile to, Item item)
    {
        // That container cannot hold more weight.
        to.SendLocalizedMessage(1080016);
    }

    public virtual void SendCantStoreMessage(Mobile to, Item item)
    {
        to.SendLocalizedMessage(500176); // That is not your container, you can't store things here.
    }

    public virtual bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (!CheckHold(from, item, true, true))
        {
            return false;
        }

        item.Location = new Point3D(p.m_X, p.m_Y, 0);
        AddItem(item);

        from.SendSound(GetDroppedSound(item), GetWorldLocation());

        return true;
    }

    private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
    {
        if (setIf)
        {
            flags |= toSet;
        }
    }

    private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet) => (flags & toGet) != 0;

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(2); // version

        var flags = SaveFlag.None;

        SetSaveFlag(ref flags, SaveFlag.MaxItems, m_MaxItems != -1);
        SetSaveFlag(ref flags, SaveFlag.GumpID, m_GumpID != -1);
        SetSaveFlag(ref flags, SaveFlag.DropSound, m_DropSound != -1);
        SetSaveFlag(ref flags, SaveFlag.LiftOverride, LiftOverride);

        writer.Write((byte)flags);

        if (GetSaveFlag(flags, SaveFlag.MaxItems))
        {
            writer.WriteEncodedInt(m_MaxItems);
        }

        if (GetSaveFlag(flags, SaveFlag.GumpID))
        {
            writer.WriteEncodedInt(m_GumpID);
        }

        if (GetSaveFlag(flags, SaveFlag.DropSound))
        {
            writer.WriteEncodedInt(m_DropSound);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        switch (version)
        {
            case 2:
                {
                    var flags = (SaveFlag)reader.ReadByte();

                    if (GetSaveFlag(flags, SaveFlag.MaxItems))
                    {
                        m_MaxItems = reader.ReadEncodedInt();
                    }
                    else
                    {
                        m_MaxItems = -1;
                    }

                    if (GetSaveFlag(flags, SaveFlag.GumpID))
                    {
                        m_GumpID = reader.ReadEncodedInt();
                    }
                    else
                    {
                        m_GumpID = -1;
                    }

                    if (GetSaveFlag(flags, SaveFlag.DropSound))
                    {
                        m_DropSound = reader.ReadEncodedInt();
                    }
                    else
                    {
                        m_DropSound = -1;
                    }

                    LiftOverride = GetSaveFlag(flags, SaveFlag.LiftOverride);

                    break;
                }
            case 1:
                {
                    m_MaxItems = reader.ReadInt();
                    goto case 0;
                }
            case 0:
                {
                    if (version < 1)
                    {
                        m_MaxItems = GlobalMaxItems;
                    }

                    m_GumpID = reader.ReadInt();
                    m_DropSound = reader.ReadInt();

                    if (m_GumpID == DefaultGumpID)
                    {
                        m_GumpID = -1;
                    }

                    if (m_DropSound == DefaultDropSound)
                    {
                        m_DropSound = -1;
                    }

                    if (m_MaxItems == DefaultMaxItems)
                    {
                        m_MaxItems = -1;
                    }

                    // m_Bounds = new Rectangle2D( reader.ReadPoint2D(), reader.ReadPoint2D() );
                    reader.ReadPoint2D();
                    reader.ReadPoint2D();

                    break;
                }
        }

        UpdateContainerData();
    }

    public override int GetTotal(TotalType type)
    {
        return type switch
        {
            TotalType.Gold => m_TotalGold,
            TotalType.Items => m_TotalItems,
            TotalType.Weight => m_TotalWeight,
            _ => base.GetTotal(type)
        };
    }

    public override void UpdateTotal(Item sender, TotalType type, int delta)
    {
        if (sender != this && delta != 0 && !sender.IsVirtualItem)
        {
            switch (type)
            {
                case TotalType.Gold:
                    m_TotalGold += delta;
                    break;

                case TotalType.Items:
                    m_TotalItems += delta;
                    InvalidateProperties();
                    break;

                case TotalType.Weight:
                    m_TotalWeight += delta;
                    InvalidateProperties();
                    break;
            }
        }

        base.UpdateTotal(sender, type, delta);
    }

    public override void UpdateTotals()
    {
        m_TotalGold = 0;
        m_TotalItems = 0;
        m_TotalWeight = 0;

        var items = m_Items;

        if (items == null)
        {
            return;
        }

        for (var i = 0; i < items.Count; ++i)
        {
            var item = items[i];

            item.UpdateTotals();

            if (item.IsVirtualItem)
            {
                continue;
            }

            m_TotalGold += item.TotalGold;
            m_TotalItems += item.TotalItems + 1;
            m_TotalWeight += item.TotalWeight + item.PileWeight;
        }
    }

    public override void OnItemAdded(Item item)
    {
        base.OnItemAdded(item);
        _version++;
    }

    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);
        _version++;
    }

    public virtual bool OnStackAttempt(Mobile from, Item stack, Item dropped) =>
        CheckHold(from, dropped, true, false) && stack.StackWith(from, dropped);

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (TryDropItem(from, dropped, true))
        {
            from.SendSound(GetDroppedSound(dropped), GetWorldLocation());

            return true;
        }

        return false;
    }

    public virtual bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage) =>
        TryDropItem(from, dropped, sendFullMessage, false);

    public virtual bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage, bool playSound)
    {
        var list = Items;

        for (var i = 0; i < list.Count; ++i)
        {
            var item = list[i];

            if (item is not Container && CheckHold(from, dropped, false, false) &&
                item.StackWith(from, dropped, playSound))
            {
                return true;
            }
        }

        if (CheckHold(from, dropped, sendFullMessage, true))
        {
            DropItem(dropped);
            return true;
        }

        return false;
    }

    public virtual bool TryDropItems(Mobile from, bool sendFullMessage, params Item[] droppedItems)
    {
        var dropItems = new List<Item>();
        var stackItems = new List<ItemStackEntry>();

        var extraItems = 0;
        var extraWeight = 0;

        for (var i = 0; i < droppedItems.Length; i++)
        {
            var dropped = droppedItems[i];

            var list = Items;

            var stacked = false;

            for (var j = 0; j < list.Count; ++j)
            {
                var item = list[j];

                if (item is not Container && CheckHold(from, dropped, false, false, 0, extraWeight) &&
                    item.CanStackWith(dropped))
                {
                    stackItems.Add(new ItemStackEntry(item, dropped));
                    extraWeight += (int)Math.Ceiling(item.Weight * (item.Amount + dropped.Amount)) -
                                   item.PileWeight; // extra weight delta, do not need TotalWeight as we do not have hybrid stackable container types
                    stacked = true;
                    break;
                }
            }

            if (!stacked && CheckHold(from, dropped, false, true, extraItems, extraWeight))
            {
                dropItems.Add(dropped);
                extraItems++;
                extraWeight += dropped.TotalWeight + dropped.PileWeight;
            }
        }

        if (dropItems.Count + stackItems.Count == droppedItems.Length) // All good
        {
            for (var i = 0; i < dropItems.Count; i++)
            {
                DropItem(dropItems[i]);
            }

            for (var i = 0; i < stackItems.Count; i++)
            {
                stackItems[i].m_StackItem.StackWith(from, stackItems[i].m_DropItem, false);
            }

            return true;
        }

        return false;
    }

    public virtual void Destroy()
    {
        var loc = GetWorldLocation();
        var map = Map;

        for (var i = Items.Count - 1; i >= 0; --i)
        {
            if (i < Items.Count)
            {
                Items[i].SetLastMoved();
                Items[i].MoveToWorld(loc, map);
            }
        }

        Delete();
    }

    public virtual void DropItem(Item dropped)
    {
        if (dropped == null)
        {
            return;
        }

        AddItem(dropped);

        var bounds = dropped.GetGraphicBounds();
        var ourBounds = Bounds;

        int x, y;

        if (bounds.Width >= ourBounds.Width)
        {
            x = (ourBounds.Width - bounds.Width) / 2;
        }
        else
        {
            x = Utility.Random(ourBounds.Width - bounds.Width);
        }

        if (bounds.Height >= ourBounds.Height)
        {
            y = (ourBounds.Height - bounds.Height) / 2;
        }
        else
        {
            y = Utility.Random(ourBounds.Height - bounds.Height);
        }

        x += ourBounds.X;
        x -= bounds.X;

        y += ourBounds.Y;
        y -= bounds.Y;

        dropped.Location = new Point3D(x, y, 0);
    }

    public override void OnDoubleClickSecureTrade(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 2))
        {
            DisplayTo(from);

            var trade = GetSecureTradeCont()?.Trade;

            if (trade != null)
            {
                if (trade.From.Mobile == from)
                {
                    DisplayTo(trade.To.Mobile);
                }
                else if (trade.To.Mobile == from)
                {
                    DisplayTo(trade.From.Mobile);
                }
            }
        }
        else
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }
    }

    public virtual bool CheckContentDisplay(Mobile from) =>
        DisplaysContent && RootParent == null ||
        RootParent is Item || RootParent == from ||
        from.AccessLevel > AccessLevel.Player;

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (CheckContentDisplay(from))
        {
            if (TotalItems == 1)
            {
                LabelTo(from, $"({TotalItems} item, {TotalWeight} stones)");
            }
            else
            {
                LabelTo(from, $"({TotalItems} items, {TotalWeight} stones)");
            }
        }

        // LabelTo( from, 1050044, String.Format( "{0}\t{1}", TotalItems.ToString(), TotalWeight.ToString() ) );
    }

    public override void OnDelete()
    {
        base.OnDelete();

        Openers = null;
    }

    public virtual void DisplayTo(Mobile to)
    {
        ProcessOpeners(to);

        var ns = to.NetState;

        if (ns != null)
        {
            ns.SendDisplayContainer(Serial, GumpID);

            SendContentTo(ns);

            if (ObjectPropertyList.Enabled)
            {
                for (var i = 0; i < Items.Count; ++i)
                {
                    ns.SendOPLInfo(Items[i]);
                }
            }
        }
    }

    public void ProcessOpeners(Mobile opener)
    {
        if (IsPublicContainer)
        {
            return;
        }

        var contains = false;

        if (Openers != null)
        {
            var worldLoc = GetWorldLocation();
            var map = Map;

            for (var i = 0; i < Openers.Count; ++i)
            {
                var mob = Openers[i];

                if (mob == opener)
                {
                    contains = true;
                }
                else
                {
                    var range = GetUpdateRange(mob);

                    if (mob.Map != map || !mob.InRange(worldLoc, range))
                    {
                        Openers.RemoveAt(i--);
                    }
                }
            }
        }

        if (!contains)
        {
            Openers ??= new List<Mobile>();

            Openers.Add(opener);
        }
        else if (Openers?.Count == 0)
        {
            Openers = null;
        }
    }

    public virtual void SendContentTo(NetState state) => state.SendContainerContent(state.Mobile, this);

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (DisplaysContent) // CheckContentDisplay( from ))
        {
            if (Core.ML)
            {
                if (ParentsContain<BankBox>()) // Root Parent is the Mobile. Parent could be another container.
                {
                    list.Add(
                        1073841, // Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~ stones
                        $"{TotalItems}\t{MaxItems}\t{TotalWeight}"
                    );
                }
                else
                {
                    list.Add(
                        1072241, // Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones
                        $"{TotalItems}\t{MaxItems}\t{TotalWeight}\t{MaxWeight}"
                    );
                }

                // TODO: Where do the other clilocs come into play? 1073839 & 1073840?
            }
            else
            {
                // ~1_COUNT~ items, ~2_WEIGHT~ stones
                list.Add(1050044, $"{TotalItems}\t{TotalWeight}");
            }
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player || from.InRange(GetWorldLocation(), 2))
        {
            DisplayTo(from);
        }
        else
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }
    }

    public bool ConsumeTotalGrouped(Type type, int amount, bool recurse, OnItemConsumed callback, CheckItemGroup grouper)
    {
        if (grouper == null)
        {
            throw new ArgumentNullException(nameof(grouper));
        }

        using var typedItems = ListItemsByType(type, recurse);

        var groups = new List<List<Item>>();
        var idx = 0;

        while (idx < typedItems.Count)
        {
            var a = typedItems[idx++];
            var group = new List<Item>
            {
                a
            };

            while (idx < typedItems.Count)
            {
                var b = typedItems[idx];
                var v = grouper(a, b);

                if (v == 0)
                {
                    group.Add(b);
                }
                else
                {
                    break;
                }

                ++idx;
            }

            groups.Add(group);
        }

        var items = new Item[groups.Count][];
        var totals = new int[groups.Count];

        var hasEnough = false;

        for (var i = 0; i < groups.Count; ++i)
        {
            items[i] = groups[i].ToArray();

            for (var j = 0; j < items[i].Length; ++j)
            {
                totals[i] += items[i][j].Amount;
            }

            if (totals[i] >= amount)
            {
                hasEnough = true;
            }
        }

        if (!hasEnough)
        {
            return false;
        }

        for (var i = 0; i < items.Length; ++i)
        {
            if (totals[i] >= amount)
            {
                var need = amount;

                for (var j = 0; j < items[i].Length; ++j)
                {
                    var item = items[i][j];

                    var theirAmount = item.Amount;

                    if (theirAmount < need)
                    {
                        callback?.Invoke(item, theirAmount);

                        item.Consume(theirAmount);
                        need -= theirAmount;
                    }
                    else
                    {
                        callback?.Invoke(item, need);

                        item.Consume(need);
                        break;
                    }
                }

                break;
            }
        }

        return true;
    }

    public int ConsumeTotalGrouped(
        Type[] types, int[] amounts, bool recurse, OnItemConsumed callback,
        CheckItemGroup grouper
    )
    {
        if (types.Length != amounts.Length)
        {
            throw new ArgumentException("length of types and amounts must match");
        }

        if (grouper == null)
        {
            throw new ArgumentNullException(nameof(grouper));
        }

        var items = new Item[types.Length][][];
        var totals = new int[types.Length][];

        for (var i = 0; i < types.Length; ++i)
        {
            var type = types[i];

            using var typedItems = ListItemsByType(type, recurse);

            var groups = new List<List<Item>>();
            var idx = 0;

            while (idx < typedItems.Count)
            {
                var a = typedItems[idx++];
                var group = new List<Item>
                {
                    a
                };

                while (idx < typedItems.Count)
                {
                    var b = typedItems[idx];
                    var v = grouper(a, b);

                    if (v == 0)
                    {
                        group.Add(b);
                    }
                    else
                    {
                        break;
                    }

                    ++idx;
                }

                groups.Add(group);
            }

            items[i] = new Item[groups.Count][];
            totals[i] = new int[groups.Count];

            var hasEnough = false;

            for (var j = 0; j < groups.Count; ++j)
            {
                items[i][j] = groups[j].ToArray();

                for (var k = 0; k < items[i][j].Length; ++k)
                {
                    totals[i][j] += items[i][j][k].Amount;
                }

                if (totals[i][j] >= amounts[i])
                {
                    hasEnough = true;
                }
            }

            if (!hasEnough)
            {
                return i;
            }
        }

        for (var i = 0; i < items.Length; ++i)
        {
            for (var j = 0; j < items[i].Length; ++j)
            {
                if (totals[i][j] >= amounts[i])
                {
                    var need = amounts[i];

                    for (var k = 0; k < items[i][j].Length; ++k)
                    {
                        var item = items[i][j][k];

                        var theirAmount = item.Amount;

                        if (theirAmount < need)
                        {
                            callback?.Invoke(item, theirAmount);

                            item.Consume(theirAmount);
                            need -= theirAmount;
                        }
                        else
                        {
                            callback?.Invoke(item, need);

                            item.Consume(need);
                            break;
                        }
                    }

                    break;
                }
            }
        }

        return -1;
    }

    public int ConsumeTotalGrouped(
        Type[][] types, int[] amounts, bool recurse, OnItemConsumed callback,
        CheckItemGroup grouper
    )
    {
        if (types.Length != amounts.Length)
        {
            throw new ArgumentException("length of types and amounts must match");
        }

        if (grouper == null)
        {
            throw new ArgumentNullException(nameof(grouper));
        }

        var items = new Item[types.Length][][];
        var totals = new int[types.Length][];

        for (var i = 0; i < types.Length; ++i)
        {
            using var typedItems = ListItemsByType(types[i], recurse);

            var groups = new List<List<Item>>();
            var idx = 0;

            while (idx < typedItems.Count)
            {
                var a = typedItems[idx++];
                var group = new List<Item>
                {
                    a
                };

                while (idx < typedItems.Count)
                {
                    var b = typedItems[idx];
                    var v = grouper(a, b);

                    if (v == 0)
                    {
                        group.Add(b);
                    }
                    else
                    {
                        break;
                    }

                    ++idx;
                }

                groups.Add(group);
            }

            items[i] = new Item[groups.Count][];
            totals[i] = new int[groups.Count];

            var hasEnough = false;

            for (var j = 0; j < groups.Count; ++j)
            {
                items[i][j] = groups[j].ToArray();

                for (var k = 0; k < items[i][j].Length; ++k)
                {
                    totals[i][j] += items[i][j][k].Amount;
                }

                if (totals[i][j] >= amounts[i])
                {
                    hasEnough = true;
                }
            }

            if (!hasEnough)
            {
                return i;
            }
        }

        for (var i = 0; i < items.Length; ++i)
        {
            for (var j = 0; j < items[i].Length; ++j)
            {
                if (totals[i][j] >= amounts[i])
                {
                    var need = amounts[i];

                    for (var k = 0; k < items[i][j].Length; ++k)
                    {
                        var item = items[i][j][k];

                        var theirAmount = item.Amount;

                        if (theirAmount < need)
                        {
                            callback?.Invoke(item, theirAmount);

                            item.Consume(theirAmount);
                            need -= theirAmount;
                        }
                        else
                        {
                            callback?.Invoke(item, need);

                            item.Consume(need);
                            break;
                        }
                    }

                    break;
                }
            }
        }

        return -1;
    }

    public int ConsumeTotal(Type[][] types, int[] amounts, bool recurse = true, OnItemConsumed callback = null)
    {
        if (types.Length != amounts.Length)
        {
            throw new ArgumentException("length of types and amounts must match");
        }

        var items = new Item[types.Length][];
        var totals = new int[types.Length];

        for (var i = 0; i < types.Length; ++i)
        {
            using var typedItems = ListItemsByType(types[i], recurse);

            items[i] = new Item[typedItems.Count];

            for (var j = 0; j < typedItems.Count; ++j)
            {
                items[i][j] = typedItems[j];
                totals[i] += typedItems[j].Amount;
            }

            if (totals[i] < amounts[i])
            {
                return i;
            }
        }

        for (var i = 0; i < types.Length; ++i)
        {
            var need = amounts[i];

            for (var j = 0; j < items[i].Length; ++j)
            {
                var item = items[i][j];

                var theirAmount = item.Amount;

                if (theirAmount < need)
                {
                    callback?.Invoke(item, theirAmount);

                    item.Consume(theirAmount);
                    need -= theirAmount;
                }
                else
                {
                    callback?.Invoke(item, need);

                    item.Consume(need);
                    break;
                }
            }
        }

        return -1;
    }

    public int ConsumeTotal(Type[] types, int[] amounts, bool recurse = true, OnItemConsumed callback = null)
    {
        if (types.Length != amounts.Length)
        {
            throw new ArgumentException("length of types and amounts must match");
        }

        var items = new Item[types.Length][];
        var totals = new int[types.Length];

        for (var i = 0; i < types.Length; ++i)
        {
            using var typedItems = ListItemsByType(types[i], recurse);

            items[i] = new Item[typedItems.Count];

            for (var j = 0; j < typedItems.Count; ++j)
            {
                items[i][j] = typedItems[j];
                totals[i] += typedItems[j].Amount;
            }

            if (totals[i] < amounts[i])
            {
                return i;
            }
        }

        for (var i = 0; i < types.Length; ++i)
        {
            var need = amounts[i];

            for (var j = 0; j < items[i].Length; ++j)
            {
                var item = items[i][j];

                var theirAmount = item.Amount;

                if (theirAmount < need)
                {
                    callback?.Invoke(item, theirAmount);

                    item.Consume(theirAmount);
                    need -= theirAmount;
                }
                else
                {
                    callback?.Invoke(item, need);

                    item.Consume(need);
                    break;
                }
            }
        }

        return -1;
    }

    public bool ConsumeTotal(Type type, int amount = 1, bool recurse = true, OnItemConsumed callback = null)
    {
        var total = 0;

        using var typedItems = ListItemsByType(type, recurse);

        // First pass, compute total
        foreach (var item in typedItems)
        {
            total += item.Amount;

            if (total >= amount)
            {
                break;
            }
        }

        // We have enough, so consume it
        if (total >= amount)
        {
            var need = amount;

            foreach (var item in typedItems)
            {
                var theirAmount = item.Amount;

                if (theirAmount < need)
                {
                    callback?.Invoke(item, theirAmount);

                    item.Consume(theirAmount);
                    need -= theirAmount;
                }
                else
                {
                    callback?.Invoke(item, need);

                    item.Consume(need);
                    return true;
                }
            }
        }

        return false;
    }

    public int ConsumeUpTo(Type type, int amount, bool recurse = true)
    {
        var consumed = 0;

        using var toDelete = PooledRefQueue<Item>.Create();

        RecurseConsumeUpTo(this, type, amount, recurse, ref consumed, toDelete);

        while (toDelete.Count > 0)
        {
            toDelete.Dequeue().Delete();
        }

        return consumed;
    }

    private static void RecurseConsumeUpTo(
        Item current, Type type, int amount, bool recurse, ref int consumed,
        PooledRefQueue<Item> toDelete
    )
    {
        if (current == null || current.Items.Count == 0)
        {
            return;
        }

        var list = current.Items;

        for (var i = 0; i < list.Count; ++i)
        {
            var item = list[i];

            if (type.IsInstanceOfType(item))
            {
                var need = amount - consumed;
                var theirAmount = item.Amount;

                if (theirAmount <= need)
                {
                    toDelete.Enqueue(item);
                    consumed += theirAmount;
                }
                else
                {
                    item.Amount -= need;
                    consumed += need;

                    return;
                }
            }
            else if (recurse && item is Container)
            {
                RecurseConsumeUpTo(item, type, amount, true, ref consumed, toDelete);
            }
        }
    }

    public int GetBestGroupAmount(Type type, bool recurse, CheckItemGroup grouper)
    {
        if (grouper == null)
        {
            throw new ArgumentNullException(nameof(grouper));
        }

        var best = 0;

        using var typedItems = ListItemsByType(type, recurse);

        var groups = new List<List<Item>>();
        var idx = 0;

        while (idx < typedItems.Count)
        {
            var a = typedItems[idx++];
            var group = new List<Item>
            {
                a
            };

            while (idx < typedItems.Count)
            {
                var b = typedItems[idx];
                var v = grouper(a, b);

                if (v == 0)
                {
                    group.Add(b);
                }
                else
                {
                    break;
                }

                ++idx;
            }

            groups.Add(group);
        }

        for (var i = 0; i < groups.Count; ++i)
        {
            var items = groups[i].ToArray();

            var total = 0;

            for (var j = 0; j < items.Length; ++j)
            {
                total += items[j].Amount;
            }

            if (total >= best)
            {
                best = total;
            }
        }

        return best;
    }

    public int GetBestGroupAmount(Type[] types, bool recurse, CheckItemGroup grouper)
    {
        if (grouper == null)
        {
            throw new ArgumentNullException(nameof(grouper));
        }

        var best = 0;

        var typedItems = ListItemsByType(types, recurse);

        var groups = new List<List<Item>>();
        var idx = 0;

        while (idx < typedItems.Count)
        {
            var a = typedItems[idx++];
            var group = new List<Item>
            {
                a
            };

            while (idx < typedItems.Count)
            {
                var b = typedItems[idx];
                var v = grouper(a, b);

                if (v == 0)
                {
                    group.Add(b);
                }
                else
                {
                    break;
                }

                ++idx;
            }

            groups.Add(group);
        }

        for (var j = 0; j < groups.Count; ++j)
        {
            var items = groups[j].ToArray();
            var total = 0;

            foreach (var item in items)
            {
                total += item.Amount;
            }

            if (total >= best)
            {
                best = total;
            }
        }

        return best;
    }

    public int GetBestGroupAmount(Type[][] types, bool recurse, CheckItemGroup grouper)
    {
        if (grouper == null)
        {
            throw new ArgumentNullException(nameof(grouper));
        }

        var best = 0;

        for (var i = 0; i < types.Length; ++i)
        {
            using var typedItems = ListItemsByType(types[i], recurse);

            var groups = new List<List<Item>>();
            var idx = 0;

            while (idx < typedItems.Count)
            {
                var a = typedItems[idx++];
                var group = new List<Item>
                {
                    a
                };

                while (idx < typedItems.Count)
                {
                    var b = typedItems[idx];
                    var v = grouper(a, b);

                    if (v == 0)
                    {
                        group.Add(b);
                    }
                    else
                    {
                        break;
                    }

                    ++idx;
                }

                groups.Add(group);
            }

            for (var j = 0; j < groups.Count; ++j)
            {
                var items = groups[j].ToArray();
                var total = 0;

                for (var k = 0; k < items.Length; ++k)
                {
                    total += items[k].Amount;
                }

                if (total >= best)
                {
                    best = total;
                }
            }
        }

        return best;
    }

    public int GetAmount(Type type, bool recurse = true)
    {
        var total = 0;

        foreach (var item in FindItems(recurse))
        {
            if (type.IsInstanceOfType(item))
            {
                total += item.Amount;
            }
        }

        return total;
    }

    public int GetAmount(Type[] types, bool recurse = true)
    {
        var total = 0;

        foreach (var item in FindItems(recurse))
        {
            if (item.InTypeList(types))
            {
                total += item.Amount;
            }
        }

        return total;
    }
    public Item FindItemByType(Type type, bool recurse = true)
    {
        foreach (var item in FindItems(recurse))
        {
            if (type.IsInstanceOfType(item))
            {
                return item;
            }
        }

        return null;
    }

    public Item FindItemByType(Type[] types, bool recurse = true)
    {
        foreach (var item in FindItems(recurse))
        {
            if (item.InTypeList(types))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    ///     Performs a Breadth-First search through all the <see cref="Item" />s and
    ///     nested <see cref="Container" />s within this <see cref="Container" />.
    /// </summary>
    /// <typeparam name="T">Type of object being searched for</typeparam>
    /// <param name="recurse">
    ///     Optional: If true, the search will recursively
    ///     check any nested <see cref="Container" />s; otherwise, nested
    ///     <see cref="Container" />s will not be searched.
    /// </param>
    /// <param name="predicate">
    ///     Optional: A predicate to check if the <see cref="Item" />
    ///     of type <typeparamref name="T" /> is the target of the search.
    /// </param>
    /// <returns>
    ///     The first <see cref="Item" /> of type <typeparamref name="T" /> that matches the optional
    ///     <paramref name="predicate" />.
    /// </returns>
    public T FindItemByType<T>(bool recurse = true, Predicate<T> predicate = null) where T : Item
    {
        foreach (var item in FindItemsByType(recurse, predicate))
        {
            return item;
        }

        return null;
    }

    [Flags]
    private enum SaveFlag : byte
    {
        None = 0x00000000,
        MaxItems = 0x00000001,
        GumpID = 0x00000002,
        DropSound = 0x00000004,
        LiftOverride = 0x00000008
    }

    private struct ItemStackEntry
    {
        public readonly Item m_StackItem;
        public readonly Item m_DropItem;

        public ItemStackEntry(Item stack, Item drop)
        {
            m_StackItem = stack;
            m_DropItem = drop;
        }
    }
}

public class ContainerData
{
    private static ILogger logger = LogFactory.GetLogger(typeof(ContainerData));
    private static readonly Dictionary<int, ContainerData> m_Table;

    static ContainerData()
    {
        m_Table = new Dictionary<int, ContainerData>();

        var path = Path.Combine(Core.BaseDirectory, "Data/containers.cfg");

        if (!File.Exists(path))
        {
            Default = new ContainerData(0x3C, new Rectangle2D(44, 65, 142, 94), 0x48);
            return;
        }

        using (var reader = new StreamReader(path))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0 || line.StartsWithOrdinal("#"))
                {
                    continue;
                }

                try
                {
                    var split = line.Split('\t');

                    if (split.Length >= 3)
                    {
                        var gumpID = Utility.ToInt32(split[0]);

                        var aRect = split[1].Split(' ');
                        if (aRect.Length < 4)
                        {
                            continue;
                        }

                        var x = Utility.ToInt32(aRect[0]);
                        var y = Utility.ToInt32(aRect[1]);
                        var width = Utility.ToInt32(aRect[2]);
                        var height = Utility.ToInt32(aRect[3]);

                        var bounds = new Rectangle2D(x, y, width, height);

                        var dropSound = Utility.ToInt32(split[2]);

                        var data = new ContainerData(gumpID, bounds, dropSound);

                        Default ??= data;

                        if (split.Length >= 4)
                        {
                            var aIDs = split[3].Split(',');

                            for (var i = 0; i < aIDs.Length; i++)
                            {
                                var id = Utility.ToInt32(aIDs[i]);

                                if (m_Table.ContainsKey(id))
                                {
                                    logger.Warning("double ItemID entry in Data\\containers.cfg");
                                }
                                else
                                {
                                    m_Table[id] = data;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        Default ??= new ContainerData(0x3C, new Rectangle2D(44, 65, 142, 94), 0x48);
    }

    public ContainerData(int gumpID, Rectangle2D bounds, int dropSound)
    {
        GumpID = gumpID;
        Bounds = bounds;
        DropSound = dropSound;
    }

    public static ContainerData Default { get; set; }

    public int GumpID { get; }

    public Rectangle2D Bounds { get; }

    public int DropSound { get; }

    public static ContainerData GetData(int itemID)
    {
        m_Table.TryGetValue(itemID, out var data);
        return data ?? Default;
    }
}
