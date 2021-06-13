using Server.Items;
using Server.Network;
using Server.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Server.packLoader;

namespace Server.Gumps
{
    
    public class LootPackCreationGump : Gump
    {
        private static readonly int BackGumpID = 0x13BE;
        //private static readonly int BorderSize = 10;
        private static readonly int perPage = 16;
        private static readonly int HeightPerItem = 30;
        private static readonly int BottomHeight = 100;
        private static readonly int TopHeight = 50;
        private  List<LootItem> _items { get; set; }
        public static readonly int TextHue = 0;
        private string PackName { get; set; }

       
        public void AddCustomizeEntry(int x, int y,int i,string text = "", bool NextEntry = true)
        {
            AddButton(5, y, 0xFA5, 0xFA7, i+1);
            AddImageTiled(x, y, 161, 23, 0xA40); // creature text box
            AddImageTiled(x, y, 159, 21, 0xBBC); // creature text box
            if(NextEntry)AddTextEntry(x, y, 156, 21, 0, 0, text);
        }

        public LootPackCreationGump(string Name, List<LootItem> items = null) : base(0, 0)
        {

            PackName = Name;
            _items = items==null?
                new List<LootItem>() : items.ToList();

            var hlen = (HeightPerItem * perPage) + TopHeight;
            AddBackground(0, 0, 260, hlen + BottomHeight, BackGumpID);
            AddHtml(50, 25, 200, 20, "<BASEFONT COLOR=#F4F4F4>ItemsPack => "+ Name +"</BASEFONT>");
            for (int i = 0; i < 16; i++)
            {
                if(_items.Count>i)
                {
                    AddCustomizeEntry(50, (i * HeightPerItem) + TopHeight, i, items[i].TypeName);
                    AddButton(220, (i * HeightPerItem) + TopHeight, 0xFA2, 0xFA4,i+ perPage);
                }
                else AddCustomizeEntry(50, (i * HeightPerItem) + TopHeight, i,"", _items.Count>=i?true:false);
            }
            AddButton(80, hlen+30, 5204, 5205, 33);
        }

        public LootItem CheckType(Mobile from,string Name,int idx)
        {
            var _itm = AssemblyHandler.FindTypeByName(Name);
            if (_itm is not null)
            {
                return new LootItem()
                {
                    PackName = PackName,
                    Item = _itm,
                    Idx = idx,
                    TypeName = Name,
                    CreateDT = DateTime.Now
                };
            }

            if (packLoader.PackExist(Name))
            {
                return new LootItem()
                {
                    IsDestPack = true,
                    Item = null,
                    Idx = idx,
                    TypeName = Name,
                    CreateDT = DateTime.Now
                };
            }
            return null;
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            var from = state.Mobile;

            switch (info.ButtonID)
            {
                case 0:
                    break;
                case int n when (n >0 && n < perPage):
                    var idx = n-1;
                    var name = info.TextEntries[idx]?.Text;
                    var item = CheckType(from,name, idx);
                    if (item is null)
                    {   
                       
                        from.SendMessage("item or pack not found");
                        from.SendGump(new LootPackCreationGump(PackName, _items));
                    }
                    else
                    {
                        //is edit
                        if (_items?.Count > idx)
                        {
                            if (_items[idx].TypeName == item.TypeName)
                            { 
                                //get param from original
                                item.AtSpawnTime = _items[idx].AtSpawnTime;
                                item.DropChance = _items[idx].DropChance;
                                item.maxIntensity = _items[idx].maxIntensity;
                                item.minIntensity = _items[idx].minIntensity;
                                item.maxProps = _items[idx].maxProps;
                                item.Quantity = _items[idx].Quantity;
                              
                            }
                            item._savedpack = _items;
                        }
                        else
                        {
                            _items.Add(item);
                            item._savedpack = _items;
                        }
                        
                        if (!item.IsDestPack)
                        {
                            from.SendGump(new LootPackItemEditor(from, item));
                        }
                        else from.SendGump(new LootPackCreationGump(PackName, _items));
                    }
                    break;
                case int n when (n+1 > perPage && n <= perPage*2):
                    var dellindx = n - perPage;
                    if (_items?.Count > dellindx)
                    {
                        _items.RemoveAt(dellindx);
                        packLoader.AddToPack(PackName, _items);
                        from.SendGump(new LootPackCreationGump(PackName, _items));
                    }
                    break;
                case 33:
                    packLoader.AddToPack(PackName,_items);
                    from.SendMessage("Pack "+ PackName + " is saved");
                    //_items.Clear();
                    break;
                default:
                    break;

            }
        }
    }
}
