using System;
using System.Collections.Generic;

namespace Server.Engines.Craft
{
    public class CraftSubResCol : List<CraftSubRes>
    {
        public CraftSubResCol() => Init = false;

        public bool Init { get; set; }

        public Type ResType { get; set; }

        public TextDefinition Name { get; set; }

        public CraftSubRes GetAt(int index) => this[index];

        public CraftSubRes SearchFor(Type type)
        {
            for (var i = 0; i < Count; i++)
            {
                var craftSubRes = this[i];
                if (craftSubRes.ItemType == type)
                {
                    return craftSubRes;
                }
            }

            return null;
        }
    }
}
