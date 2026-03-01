using System;
using Server.Network;
using Server.Items;
using Server.Menus.ItemLists;

namespace Server.Engines.Craft.T2A
{
    public class InscriptionMenu : ItemListMenu
    {
        private Mobile m_Mobile;
        private string IsFrom;
        private BaseTool m_Tool;
        private ItemListEntry[] m_Entries;

        public InscriptionMenu(Mobile m, ItemListEntry[] entries, string Is, BaseTool tool) : base("Choose an item.", entries)
        {
            m_Mobile = m;
            IsFrom = Is;
            m_Tool = tool;
            m_Entries = entries;
        }

        public static bool GetScroll(Mobile from, Type typeItem)
        {
            if (typeItem != null)
            {
                object o = Activator.CreateInstance(typeItem, 1);

                if (o is SpellScroll)
                {
                    SpellScroll scroll = (SpellScroll)o;
                    Spellbook book = Spellbook.Find(from, scroll.SpellID);

                    scroll.Delete();
                    return book != null && book.HasSpell(scroll.SpellID);
                }
                else if (o is Item)
                {
                    ((Item)o).Delete();
                }
            }
            return false;
        }

        public static ItemListEntry[] Main(Mobile from)
        {
            bool allRequiredSkills = true;
            ItemListEntry[] entries = new ItemListEntry[8];
            double min = 0;
            SkillName skillname = SkillName.Magery;
            Skill skill = from.Skills[skillname];
            int count;
            int missing = 0;

            count = 0;
            for (int i = 0; i < 8; i++)
            {
                if (GetScroll(from, DefInscription.CraftSystem.CraftItems[i].ItemType))
                {
                    count++;
                }
            }

            min = 100.0 / 7.0 * 0 - 20;
            if (DefInscription.CraftSystem.CraftItems[0].GetSuccessChance(from, typeof(BlackPearl), DefInscription.CraftSystem, false, out allRequiredSkills) > 0 && skill.Value >= min && count > 0)
            {
                entries[0-missing] = new ItemListEntry("First Circle", 8384);
            }
            else
            {
                missing++;
            }

            count = 0;
            for (int i = 8; i < 16; i++)
            {
                if (GetScroll(from, DefInscription.CraftSystem.CraftItems[i].ItemType))
                {
                    count++;
                }
            }

            min = 100.0 / 7.0 * 1 - 20;
            if (DefInscription.CraftSystem.CraftItems[8].GetSuccessChance(from, typeof(BlackPearl), DefInscription.CraftSystem, false, out allRequiredSkills) > 0 && skill.Value >= min && count > 0)
            {
                entries[1 - missing] = new ItemListEntry("Second Circle", 8385,0,1);
            }
            else
            {
                missing++;
            }

            count = 0;
            for (int i = 16; i < 24; i++)
            {
                if (GetScroll(from, DefInscription.CraftSystem.CraftItems[i].ItemType))
                {
                    count++;
                }
            }

            min = 100.0 / 7.0 * 2 - 20;
            if (DefInscription.CraftSystem.CraftItems[16].GetSuccessChance(from, typeof(BlackPearl), DefInscription.CraftSystem, false, out allRequiredSkills) > 0 && skill.Value >= min && count > 0)
            {
                entries[2 - missing] = new ItemListEntry("Third Circle", 8386,0,2);
            }
            else
            {
                missing++;
            }

            count = 0;
            for (int i = 24; i < 32; i++)
            {
                if (GetScroll(from, DefInscription.CraftSystem.CraftItems[i].ItemType))
                {
                    count++;
                }
            }

            min = 100.0 / 7.0 * 3 - 20;
            if (DefInscription.CraftSystem.CraftItems[24].GetSuccessChance(from, typeof(BlackPearl), DefInscription.CraftSystem, false, out allRequiredSkills) > 0 && skill.Value >= min && count > 0)
            {
                entries[3 - missing] = new ItemListEntry("Forth Circle", 8387,0,3);
            }
            else
            {
                missing++;
            }

            count = 0;
            for (int i = 32; i < 40; i++)
            {
                if (GetScroll(from, DefInscription.CraftSystem.CraftItems[i].ItemType))
                {
                    count++;
                }
            }

            min = 100.0 / 7.0 * 4 - 20;
            if (DefInscription.CraftSystem.CraftItems[32].GetSuccessChance(from, typeof(BlackPearl), DefInscription.CraftSystem, false, out allRequiredSkills) > 0 && skill.Value >= min && count > 0)
            {
                entries[4 - missing] = new ItemListEntry("Fifth Circle", 8388,0,4);
            }
            else
            {
                missing++;
            }

            count = 0;
            for (int i = 40; i < 48; i++)
            {
                if (GetScroll(from, DefInscription.CraftSystem.CraftItems[i].ItemType))
                {
                    count++;
                }
            }

            min = 100.0 / 7.0 * 5 - 20;
            if (DefInscription.CraftSystem.CraftItems[40].GetSuccessChance(from, typeof(BlackPearl), DefInscription.CraftSystem, false, out allRequiredSkills) > 0 && skill.Value >= min && count > 0)
            {
                entries[5 - missing] = new ItemListEntry("Sixth Circle", 8389,0,5);
            }
            else
            {
                missing++;
            }

            count = 0;
            for (int i = 48; i < 56; i++)
            {
                if (GetScroll(from, DefInscription.CraftSystem.CraftItems[i].ItemType))
                {
                    count++;
                }
            }

            min = 100.0 / 7.0 * 6 - 20;
            if (DefInscription.CraftSystem.CraftItems[48].GetSuccessChance(from, typeof(BlackPearl), DefInscription.CraftSystem, false, out allRequiredSkills) > 0 && skill.Value >= min && count > 0)
            {
                entries[6 - missing] = new ItemListEntry("Seventh Circle", 8390,0,6);
            }
            else
            {
                missing++;
            }

            count = 0;
            for (int i = 56; i < 64; i++)
            {
                if (GetScroll(from, DefInscription.CraftSystem.CraftItems[i].ItemType))
                {
                    count++;
                }
            }

            min = 100.0 / 7.0 * 7 - 20;
            if (DefInscription.CraftSystem.CraftItems[56].GetSuccessChance(from, typeof(BlackPearl), DefInscription.CraftSystem, false, out allRequiredSkills) > 0 && skill.Value >= min && count > 0)
            {
                entries[7 - missing] = new ItemListEntry("Eighth Circle", 8391,0,7);
            }
            else
            {
                missing++;
            }

            Array.Resize(ref entries, entries.Length - missing);
            return entries;
        }

