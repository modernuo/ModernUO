using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Server.packLoader;

namespace Server.Gumps
{
    public class LootPackGump : Gump
    {
        private static readonly int BackGumpID = 0x13BE;
        //private static readonly int BorderSize = 10;
        private static readonly int perPage = 15;
        private static readonly int HeightPerItem = 30;
        private static readonly int BottomHeight = 100;
        private static readonly int TopHeight = 130;
        public static readonly int TextHue = 0;
        public static int LastItem = 0;
        private static int TotalPage = 0;
        private static int TakedItems = 0;
        private static int M_page = 0;
        private  Dictionary<string, Pack> Packs { get; set; }

        public int GetButtonID(int type, int index) => 1 + index * 10 + type;


        public void AddCustomizeEntry(int x,int y)
        {
            AddImageTiled(x, y, 161, 23, 0xA40); // creature text box
            AddImageTiled(x,y, 159, 21, 0xBBC); // creature text box
            AddTextEntry(x, y, 156, 21, 0, 0, "");
        }
        public void AddCustomizeLabel(int x, int y,string Name)
        {
            AddImageTiled(x, y, 161, 23, 0xA40); // creature text box
            AddImageTiled(x, y, 159, 21, 0xBBC); // creature text box
            AddLabel(x, y, 0, Name);
        }

        public LootPackGump(int page = 0) : base(0, 0)
        {
            Packs = packLoader.GetPacks();
            var bodyHeight = (HeightPerItem * perPage) + TopHeight;
            TotalPage = (Packs.Count / perPage);

            TakedItems = 0;
            M_page = page;
            AddBackground(0, 0, 260, bodyHeight + BottomHeight, BackGumpID);
            //AddImageTiled(0, 0, 260, bodyHeight + BottomHeight, 2624);

            if (page > 0) AddButton(110, bodyHeight + 50, 0x15E3, 0x15E7, 2);
            else AddImage(110, bodyHeight + 50, 0x25EA);
            if (page < TotalPage) AddButton(130, bodyHeight + 50, 0x15E1, 0x15E5, 3);
            else AddImage(130, bodyHeight + 50, 0x25E6);


            AddHtml(80, 0, 200, 20, "<BASEFONT COLOR=#F4F4F4>LootPack Editor</BASEFONT>");
            AddHtml(80, 40, 200, 20, "<BASEFONT COLOR=#F4F4F4>New Lootpack Name</BASEFONT>");
            AddCustomizeEntry(55, 60);
            AddButton(90, 90, 5204, 5205, 1);
            LastItem = Packs.Count + 5;

            for (var i = 0; i < Packs.Count; i++)
            {
                var idx = i + (perPage * page);
                if (TakedItems >= perPage || idx >= Packs.Count) break;
                var item = Packs.ElementAt(idx);
                AddButton(7, (i * HeightPerItem) + TopHeight, 0xFA5, 0xFA7, idx + 5); // Edit
                AddCustomizeLabel(50, (i * HeightPerItem) + TopHeight, item.Key);
                AddButton(220, (i * HeightPerItem) + TopHeight, 0xFA2, 0xFA4, LastItem + idx); // Delete
                TakedItems++;

            }



        }
        public override void OnResponse(NetState state, RelayInfo info)
        {
            var from = state.Mobile;

            switch (info.ButtonID)
            {
                case 0: // close
                    break;
                case 1://add
                    var name = info.GetTextEntry(0).Text;
                    if (name.Length < 2 || name.Contains(" "))
                    {
                        from.SendMessage("The name cannot be less than " +
                            "2 characters or contain a space");
                        break;
                    }
                    if (Packs.ContainsKey(name))
                    {
                        from.SendMessage("Name is already used");
                        break;
                    }
                    from.SendGump(new LootPackCreationGump(name, null));
                    break;
                case 2://prev
                    from.SendGump(new LootPackGump(M_page -= 1));
                    break;
                case 3://next
                    from.SendGump(new LootPackGump(M_page += 1));
                    break;
                case int n when (n >= 5 && n < LastItem): //edit
                    var Idx = n - 5;
                    if (Idx > Packs.Count) return;
                    var Pack = Packs.ElementAt(Idx);
                    from.SendGump(new LootPackCreationGump(Pack.Key, Pack.Value.LootItems));
                    break;

                case int n when (n >= LastItem && n <= LastItem + Packs.Count): //delete
                    var DellIdx = (n - LastItem);
                    if (DellIdx > Packs.Count) return;
                    var DellPack = Packs.ElementAt(DellIdx);
                    packLoader.DeletePack(DellPack.Key);
                    from.SendGump(new LootPackGump(M_page));
                    break;
                default:
                    break;

            }
        }
    }
}
