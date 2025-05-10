using System;
using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Network;
using Server.Menus.ItemLists;

namespace Server.Engines.Craft.T2A;

public class AlchemyMenu : ItemListMenu
{
    private readonly Mobile m_Mobile;
    private string m_IsFrom;
    private readonly BaseTool m_Tool;
    private readonly ItemListEntry[] m_Entries;

    public AlchemyMenu(Mobile m, ItemListEntry[] entries, string isFrom, BaseTool tool)
        : base("Choose an item.", entries)
    {
        m_Mobile = m;
        m_IsFrom = isFrom;
        m_Tool = tool;
        m_Entries = entries;
    }

    public static ItemListEntry[] Main(Mobile from)
    {
        var entries = new ItemListEntry[8];
        entries[0] = new ItemListEntry("Refresh", 0xF0B, 0, 0);
        entries[1] = new ItemListEntry("Agility", 0xF08, 0, 1);
        entries[2] = new ItemListEntry("Night Sight", 0xF06, 0, 2);
        entries[3] = new ItemListEntry("Heal", 0xF0C, 0, 3);
        entries[4] = new ItemListEntry("Strength", 0xF09, 0, 4);
        entries[5] = new ItemListEntry("Poison", 0xF0A, 0, 5);
        entries[6] = new ItemListEntry("Cure", 0xF07, 0, 6);
        entries[7] = new ItemListEntry("Explosion", 0xF0D, 0, 7);
        return entries;
    }

    public static ItemListEntry[] Refresh(Mobile from) => UserAlchemyMenu.Refresh(from);
    public static ItemListEntry[] Agility(Mobile from) => UserAlchemyMenu.Agility(from);
    public static ItemListEntry[] NightSight(Mobile from) => UserAlchemyMenu.NightSight(from);
    public static ItemListEntry[] Heal(Mobile from) => UserAlchemyMenu.Heal(from);
    public static ItemListEntry[] Strength(Mobile from) => UserAlchemyMenu.Strength(from);
    public static ItemListEntry[] Poison(Mobile from) => UserAlchemyMenu.Poison(from);
    public static ItemListEntry[] Cure(Mobile from) => UserAlchemyMenu.Cure(from);
    public static ItemListEntry[] Explosion(Mobile from) => UserAlchemyMenu.Explosion(from);

    public override void OnResponse(NetState state, int index)
    {
        if (index < 0 || index >= m_Entries.Length)
        {
            m_Mobile.SendMenu(new AlchemyMenu(m_Mobile, AlchemyMenu.Main(m_Mobile), "Main", m_Tool));
            m_Mobile.SendAsciiMessage("You don't have the required skills to attempt this item.");
            return;
        }
        var from = m_Mobile;
        var craftIndex = m_Entries[index].CraftIndex;
        if (m_IsFrom == "Main")
        {
            ItemListEntry[] submenu = null;
            string submenuName = null;
            switch (craftIndex)
            {
                case 0: submenu = Refresh(from); submenuName = "Refresh"; break;
                case 1: submenu = Agility(from); submenuName = "Agility"; break;
                case 2: submenu = NightSight(from); submenuName = "NightSight"; break;
                case 3: submenu = Heal(from); submenuName = "Heal"; break;
                case 4: submenu = Strength(from); submenuName = "Strength"; break;
                case 5: submenu = Poison(from); submenuName = "Poison"; break;
                case 6: submenu = Cure(from); submenuName = "Cure"; break;
                case 7: submenu = Explosion(from); submenuName = "Explosion"; break;
            }
            if (submenu == null || submenu.Length == 0)
            {
                from.SendAsciiMessage("You don't have the required skills to attempt this item.");
                from.SendMenu(new AlchemyMenu(from, Main(from), "Main", m_Tool));
                return;
            }
            from.SendMenu(new AlchemyMenu(from, submenu, submenuName, m_Tool));
            return;
        }
        else
        {
            UserAlchemyMenu.OnResponse(from, m_IsFrom, m_Entries, index, m_Tool);
        }
    }
}

