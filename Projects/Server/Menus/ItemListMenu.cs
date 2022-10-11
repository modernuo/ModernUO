using Server.Network;

namespace Server.Menus.ItemLists;

public class ItemListEntry
{
    public ItemListEntry(string name, int itemID, int hue = 0)
    {
        Name = name?.Trim() ?? "";
        ItemID = itemID;
        Hue = hue;
    }

    public string Name { get; }

    public int ItemID { get; }

    public int Hue { get; }
}

public class ItemListMenu : IMenu
{
    private static int m_NextSerial;

    public ItemListMenu(string question, ItemListEntry[] entries)
    {
        Question = question.Trim();
        Entries = entries;

        do
        {
            Serial = m_NextSerial++;
            Serial &= 0x7FFFFFFF;
        } while (Serial == 0);

        Serial = (int)((uint)Serial | 0x80000000);
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
