using System.Collections.Generic;

namespace Server;

public class BuyItemStateComparer : IComparer<BuyItemState>
{
    public int Compare(BuyItemState l, BuyItemState r)
    {
        if (l == null && r == null)
        {
            return 0;
        }

        if (l == null)
        {
            return -1;
        }

        if (r == null)
        {
            return 1;
        }

        return l.MySerial.CompareTo(r.MySerial);
    }
}

public class BuyItemResponse
{
    public BuyItemResponse(Serial serial, int amount)
    {
        Serial = serial;
        Amount = amount;
    }

    public Serial Serial { get; }

    public int Amount { get; }
}

public class SellItemResponse
{
    public SellItemResponse(Item i, int amount)
    {
        Item = i;
        Amount = amount;
    }

    public Item Item { get; }

    public int Amount { get; }
}

public class SellItemStateComparer : IEqualityComparer<SellItemState>
{
    public bool Equals(SellItemState l, SellItemState r) =>
        l == null && r == null || l != null && r != null && l.Item == r.Item;

    public int GetHashCode(SellItemState obj) => obj.Item.GetHashCode();
}

public class SellItemState
{
    public SellItemState(Item item, int price, string name)
    {
        Item = item;
        Price = price;
        Name = name;
    }

    public Item Item { get; }

    public int Price { get; }

    public string Name { get; }
}

public class BuyItemState
{
    public BuyItemState(string name, Serial cont, Serial serial, int price, int amount, int itemID, int hue)
    {
        Description = name;
        ContainerSerial = cont;
        MySerial = serial;
        Price = price;
        Amount = amount;
        ItemID = itemID;
        Hue = hue;
    }

    public int Price { get; }

    public Serial MySerial { get; }

    public Serial ContainerSerial { get; }

    public int ItemID { get; }

    public int Amount { get; }

    public int Hue { get; }

    public string Description { get; }
}