// Helper class to encapsulate the user's provided item list logic
public static class UserAlchemyMenu
{
    // --- Begin pasted methods from user ---
    public static ItemListEntry[] Refresh(Mobile from)
    {
        Type type;
        int itemid;
        string name;
        Item item;
        CraftRes craftResource;
        bool allRequiredSkills = true;
        double chance;
        int ResAmount;
        int missing = 0;

        ItemListEntry[] entries = new ItemListEntry[2];
        
        for (int i = 0; i < 2; ++i)
        {
            chance = DefAlchemy.CraftSystem.CraftItems[i].GetSuccessChance(from, typeof(BlackPearl), DefAlchemy.CraftSystem, false, out allRequiredSkills);

            if ((chance > 0) && (from.Backpack.GetAmount(typeof(BlackPearl)) >= DefAlchemy.CraftSystem.CraftItems[i].Resources[0].Amount))
            {
                type = DefAlchemy.CraftSystem.CraftItems[i].ItemType;
                craftResource = DefAlchemy.CraftSystem.CraftItems[i].Resources[0];

                item = null;
                try { item = Activator.CreateInstance(type) as Item; }
                catch { }
                name = item.GetType().Name;
                name = name.Replace("hP", "h P");
                name = name.Replace("lR", "l R");
                name = name.Replace(" Potion", "");
                itemid = item.ItemID;

                entries[i-missing] = new ItemListEntry(name, itemid, 0, i);

                if (item != null)
                    item.Delete();
            }
            else
                missing++;
        }
        Array.Resize(ref entries, (entries.Length - missing));
        return entries;
    }

    public static ItemListEntry[] Agility(Mobile from)
    {
        Type type;
        int itemid;
        string name;
        Item item;
        CraftRes craftResource;
        bool allRequiredSkills = true;
        double chance;
        int missing = 0;

        ItemListEntry[] entries = new ItemListEntry[2];

        for (int i = 0; i < 2; ++i)
        {
            chance = DefAlchemy.CraftSystem.CraftItems[i+2].GetSuccessChance(from, typeof(Bloodmoss), DefAlchemy.CraftSystem, false, out allRequiredSkills);

            if ((chance > 0) && (from.Backpack.GetAmount(typeof(Bloodmoss)) >= DefAlchemy.CraftSystem.CraftItems[i+2].Resources[0].Amount))
            {
                type = DefAlchemy.CraftSystem.CraftItems[i+2].ItemType;
                craftResource = DefAlchemy.CraftSystem.CraftItems[i+2].Resources[0];

                item = null;
                try { item = Activator.CreateInstance(type) as Item; }
                catch { }
                name = item.GetType().Name;
                name = name.Replace("rA", "r A");
                name = name.Replace("yP", "y P");
                name = name.Replace(" Potion", "");
                itemid = item.ItemID;

                entries[i-missing] = new ItemListEntry(name, itemid, 0, i);

                if (item != null)
                    item.Delete();
            }
            else
                missing++;
        }
        Array.Resize(ref entries, (entries.Length - missing));
        return entries;
    }

    public static ItemListEntry[] NightSight(Mobile from)
    {
        Type type;
        int itemid;
        string name;
        Item item;
        CraftRes craftResource;
        bool allRequiredSkills = true;
        double chance;
        int missing = 0;
        ItemListEntry[] entries = new ItemListEntry[1];

        for (int i = 0; i < 1; ++i)
        {
            chance = DefAlchemy.CraftSystem.CraftItems[i+4].GetSuccessChance(from, typeof(SpidersSilk), DefAlchemy.CraftSystem, false, out allRequiredSkills);

            if ((chance > 0) && (from.Backpack.GetAmount(typeof(SpidersSilk)) >= DefAlchemy.CraftSystem.CraftItems[i+4].Resources[0].Amount))
            {
                type = DefAlchemy.CraftSystem.CraftItems[i+4].ItemType;
                craftResource = DefAlchemy.CraftSystem.CraftItems[i+4].Resources[0];

                item = null;
                try { item = Activator.CreateInstance(type) as Item; }
                catch { }
                name = item.GetType().Name;
                name = name.Replace("tP", "t P");
                name = name.Replace(" Potion", "");
                itemid = item.ItemID;

                entries[i-missing] = new ItemListEntry(name, itemid, 0, i);

                if (item != null)
                    item.Delete();
            }
            else
                missing++;
        }
        Array.Resize(ref entries, (entries.Length - missing));
        return entries;
    }