        public static ItemListEntry[] First(Mobile from)
        {
            Type type;
            string name;
            Item item;
            CraftRes craftResource;
            bool allRequiredSkills = true;
            double chance;
            bool hasres = true;
            int missing = 0;

            ItemListEntry[] entries = new ItemListEntry[8];

            for (int i = 0; i < 8; ++i)
            {
                type = DefInscription.CraftSystem.CraftItems[i].ItemType;
                chance = DefInscription.CraftSystem.CraftItems[i].GetSuccessChance(from, DefInscription.CraftSystem.CraftItems[i].Resources[0].ItemType, DefInscription.CraftSystem, false, out allRequiredSkills);
                craftResource = DefInscription.CraftSystem.CraftItems[i].Resources[0];
                hasres = true;

                if (chance > 0 && hasres && GetScroll(from, DefInscription.CraftSystem.CraftItems[i].ItemType))
                {
                    item = null;
                    try { item = Activator.CreateInstance(type, 1) as Item; }
                    catch { }
                    name = item.GetType().Name;
                    name = name.Replace("Scroll", "");
                    entries[i-missing] = new ItemListEntry(string.Format("{0}", name), 8320 + i, 0, i);
                    if (item != null)
                    {
                        item.Delete();
                    }
                }
                else
                {
                    missing++;
                }
            }
            Array.Resize(ref entries, entries.Length - missing);
            return entries;
        }
        public static ItemListEntry[] Second(Mobile from)
        {
            Type type;
            string name;
            Item item;
            CraftRes craftResource;
            bool allRequiredSkills = true;
            double chance;
            bool hasres = true;
            int missing = 0;

            ItemListEntry[] entries = new ItemListEntry[8];

            for (int i = 0; i < 8; ++i)
            {
                type = DefInscription.CraftSystem.CraftItems[i+8].ItemType;
                chance = DefInscription.CraftSystem.CraftItems[i+8].GetSuccessChance(from, DefInscription.CraftSystem.CraftItems[i+8].Resources[0].ItemType, DefInscription.CraftSystem, false, out allRequiredSkills);
                craftResource = DefInscription.CraftSystem.CraftItems[i+8].Resources[0];
                hasres = true;

                if (chance > 0 && hasres && GetScroll(from, DefInscription.CraftSystem.CraftItems[i+8].ItemType))
                {
                    item = null;
                    try { item = Activator.CreateInstance(type, 1) as Item; }
                    catch { }
                    name = item.GetType().Name;
                    name = name.Replace("Scroll", "");
                    entries[i-missing] = new ItemListEntry(string.Format("{0}", name), 8320 + i+8, 0, i);
                    if (item != null)
                    {
                        item.Delete();
                    }
                }
                else
                {
                    missing++;
                }
            }
            Array.Resize(ref entries, entries.Length - missing);
            return entries;
        }
        public static ItemListEntry[] Third(Mobile from)
        {
            Type type;
            string name;
            Item item;
            CraftRes craftResource;
            bool allRequiredSkills = true;
            double chance;
            bool hasres = true;
            int missing = 0;

            ItemListEntry[] entries = new ItemListEntry[8];

            for (int i = 0; i < 8; ++i)
            {
                type = DefInscription.CraftSystem.CraftItems[i + 16].ItemType;
                chance = DefInscription.CraftSystem.CraftItems[i + 16].GetSuccessChance(from, DefInscription.CraftSystem.CraftItems[i + 16].Resources[0].ItemType, DefInscription.CraftSystem, false, out allRequiredSkills);
                craftResource = DefInscription.CraftSystem.CraftItems[i + 16].Resources[0];
                hasres = true;

                if (chance > 0 && hasres && GetScroll(from, DefInscription.CraftSystem.CraftItems[i + 16].ItemType))
                {
                    item = null;
                    try { item = Activator.CreateInstance(type, 1) as Item; }
                    catch { }
                    name = item.GetType().Name;
                    name = name.Replace("Scroll", "");
                    entries[i-missing] = new ItemListEntry(string.Format("{0}", name), 8320 + i + 16, 0, i);
                    if (item != null)
                    {
                        item.Delete();
                    }
                }
                else
                {
                    missing++;
                }
            }
            Array.Resize(ref entries, entries.Length - missing);
            return entries;
        }
        public static ItemListEntry[] Fourth(Mobile from)
        {
            Type type;
            string name;
            Item item;
            CraftRes craftResource;
            bool allRequiredSkills = true;
            double chance;
            bool hasres = true;
            int missing = 0;

            ItemListEntry[] entries = new ItemListEntry[8];

            for (int i = 0; i < 8; ++i)
            {
                type = DefInscription.CraftSystem.CraftItems[i + 24].ItemType;
                chance = DefInscription.CraftSystem.CraftItems[i + 24].GetSuccessChance(from, DefInscription.CraftSystem.CraftItems[i + 24].Resources[0].ItemType, DefInscription.CraftSystem, false, out allRequiredSkills);
                craftResource = DefInscription.CraftSystem.CraftItems[i + 24].Resources[0];
                hasres = true;

                if (chance > 0 && hasres && GetScroll(from, DefInscription.CraftSystem.CraftItems[i + 24].ItemType))
                {
                    item = null;
                    try { item = Activator.CreateInstance(type, 1) as Item; }
                    catch { }
                    name = item.GetType().Name;
                    name = name.Replace("Scroll", "");
                    entries[i-missing] = new ItemListEntry(string.Format("{0}", name), 8320 + i + 24, 0, i);
                    if (item != null)
                    {
                        item.Delete();
                    }
                }
                else
                {
                    missing++;
                }
            }
            Array.Resize(ref entries, entries.Length - missing);
            return entries;
        }
        public static ItemListEntry[] Fifth(Mobile from)
        {
            Type type;
            string name;
            Item item;
            CraftRes craftResource;
            bool allRequiredSkills = true;
            double chance;
            bool hasres = true;
            int missing = 0;

            ItemListEntry[] entries = new ItemListEntry[8];

            for (int i = 0; i < 8; ++i)
            {
                type = DefInscription.CraftSystem.CraftItems[i + 32].ItemType;
                chance = DefInscription.CraftSystem.CraftItems[i + 32].GetSuccessChance(from, DefInscription.CraftSystem.CraftItems[i + 32].Resources[0].ItemType, DefInscription.CraftSystem, false, out allRequiredSkills);
                craftResource = DefInscription.CraftSystem.CraftItems[i + 32].Resources[0];
                hasres = true;

                if (chance > 0 && hasres && GetScroll(from, DefInscription.CraftSystem.CraftItems[i + 32].ItemType))
                {
                    item = null;
                    try { item = Activator.CreateInstance(type, 1) as Item; }
                    catch { }
                    name = item.GetType().Name;
                    name = name.Replace("Scroll", "");
                    entries[i-missing] = new ItemListEntry(string.Format("{0}", name), 8320 + i + 32, 0, i);
                    if (item != null)
                    {
                        item.Delete();
                    }
                }
                else
                {
                    missing++;
                }
            }
            Array.Resize(ref entries, entries.Length - missing);
            return entries;
        }
        public static ItemListEntry[] Sixth(Mobile from)
        {
            Type type;
            string name;
            Item item;
            CraftRes craftResource;
            bool allRequiredSkills = true;
            double chance;
            bool hasres = true;
            int missing = 0;

            ItemListEntry[] entries = new ItemListEntry[8];

            for (int i = 0; i < 8; ++i)
            {
                type = DefInscription.CraftSystem.CraftItems[i + 40].ItemType;
                chance = DefInscription.CraftSystem.CraftItems[i + 40].GetSuccessChance(from, DefInscription.CraftSystem.CraftItems[i + 40].Resources[0].ItemType, DefInscription.CraftSystem, false, out allRequiredSkills);
                craftResource = DefInscription.CraftSystem.CraftItems[i + 40].Resources[0];
                hasres = true;

                if (chance > 0 && hasres && GetScroll(from, DefInscription.CraftSystem.CraftItems[i + 40].ItemType))
                {
                    item = null;
                    try { item = Activator.CreateInstance(type, 1) as Item; }
                    catch { }
                    name = item.GetType().Name;
                    name = name.Replace("Scroll", "");
                    entries[i-missing] = new ItemListEntry(string.Format("{0}", name), 8320 + i + 40, 0, i);
                    if (item != null)
                    {
                        item.Delete();
                    }
                }
                else
                {
                    missing++;
                }
            }
            Array.Resize(ref entries, entries.Length - missing);
            return entries;
        }
        public static ItemListEntry[] Seventh(Mobile from)
        {
            Type type;
            string name;
            Item item;
            CraftRes craftResource;
            bool allRequiredSkills = true;
            double chance;
            bool hasres = true;
            int missing = 0;

            ItemListEntry[] entries = new ItemListEntry[8];

            for (int i = 0; i < 8; ++i)
            {
                type = DefInscription.CraftSystem.CraftItems[i + 48].ItemType;
                chance = DefInscription.CraftSystem.CraftItems[i + 48].GetSuccessChance(from, DefInscription.CraftSystem.CraftItems[i + 48].Resources[0].ItemType, DefInscription.CraftSystem, false, out allRequiredSkills);
                craftResource = DefInscription.CraftSystem.CraftItems[i + 48].Resources[0];
                hasres = true;

                if (chance > 0 && hasres && GetScroll(from, DefInscription.CraftSystem.CraftItems[i + 48].ItemType))
                {
                    item = null;
                    try { item = Activator.CreateInstance(type, 1) as Item; }
                    catch { }
                    name = item.GetType().Name;
                    name = name.Replace("Scroll", "");
                    entries[i-missing] = new ItemListEntry(string.Format("{0}", name), 8320 + i + 48, 0, i);
                    if (item != null)
                    {
                        item.Delete();
                    }
                }
                else
                {
                    missing++;
                }
            }
            Array.Resize(ref entries, entries.Length - missing);
            return entries;
        }
        public static ItemListEntry[] Eighth(Mobile from)
        {
            Type type;
            string name;
            Item item;
            CraftRes craftResource;
            bool allRequiredSkills = true;
            double chance;
            bool hasres = true;
            int missing = 0;
            ItemListEntry[] entries = new ItemListEntry[8];

            for (int i = 0; i < 8; ++i)
            {
                type = DefInscription.CraftSystem.CraftItems[i + 56].ItemType;
                chance = DefInscription.CraftSystem.CraftItems[i + 56].GetSuccessChance(from, DefInscription.CraftSystem.CraftItems[i + 56].Resources[0].ItemType, DefInscription.CraftSystem, false, out allRequiredSkills);
                craftResource = DefInscription.CraftSystem.CraftItems[i + 56].Resources[0];
                hasres = true;

                if (chance > 0 && hasres && GetScroll(from, DefInscription.CraftSystem.CraftItems[i + 56].ItemType))
                {
                    item = null;
                    try { item = Activator.CreateInstance(type, 1) as Item; }
                    catch { }
                    name = item.GetType().Name;
                    name = name.Replace("Scroll", "");
                    entries[i-missing] = new ItemListEntry(string.Format("{0}", name), 8320 + i + 56, 0, i);
                    if (item != null)
                    {
                        item.Delete();
                    }
                }
                else
                {
                    missing++;
                }
            }
            Array.Resize(ref entries, entries.Length - missing);
            return entries;
        }

