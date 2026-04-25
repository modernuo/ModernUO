/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Container.cs                                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Server.Collections;
using Server.Logging;
using Server.Network;
using ModernUO.Serialization;

namespace Server.Items;

public delegate void OnItemConsumed(Item item, int amount);

public delegate int CheckItemGroup(Item a, Item b);

public delegate void ContainerSnoopHandler(Container cont, Mobile from);

[SerializationGenerator(0, false)]
public partial class Container : Item
{
    private ContainerData _containerData;

    internal List<Item> _items;

    private int _totalGold;

    private int _totalItems;
    private int _totalWeight;
    internal int _version;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _liftOverride;

    [SerializableFieldSaveFlag(3)]
    private bool ShouldSerializeLiftOverride() => _liftOverride;

    public Container(int itemID) : base(itemID)
    {
        _gumpID = -1;
        _dropSound = -1;
        _maxItems = -1;
    }

    public static ContainerSnoopHandler SnoopHandler { get; set; }

    public ContainerData ContainerData
    {
        get => _containerData ?? UpdateContainerData();
        set => _containerData = value;
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
                _containerData = null;
            }
        }
    }

    [EncodedInt]
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxItems
    {
        get => _maxItems == -1 ? DefaultMaxItems : _maxItems;
        set
        {
            _maxItems = value;
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [SerializableFieldSaveFlag(0)]
    private bool ShouldSerializeMaxItems() => _maxItems != -1;

    [SerializableFieldDefault(0)]
    private int MaxItemsDefaultValue() => -1;

    [EncodedInt]
    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int GumpID
    {
        get => _gumpID == -1 ? DefaultGumpID : _gumpID;
        set
        {
            _gumpID = value;
            this.MarkDirty();
        }
    }

    [SerializableFieldSaveFlag(1)]
    private bool ShouldSerializeGumpId() => _gumpID != -1;

    [SerializableFieldDefault(1)]
    private int GumpIDDefaultValue() => -1;

    [EncodedInt]
    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int DropSound
    {
        get => _dropSound == -1 ? DefaultDropSound : _dropSound;
        set
        {
            _dropSound = value;
            this.MarkDirty();
        }
    }

    [SerializableFieldSaveFlag(2)]
    private bool ShouldSerializeDropSound() => _dropSound != -1;

    [SerializableFieldDefault(2)]
    private int DropSoundDefaultValue() => -1;

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual int MaxWeight => Parent is Container { MaxWeight: 0 } ? 0 : DefaultMaxWeight;

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

    public virtual ContainerData UpdateContainerData() => ContainerData = ContainerData.GetData(ItemID);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckHold(Mobile m, Item item, bool message) => CheckHold(m, item, message, true, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        while (parent is Item parentItem)
        {
            if (parentItem is Container container)
            {
                return container.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
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

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        UpdateContainerData();
    }

    public override int GetTotal(TotalType type)
    {
        return type switch
        {
            TotalType.Gold => _totalGold,
            TotalType.Items => _totalItems,
            TotalType.Weight => _totalWeight,
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
                    {
                        _totalGold += delta;
                        break;
                    }

                case TotalType.Items:
                    {
                        _totalItems += delta;
                        InvalidateProperties();
                        break;
                    }

                case TotalType.Weight:
                    {
                        _totalWeight += delta;
                        InvalidateProperties();
                        break;
                    }
            }
        }

        base.UpdateTotal(sender, type, delta);
    }

    public override void UpdateTotals()
    {
        _totalGold = 0;
        _totalItems = 0;
        _totalWeight = 0;

        var items = _items;

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

            _totalGold += item.TotalGold;
            _totalItems += item.TotalItems + 1;
            _totalWeight += item.TotalWeight + item.PileWeight;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void OnItemAdded(Item item)
    {
        base.OnItemAdded(item);
        _version++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);
        _version++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public virtual void Destroy()
    {
        var loc = GetWorldLocation();
        var map = Map;
        var items = Items;

        for (var i = items.Count - 1; i >= 0; --i)
        {
            var item = items[i];
            item.SetLastMoved();
            item.MoveToWorld(loc, map);
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
                var items = Items;
                for (var i = 0; i < items.Count; ++i)
                {
                    ns.SendOPLInfo(items[i]);
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
            Openers ??= [];

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

    public int ConsumeTotalGrouped(
        ReadOnlySpan<Type> types, ReadOnlySpan<int> amounts,
        bool recurse, OnItemConsumed callback, CheckItemGroup grouper)
    {
        if (types.Length != amounts.Length)
        {
            throw new ArgumentException("length of types and amounts must match");
        }
        ArgumentNullException.ThrowIfNull(grouper);

        // Phase 1: every slot must have a single group with sum >= amount
        // before any consume happens (preserves all-or-nothing semantics).
        for (var i = 0; i < types.Length; i++)
        {
            using var items = ListItemsByType(types[i], recurse);
            if (!TryFindGroupMeetingAmount(items, amounts[i], grouper, out _, out _))
            {
                return i;
            }
        }

        // Phase 2: re-list and consume. Trades one extra ListItemsByType per
        // slot (small) for eliminating List<List<Item>> + Item[][] + int[]
        // grouping bridges (large). Live mutation of the container during
        // Item.Consume bumps _version, so each phase walks its own snapshot.
        for (var i = 0; i < types.Length; i++)
        {
            using var items = ListItemsByType(types[i], recurse);
            if (TryFindGroupMeetingAmount(items, amounts[i], grouper, out var start, out var len))
            {
                ConsumeSlice(items, start, len, amounts[i], callback);
            }
        }

        return -1;
    }

    public int ConsumeTotalGrouped(
        Type[][] types, ReadOnlySpan<int> amounts,
        bool recurse, OnItemConsumed callback, CheckItemGroup grouper)
    {
        if (types.Length != amounts.Length)
        {
            throw new ArgumentException("length of types and amounts must match");
        }
        ArgumentNullException.ThrowIfNull(grouper);

        for (var i = 0; i < types.Length; i++)
        {
            using var items = ListItemsByType(types[i], recurse);
            if (!TryFindGroupMeetingAmount(items, amounts[i], grouper, out _, out _))
            {
                return i;
            }
        }

        for (var i = 0; i < types.Length; i++)
        {
            using var items = ListItemsByType(types[i], recurse);
            if (TryFindGroupMeetingAmount(items, amounts[i], grouper, out var start, out var len))
            {
                ConsumeSlice(items, start, len, amounts[i], callback);
            }
        }

        return -1;
    }

    public int ConsumeTotal(
        Type[][] types, ReadOnlySpan<int> amounts,
        bool recurse = true, OnItemConsumed callback = null)
    {
        if (types.Length != amounts.Length)
        {
            throw new ArgumentException("length of types and amounts must match");
        }

        // Phase 1: validate every slot before any consume (all-or-nothing).
        for (var i = 0; i < types.Length; i++)
        {
            if (GetAmount(types[i], recurse) < amounts[i])
            {
                return i;
            }
        }

        // Phase 2: materialize per slot and consume.
        for (var i = 0; i < types.Length; i++)
        {
            using var items = ListItemsByType(types[i], recurse);
            ConsumeSlice(items, 0, items.Count, amounts[i], callback);
        }

        return -1;
    }

    public int ConsumeTotal(
        ReadOnlySpan<Type> types, ReadOnlySpan<int> amounts,
        bool recurse = true, OnItemConsumed callback = null)
    {
        if (types.Length != amounts.Length)
        {
            throw new ArgumentException("length of types and amounts must match");
        }

        for (var i = 0; i < types.Length; i++)
        {
            if (GetAmount(types[i], recurse) < amounts[i])
            {
                return i;
            }
        }

        for (var i = 0; i < types.Length; i++)
        {
            using var items = ListItemsByType(types[i], recurse);
            ConsumeSlice(items, 0, items.Count, amounts[i], callback);
        }

        return -1;
    }

    public bool ConsumeTotal(Type type, int amount = 1, bool recurse = true, OnItemConsumed callback = null)
    {
        if (!HasAmount(type, amount, recurse))
        {
            return false;
        }

        using var items = ListItemsByType(type, recurse);
        ConsumeSlice(items, 0, items.Count, amount, callback);
        return true;
    }

    public int ConsumeUpTo(Type type, int amount, bool recurse = true)
    {
        var consumed = 0;

        var toDelete = PooledRefQueue<Item>.Create();

        RecurseConsumeUpTo(this, type, amount, recurse, ref consumed, ref toDelete);

        while (toDelete.Count > 0)
        {
            toDelete.Dequeue().Delete();
        }

        toDelete.Dispose();

        return consumed;
    }

    private static void RecurseConsumeUpTo(
        Item current, Type type, int amount, bool recurse, ref int consumed,
        ref PooledRefQueue<Item> toDelete
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
                RecurseConsumeUpTo(item, type, amount, true, ref consumed, ref toDelete);
            }
        }
    }

    public int GetBestGroupAmount(Type[] types, bool recurse, CheckItemGroup grouper)
    {
        ArgumentNullException.ThrowIfNull(grouper);
        using var items = ListItemsByType(types, recurse);
        return BestGroupTotal(items, grouper);
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

    // Sums Item.Amount of all items matching `type`, bailing as soon as
    // `amount` is reached. Walks the alloc-free FindItemsByType enumerator —
    // no list materialization. Used as the Phase-1 sufficiency check by the
    // ConsumeTotal overloads.
    private bool HasAmount(Type type, int amount, bool recurse)
    {
        var total = 0;
        foreach (var item in FindItemsByType(type, recurse))
        {
            total += item.Amount;
            if (total >= amount)
            {
                return true;
            }
        }
        return false;
    }

    // Walks `items` (BFS-ordered snapshot) in adjacency-based groups defined
    // by `grouper`. Returns the first group whose Amount sum is >= `amount`,
    // emitting its slice [start, start+length). Streaming, no per-group list.
    private static bool TryFindGroupMeetingAmount(
        PooledRefList<Item> items, int amount, CheckItemGroup grouper,
        out int groupStart, out int groupLength)
    {
        var i = 0;
        while (i < items.Count)
        {
            var leader = items[i];
            var start = i;
            var total = leader.Amount;
            i++;
            while (i < items.Count && grouper(leader, items[i]) == 0)
            {
                total += items[i].Amount;
                i++;
            }
            if (total >= amount)
            {
                groupStart = start;
                groupLength = i - start;
                return true;
            }
        }
        groupStart = 0;
        groupLength = 0;
        return false;
    }

    // Returns the largest group sum across `items` partitioned by `grouper`.
    // Streaming, no per-group list.
    private static int BestGroupTotal(PooledRefList<Item> items, CheckItemGroup grouper)
    {
        var best = 0;
        var i = 0;
        while (i < items.Count)
        {
            var leader = items[i];
            var total = leader.Amount;
            i++;
            while (i < items.Count && grouper(leader, items[i]) == 0)
            {
                total += items[i].Amount;
                i++;
            }
            if (total > best)
            {
                best = total;
            }
        }
        return best;
    }

    // Consumes `need` units from the slice [start, start+length) of `items`,
    // firing `callback` once per item touched with the actual delta. Items
    // with Amount <= delta are deleted via Item.Consume.
    private static void ConsumeSlice(PooledRefList<Item> items, int start, int length, int need, OnItemConsumed callback)
    {
        for (var k = 0; k < length && need > 0; k++)
        {
            var item = items[start + k];
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
                return;
            }
        }
    }
}

public class ContainerData
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(ContainerData));
    private static readonly Dictionary<int, ContainerData> _table;

    static ContainerData()
    {
        _table = new Dictionary<int, ContainerData>();

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

                                if (_table.ContainsKey(id))
                                {
                                    _logger.Warning("double ItemID entry in Data\\containers.cfg");
                                }
                                else
                                {
                                    _table[id] = data;
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
        _table.TryGetValue(itemID, out var data);
        return data ?? Default;
    }
}