    public static ItemListEntry[] Heal(Mobile from)
    {
        Type type;
        int itemid;
        string name;
        Item item;
        CraftRes craftResource;
        bool allRequiredSkills = true;
        double chance;
        int missing = 0;

        ItemListEntry[] entries = new ItemListEntry[3];

        for (int i = 0; i < 3; ++i)
        {
            chance = DefAlchemy.CraftSystem.CraftItems[i+5].GetSuccessChance(from, typeof(Ginseng), DefAlchemy.CraftSystem, false, out allRequiredSkills);

            if ((chance > 0) && (from.Backpack.GetAmount(typeof(Ginseng)) >= DefAlchemy.CraftSystem.CraftItems[i+5].Resources[0].Amount))
            {
                type = DefAlchemy.CraftSystem.CraftItems[i+5].ItemType;
                craftResource = DefAlchemy.CraftSystem.CraftItems[i+5].Resources[0];

                item = null;
                try { item = Activator.CreateInstance(type) as Item; }
                catch { }
                name = item.GetType().Name;
                name = name.Replace("rH", "r H");
                name = name.Replace("lP", "l P");
                name = name.Replace(" Potion", "");
                itemid = item.ItemID;

                entries[i-missing] = new ItemListEntry(name, itemid, 0, i);

                if (item != null)
                    item.Delete();
            }
            else
                missing++;
        }
        Array.Resize(ref entries, (entries.Length - missing));
        return entries;
    }

    public static ItemListEntry[] Strength(Mobile from)
    {
        Type type;
        int itemid;
        string name;
        Item item;
        CraftRes craftResource;
        bool allRequiredSkills = true;
        double chance;
        int missing = 0;

        ItemListEntry[] entries = new ItemListEntry[2];

        for (int i = 0; i < 2; ++i)
        {
            chance = DefAlchemy.CraftSystem.CraftItems[i+8].GetSuccessChance(from, typeof(MandrakeRoot), DefAlchemy.CraftSystem, false, out allRequiredSkills);

            if ((chance > 0) && (from.Backpack.GetAmount(typeof(MandrakeRoot)) >= DefAlchemy.CraftSystem.CraftItems[i+8].Resources[0].Amount))
            {
                type = DefAlchemy.CraftSystem.CraftItems[i+8].ItemType;
                craftResource = DefAlchemy.CraftSystem.CraftItems[i+8].Resources[0];

                item = null;
                try { item = Activator.CreateInstance(type) as Item; }
                catch { }
                name = item.GetType().Name;
                name = name.Replace("hP", "h P");
                name = name.Replace("rS", "r S");
                name = name.Replace(" Potion", "");
                itemid = item.ItemID;

                entries[i-missing] = new ItemListEntry(name, itemid, 0, i);

                if (item != null)
                    item.Delete();
            }
            else
                missing++;
        }
        Array.Resize(ref entries, (entries.Length - missing));
        return entries;
    }

