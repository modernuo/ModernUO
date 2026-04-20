using Server.Network;

namespace Server.Menus.ItemLists;

public class ItemListEntry
{
    public ItemListEntry(string name, int itemID, int hue = 0, int craftIndex = 0)
    {
        Name = name?.Trim() ?? "";
        ItemID = itemID;
        Hue = hue;
        CraftIndex = craftIndex;
    }

    public string Name { get; }

    public int ItemID { get; }

    public int Hue { get; }

    public int CraftIndex { get; }
}

public class ItemListMenu : BaseMenu
{
    public ItemListMenu(string question, ItemListEntry[] entries)
    {
        Question = question.Trim();
        Entries = entries;
    }

    public string Question { get; }

    public ItemListEntry[] Entries { get; set; }

    public override int EntryLength => Entries.Length;

    public override void SendTo(NetState state)
    {
        state.AddMenu(this);
        state.SendDisplayItemListMenu(this);
    }
}
