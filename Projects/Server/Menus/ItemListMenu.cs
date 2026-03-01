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

public class ItemListMenu : IMenu
{
    private static int m_NextSerial = 1;

    public ItemListMenu(string question, ItemListEntry[] entries)
    {
        Question = question.Trim();
        Entries = entries;

        Serial = m_NextSerial++;
        if (Serial == 0)
            Serial = m_NextSerial++;
        System.Console.WriteLine($"[DEBUG] Created ItemListMenu with serial={Serial}");
    }

    public string Question { get; }

    public ItemListEntry[] Entries { get; set; }

    public int Serial { get; }

    public int EntryLength => Entries.Length;

    public virtual void OnCancel(NetState state)
    {
    }

    public virtual void OnResponse(NetState state, int index)
    {
    }

    public void SendTo(NetState state)
    {
        state.AddMenu(this);
        state.SendDisplayItemListMenu(this);
    }
}