    public static ItemListEntry[] Poison(Mobile from)
    {
        Type type;
        int itemid;
        string name;
        Item item;
        CraftRes craftResource;
        bool allRequiredSkills = true;
        double chance;
        int missing = 0;

        ItemListEntry[] entries = new ItemListEntry[4];

        for (int i = 0; i < 4; ++i)
        {
            chance = DefAlchemy.CraftSystem.CraftItems[i+10].GetSuccessChance(from, typeof(Nightshade), DefAlchemy.CraftSystem, false, out allRequiredSkills);

            if ((chance > 0) && (from.Backpack.GetAmount(typeof(Nightshade)) >= DefAlchemy.CraftSystem.CraftItems[i+10].Resources[0].Amount))
            {
                type = DefAlchemy.CraftSystem.CraftItems[i+10].ItemType;
                craftResource = DefAlchemy.CraftSystem.CraftItems[i+10].Resources[0];

                item = null;
                try { item = Activator.CreateInstance(type) as Item; }
                catch { }
                name = item.GetType().Name;
                name = name.Replace("nP", "n P");
                name = name.Replace("rP", "r P");
                name = name.Replace("yP", "y P");
                name = name.Replace(" Potion", "");
                itemid = item.ItemID;

                entries[i-missing] = new ItemListEntry(name, itemid, 0, i);

                if (item != null)
                    item.Delete();
            }
            else
                missing++;
        }
        Array.Resize(ref entries, (entries.Length - missing));
        return entries;
    }

    public static ItemListEntry[] Cure(Mobile from)
    {
        Type type;
        int itemid;
        string name;
        Item item;
        CraftRes craftResource;
        bool allRequiredSkills = true;
        double chance;
        int missing = 0;

        ItemListEntry[] entries = new ItemListEntry[3];

        for (int i = 0; i < 3; ++i)
        {
            chance = DefAlchemy.CraftSystem.CraftItems[i+14].GetSuccessChance(from, typeof(Garlic), DefAlchemy.CraftSystem, false, out allRequiredSkills);

            if ((chance > 0) && (from.Backpack.GetAmount(typeof(Garlic)) >= DefAlchemy.CraftSystem.CraftItems[i+14].Resources[0].Amount))
            {
                type = DefAlchemy.CraftSystem.CraftItems[i+14].ItemType;
                craftResource = DefAlchemy.CraftSystem.CraftItems[i+14].Resources[0];

                item = null;
                try { item = Activator.CreateInstance(type) as Item; }
                catch { }
                name = item.GetType().Name;
                name = name.Replace("eP", "e P");
                name = name.Replace("rC", "r C");
                name = name.Replace(" Potion", "");
                itemid = item.ItemID;

                entries[i-missing] = new ItemListEntry(name, itemid, 0, i);

                if (item != null)
                    item.Delete();
            }
            else
                missing++;
        }
        Array.Resize(ref entries, (entries.Length - missing));
        return entries;
    }

    public static ItemListEntry[] Explosion(Mobile from)
    {
        Type type;
        int itemid;
        string name;
        Item item;
        CraftRes craftResource;
        bool allRequiredSkills = true;
        double chance;
        int missing = 0;

        ItemListEntry[] entries = new ItemListEntry[3];

        for (int i = 0; i < 3; ++i)
        {
            chance = DefAlchemy.CraftSystem.CraftItems[i+17].GetSuccessChance(from, typeof(SulfurousAsh), DefAlchemy.CraftSystem, false, out allRequiredSkills);

            if ((chance > 0) && (from.Backpack.GetAmount(typeof(SulfurousAsh)) >= DefAlchemy.CraftSystem.CraftItems[i+17].Resources[0].Amount))
            {
                type = DefAlchemy.CraftSystem.CraftItems[i+17].ItemType;
                craftResource = DefAlchemy.CraftSystem.CraftItems[i+17].Resources[0];

                item = null;
                try { item = Activator.CreateInstance(type) as Item; }
                catch { }
                name = item.GetType().Name;
                name = name.Replace("nP", "n P");
                name = name.Replace("rE", "r E");
                name = name.Replace(" Potion", "");
                itemid = item.ItemID;

                entries[i-missing] = new ItemListEntry(name, itemid, 0, i);

                if (item != null)
                    item.Delete();
            }
            else
                missing++;
        }

        Array.Resize(ref entries, (entries.Length - missing));
        return entries;
    }

