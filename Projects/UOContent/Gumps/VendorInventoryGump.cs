using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Gumps;

public class VendorInventoryGump : DynamicGump
{
    private readonly BaseHouse _house;
    private readonly Mobile _from;
    private readonly VendorInventory[] _inventories;

    public override bool Singleton => true;

    private VendorInventoryGump(BaseHouse house, Mobile from) : base(50, 50)
    {
        _house = house;
        _from = from;
        _inventories = house.VendorInventories.ToArray();
    }

    public static void DisplayTo(Mobile from, BaseHouse house)
    {
        if (from?.NetState != null && house?.Deleted == false && house.VendorInventories.Count != 0)
        {
            from.SendGump(new VendorInventoryGump(house, from));
        }
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 420, 50 + 20 * _inventories.Length, 0x13BE);

        builder.AddImageTiled(10, 10, 400, 20, 0xA40);
        builder.AddHtmlLocalized(15, 10, 200, 20, 1062435, 0x7FFF); // Reclaim Vendor Inventory
        builder.AddHtmlLocalized(330, 10, 50, 20, 1062465, 0x7FFF); // Expires

        builder.AddImageTiled(10, 40, 400, 20 * _inventories.Length, 0xA40);

        for (var i = 0; i < _inventories.Length; i++)
        {
            var inventory = _inventories[i];

            var y = 40 + 20 * i;

            if (inventory.Owner == _from)
            {
                builder.AddButton(10, y, 0xFA5, 0xFA7, i + 1);
            }

            builder.AddLabel(45, y, 0x481, $"{inventory.ShopName} ({inventory.VendorName})");

            var expire = inventory.ExpireTime - Core.Now;
            var hours = (int)expire.TotalHours;

            builder.AddLabel(320, y, 0x481, $"{hours}");
            builder.AddHtmlLocalized(350, y, 50, 20, 1062466, 0x7FFF); // hour(s)
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            return;
        }

        var from = sender.Mobile;
        var sign = _house.Sign;

        if (_house.Deleted || sign?.Deleted != false || !from.CheckAlive())
        {
            return;
        }

        if (from.Map != sign.Map || !from.InRange(sign, 5))
        {
            from.SendLocalizedMessage(1062429); // You must be within five paces of the house sign to use this option.
            return;
        }

        var index = info.ButtonID - 1;
        if (index < 0 || index >= _inventories.Length)
        {
            return;
        }

        var inventory = _inventories[index];

        if (inventory.Owner != from || !_house.VendorInventories.Contains(inventory))
        {
            return;
        }

        var totalItems = 0;
        var givenToBackpack = 0;
        var givenToBankBox = 0;
        for (var i = inventory.Items.Count - 1; i >= 0; i--)
        {
            var item = inventory.Items[i];

            if (item.Deleted)
            {
                inventory.Items.RemoveAt(i);
                continue;
            }

            totalItems += 1 + item.TotalItems;

            if (from.PlaceInBackpack(item))
            {
                inventory.Items.RemoveAt(i);
                givenToBackpack += 1 + item.TotalItems;
            }
            else if (from.BankBox.TryDropItem(from, item, false))
            {
                inventory.Items.RemoveAt(i);
                givenToBankBox += 1 + item.TotalItems;
            }
        }

        // The vendor you selected had ~1_COUNT~ items in its inventory, and ~2_AMOUNT~ gold in its account.
        from.SendLocalizedMessage(1062436, $"{totalItems}\t{inventory.Gold}");

        var givenGold = Banker.DepositUpTo(from, inventory.Gold);
        inventory.Gold -= givenGold;

        // ~1_AMOUNT~ gold has been deposited into your bank box.
        from.SendLocalizedMessage(1060397, givenGold.ToString());

        // ~1_COUNT~ items have been removed from the shop inventory and placed in your backpack.
        // ~2_BANKCOUNT~ items were removed from the shop inventory and placed in your bank box.
        from.SendLocalizedMessage(1062437, $"{givenToBackpack}\t{givenToBankBox}");

        if (inventory.Gold > 0 || inventory.Items.Count > 0)
        {
            // Some of the shop inventory would not fit in your backpack or bank box.  Please free up some room and try again.
            from.SendLocalizedMessage(1062440);
        }
        else
        {
            inventory.Delete();
            from.SendLocalizedMessage(1062438); // The shop is now empty of inventory and funds, so it has been deleted.
        }
    }
}
