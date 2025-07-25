using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.BulkOrders;
using Server.Ethics;
using Server.Items;
using Server.Multis;
using Server.Targeting;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class VendorBackpack : Backpack
{
    public VendorBackpack() => Layer = Layer.Backpack;

    public override double DefaultWeight => 1.0;
    public override int DefaultMaxWeight => 0;

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
    {
        if (!base.CheckHold(m, item, message, checkItems, plusItems, plusWeight))
        {
            return false;
        }

        if (Ethic.IsImbued(item, true))
        {
            if (message)
            {
                m.SendMessage("Imbued items may not be sold here.");
            }

            return false;
        }

        if (!BaseHouse.NewVendorSystem && Parent is PlayerVendor vendor)
        {
            var house = vendor.House;

            if (house?.IsAosRules == true && !house.CheckAosStorage(1 + item.TotalItems + plusItems))
            {
                if (message)
                {
                    m.SendLocalizedMessage(1061839); // This action would exceed the secure storage limit of the house.
                }

                return false;
            }
        }

        return true;
    }

    public override bool IsAccessibleTo(Mobile m) => true;

    public override bool CheckItemUse(Mobile from, Item item)
    {
        if (!base.CheckItemUse(from, item))
        {
            return false;
        }

        if (item is Container or BulkOrderBook)
        {
            return true;
        }

        from.SendLocalizedMessage(500447); // That is not accessible.
        return false;
    }

    public override bool CheckTarget(Mobile from, Target targ, object targeted) =>
        base.CheckTarget(from, targ, targeted) &&
        (from.AccessLevel >= AccessLevel.GameMaster ||
         targ.GetType().IsDefined(typeof(PlayerVendorTargetAttribute), false));

    public override void GetChildContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list, Item item)
    {
        base.GetChildContextMenuEntries(from, ref list, item);

        if (RootParent is not PlayerVendor pv || pv.IsOwner(from))
        {
            return;
        }

        var vi = pv.GetVendorItem(item);

        if (vi != null)
        {
            list.Add(new BuyEntry());
        }
    }

    public override void GetChildNameProperties(IPropertyList list, Item item)
    {
        base.GetChildNameProperties(list, item);

        var pv = RootParent as PlayerVendor;

        var vi = pv?.GetVendorItem(item);

        if (vi == null)
        {
            return;
        }

        if (!vi.IsForSale)
        {
            list.Add(1043307); // Price: Not for sale.
        }
        else if (vi.IsForFree)
        {
            list.Add(1043306); // Price: FREE!
        }
        else
        {
            list.Add(1043304, vi.FormattedPrice); // Price: ~1_COST~
        }
    }

    public override void GetChildProperties(IPropertyList list, Item item)
    {
        base.GetChildProperties(list, item);

        var pv = RootParent as PlayerVendor;

        var vi = pv?.GetVendorItem(item);

        if (vi?.Description?.Length > 0)
        {
            list.Add(1043305, vi.Description); // <br>Seller's Description:<br>"~1_DESC~"
        }
    }

    public override void OnSingleClickContained(Mobile from, Item item)
    {
        if (RootParent is PlayerVendor vendor)
        {
            var vi = vendor.GetVendorItem(item);

            if (vi != null)
            {
                if (!vi.IsForSale)
                {
                    item.LabelTo(from, 1043307); // Price: Not for sale.
                }
                else if (vi.IsForFree)
                {
                    item.LabelTo(from, 1043306); // Price: FREE!
                }
                else
                {
                    item.LabelTo(from, 1043304, vi.FormattedPrice); // Price: ~1_COST~
                }

                if (!string.IsNullOrEmpty(vi.Description))
                {
                    item.LabelTo(from, $"Description: {vi.Description}");
                }
            }
        }

        base.OnSingleClickContained(from, item);
    }

    private class BuyEntry : ContextMenuEntry
    {
        public BuyEntry() : base(6103)
        {
        }

        public override bool NonLocalUse => true;

        public override void OnClick(Mobile from, IEntity target)
        {
            if (target is Item { Deleted: false } item)
            {
                PlayerVendor.TryToBuy(item, from);
            }
        }
    }
}