    public static void OnResponse(Mobile m_Mobile, string IsFrom, ItemListEntry[] m_Entries, int index, BaseTool m_Tool)
    {
        if (m_Mobile.Backpack.GetAmount(typeof(Bottle)) == 0)
        {
            m_Mobile.SendAsciiMessage("You need an empty bottle to make a potion.");
            return;
        }

        if (IsFrom == "Refresh")
        {
            var itemDef = DefAlchemy.CraftSystem.CraftItems[m_Entries[index].CraftIndex];
            int num = DefAlchemy.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendLocalizedMessage(num);
                m_Mobile.SendMenu(new AlchemyMenu(m_Mobile, Refresh(m_Mobile), "Refresh", m_Tool));
                return;
            }
            DefAlchemy.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, typeof(BlackPearl), m_Tool, itemDef);
        }
        else if (IsFrom == "Agility")
        {
            var itemDef = DefAlchemy.CraftSystem.CraftItems[m_Entries[index].CraftIndex+2];
            int num = DefAlchemy.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendLocalizedMessage(num);
                m_Mobile.SendMenu(new AlchemyMenu(m_Mobile, Agility(m_Mobile), "Agility", m_Tool));
                return;
            }
            DefAlchemy.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, typeof(Bloodmoss), m_Tool, itemDef);
        }
        else if (IsFrom == "NightSight")
        {
            var itemDef = DefAlchemy.CraftSystem.CraftItems[m_Entries[index].CraftIndex+4];
            int num = DefAlchemy.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendLocalizedMessage(num);
                m_Mobile.SendMenu(new AlchemyMenu(m_Mobile, NightSight(m_Mobile), "NightSight", m_Tool));
                return;
            }
            DefAlchemy.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, typeof(SpidersSilk), m_Tool, itemDef);
        }
        else if (IsFrom == "Heal")
        {
            var itemDef = DefAlchemy.CraftSystem.CraftItems[m_Entries[index].CraftIndex+5];
            int num = DefAlchemy.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendLocalizedMessage(num);
                m_Mobile.SendMenu(new AlchemyMenu(m_Mobile, Heal(m_Mobile), "Heal", m_Tool));
                return;
            }
            DefAlchemy.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, typeof(Ginseng), m_Tool, itemDef);
        }
        else if (IsFrom == "Strength")
        {
            var itemDef = DefAlchemy.CraftSystem.CraftItems[m_Entries[index].CraftIndex+8];
            int num = DefAlchemy.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendLocalizedMessage(num);
                m_Mobile.SendMenu(new AlchemyMenu(m_Mobile, Strength(m_Mobile), "Strength", m_Tool));
                return;
            }
            DefAlchemy.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, typeof(MandrakeRoot), m_Tool, itemDef);
        }
        else if (IsFrom == "Poison")
        {
            var itemDef = DefAlchemy.CraftSystem.CraftItems[m_Entries[index].CraftIndex+10];
            int num = DefAlchemy.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendLocalizedMessage(num);
                m_Mobile.SendMenu(new AlchemyMenu(m_Mobile, Poison(m_Mobile), "Poison", m_Tool));
                return;
            }
            DefAlchemy.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, typeof(Nightshade), m_Tool, itemDef);
        }
        else if (IsFrom == "Cure")
        {
            var itemDef = DefAlchemy.CraftSystem.CraftItems[m_Entries[index].CraftIndex+14];
            int num = DefAlchemy.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendLocalizedMessage(num);
                m_Mobile.SendMenu(new AlchemyMenu(m_Mobile, Cure(m_Mobile), "Cure", m_Tool));
                return;
            }
            DefAlchemy.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, typeof(Garlic), m_Tool, itemDef);
        }
        else if (IsFrom == "Explosion")
        {
            var itemDef = DefAlchemy.CraftSystem.CraftItems[m_Entries[index].CraftIndex+17];
            int num = DefAlchemy.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendLocalizedMessage(num);
                m_Mobile.SendMenu(new AlchemyMenu(m_Mobile, Explosion(m_Mobile), "Explosion", m_Tool));
                return;
            }
            DefAlchemy.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, typeof(SulfurousAsh), m_Tool, itemDef);
        }
    }
    // --- End pasted methods from user ---
} 