using System.Collections.Generic;

namespace Server.Engines.Craft
{
    public enum CraftMarkOption
    {
        MarkItem,
        DoNotMark,
        PromptForMark
    }

    public class CraftContext
    {
        public CraftContext()
        {
            Items = new List<CraftItem>();
            LastResourceIndex = -1;
            LastResourceIndex2 = -1;
            LastGroupIndex = -1;
        }

        public List<CraftItem> Items { get; }

        public int LastResourceIndex { get; set; }

        public int LastResourceIndex2 { get; set; }

        public int LastGroupIndex { get; set; }

        public bool DoNotColor { get; set; }

        public CraftMarkOption MarkOption { get; set; }

        public CraftItem LastMade
        {
            get
            {
                if (Items.Count > 0)
                {
                    return Items[0];
                }

                return null;
            }
        }

        public void OnMade(CraftItem item)
        {
            Items.Remove(item);

            if (Items.Count == 10)
            {
                Items.RemoveAt(9);
            }

            Items.Insert(0, item);
        }
    }
}