        public override void OnResponse(NetState state, int index)
        {
            if (m_Mobile.Backpack.GetAmount(typeof(BlankScroll)) == 0)
            {
                m_Mobile.SendAsciiMessage("You do not have enough blank scrolls to make that.");
                return;
            }

            if (IsFrom == "Main")
            {
                if (m_Entries[index].CraftIndex == 0)
                {
                    IsFrom = "First";
                    m_Mobile.SendMenu(new InscriptionMenu(m_Mobile, First(m_Mobile), IsFrom, m_Tool));
                }
                else if (m_Entries[index].CraftIndex == 1)
                {
                    IsFrom = "Second";
                    m_Mobile.SendMenu(new InscriptionMenu(m_Mobile, Second(m_Mobile), IsFrom, m_Tool));
                }
                else if (m_Entries[index].CraftIndex == 2)
                {
                    IsFrom = "Third";
                    m_Mobile.SendMenu(new InscriptionMenu(m_Mobile, Third(m_Mobile), IsFrom, m_Tool));
                }
                else if (m_Entries[index].CraftIndex == 3)
                {
                    IsFrom = "Fourth";
                    m_Mobile.SendMenu(new InscriptionMenu(m_Mobile, Fourth(m_Mobile), IsFrom, m_Tool));
                }
                else if (m_Entries[index].CraftIndex == 4)
                {
                    IsFrom = "Fifth";
                    m_Mobile.SendMenu(new InscriptionMenu(m_Mobile, Fifth(m_Mobile), IsFrom, m_Tool));
                }
                else if (m_Entries[index].CraftIndex == 5)
                {
                    IsFrom = "Sixth";
                    m_Mobile.SendMenu(new InscriptionMenu(m_Mobile, Sixth(m_Mobile), IsFrom, m_Tool));
                }
                else if (m_Entries[index].CraftIndex == 6)
                {
                    IsFrom = "Seventh";
                    m_Mobile.SendMenu(new InscriptionMenu(m_Mobile, Seventh(m_Mobile), IsFrom, m_Tool));
                }
                else if (m_Entries[index].CraftIndex == 7)
                {
                    IsFrom = "Eighth";
                    m_Mobile.SendMenu(new InscriptionMenu(m_Mobile, Eighth(m_Mobile), IsFrom, m_Tool));
                }
            }
            else if (IsFrom == "First")
            {
                Type type = null;
                CraftContext context = DefInscription.CraftSystem.GetContext(m_Mobile);
                CraftSubResCol res = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex].UseSubRes2 ? DefInscription.CraftSystem.CraftSubRes2 : DefInscription.CraftSystem.CraftSubRes;
                int resIndex = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex].UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
                if (resIndex > -1)
                {
                    type = res[resIndex].ItemType;
                }
                DefInscription.CraftSystem.CreateItem(m_Mobile, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex].ItemType, type, m_Tool, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex]);
            }
            else if (IsFrom == "Second")
            {
                Type type = null;
                CraftContext context = DefInscription.CraftSystem.GetContext(m_Mobile);
                CraftSubResCol res = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex+8].UseSubRes2 ? DefInscription.CraftSystem.CraftSubRes2 : DefInscription.CraftSystem.CraftSubRes;
                int resIndex = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex+8].UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
                if (resIndex > -1)
                {
                    type = res[resIndex].ItemType;
                }
                DefInscription.CraftSystem.CreateItem(m_Mobile, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex+8].ItemType, type, m_Tool, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex+8]);
            }
            else if (IsFrom == "Third")
            {
                Type type = null;
                CraftContext context = DefInscription.CraftSystem.GetContext(m_Mobile);
                CraftSubResCol res = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 16].UseSubRes2 ? DefInscription.CraftSystem.CraftSubRes2 : DefInscription.CraftSystem.CraftSubRes;
                int resIndex = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 16].UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
                if (resIndex > -1)
                {
                    type = res[resIndex].ItemType;
                }
                DefInscription.CraftSystem.CreateItem(m_Mobile, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 16].ItemType, type, m_Tool, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 16]);
            }
            else if (IsFrom == "Fourth")
            {
                Type type = null;
                CraftContext context = DefInscription.CraftSystem.GetContext(m_Mobile);
                CraftSubResCol res = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 24].UseSubRes2 ? DefInscription.CraftSystem.CraftSubRes2 : DefInscription.CraftSystem.CraftSubRes;
                int resIndex = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 24].UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
                if (resIndex > -1)
                {
                    type = res[resIndex].ItemType;
                }
                DefInscription.CraftSystem.CreateItem(m_Mobile, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 24].ItemType, type, m_Tool, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 24]);
            }
            else if (IsFrom == "Fifth")
            {
                Type type = null;
                CraftContext context = DefInscription.CraftSystem.GetContext(m_Mobile);
                CraftSubResCol res = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 32].UseSubRes2 ? DefInscription.CraftSystem.CraftSubRes2 : DefInscription.CraftSystem.CraftSubRes;
                int resIndex = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 32].UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
                if (resIndex > -1)
                {
                    type = res[resIndex].ItemType;
                }
                DefInscription.CraftSystem.CreateItem(m_Mobile, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 32].ItemType, type, m_Tool, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 32]);
            }
            else if (IsFrom == "Sixth")
            {
                Type type = null;
                CraftContext context = DefInscription.CraftSystem.GetContext(m_Mobile);
                CraftSubResCol res = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 40].UseSubRes2 ? DefInscription.CraftSystem.CraftSubRes2 : DefInscription.CraftSystem.CraftSubRes;
                int resIndex = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 40].UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
                if (resIndex > -1)
                {
                    type = res[resIndex].ItemType;
                }
                DefInscription.CraftSystem.CreateItem(m_Mobile, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 40].ItemType, type, m_Tool, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 40]);
            }
            else if (IsFrom == "Seventh")
            {
                Type type = null;
                CraftContext context = DefInscription.CraftSystem.GetContext(m_Mobile);
                CraftSubResCol res = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 48].UseSubRes2 ? DefInscription.CraftSystem.CraftSubRes2 : DefInscription.CraftSystem.CraftSubRes;
                int resIndex = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 48].UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
                if (resIndex > -1)
                {
                    type = res[resIndex].ItemType;
                }
                DefInscription.CraftSystem.CreateItem(m_Mobile, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 48].ItemType, type, m_Tool, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 48]);
            }
            else if (IsFrom == "Eighth")
            {
                Type type = null;
                CraftContext context = DefInscription.CraftSystem.GetContext(m_Mobile);
                CraftSubResCol res = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 56].UseSubRes2 ? DefInscription.CraftSystem.CraftSubRes2 : DefInscription.CraftSystem.CraftSubRes;
                int resIndex = DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 56].UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
                if (resIndex > -1)
                {
                    type = res[resIndex].ItemType;
                }
                DefInscription.CraftSystem.CreateItem(m_Mobile, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 56].ItemType, type, m_Tool, DefInscription.CraftSystem.CraftItems[m_Entries[index].CraftIndex + 56]);
            }
        }
    }
} 