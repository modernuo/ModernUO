using Server.Network;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using static Server.DataModel;
using static Server.DataStore;

namespace Server.Gumps
{
    public class BaseViewGump<T> : Gump
    {
        private Mobile _from;
        private int _totalPage = 0;
        private const int _typeCount = 10;
        private static readonly int BackGumpID = 0x13BE;
        private static readonly int perPage = 15;
        private static readonly int HeightPerItem = 30;
        private static readonly int BottomHeight = 100;
        private static readonly int TopHeight = 130;

        public int Page = 0;
        public virtual string Name => typeof(T).Name;
        public virtual void OnClose() { }
        public virtual void OnAdd(Mobile m, string name) { }
        public virtual void OnEdit(Mobile m, int index) { }
        public virtual void OnPressTwoButton(Mobile m, int index) { }
        public virtual void OnDelete(Mobile m, int index) { }
        public virtual void OnPropertyEdited(Mobile m, object m_Object) { }
        public void CaptureHandler() => PropertiesGump.OnEdit += OnEditHandler;
        public List<KeyValuePair<string, T>> Collection { get; set; }
        public int GetButtonID(int type, int index) => 1 + type + index * _typeCount;
        public virtual void OnPage(Mobile m, int page) => m.SendGump(Activator.CreateInstance(this.GetType(), _from, page) as Gump);

        private void OnEditHandler(object m_obj)
        {
            OnPropertyEdited(_from, m_obj);
            PropertiesGump.OnEdit -= OnEditHandler;
        }

        public BaseViewGump(Mobile owner, List<KeyValuePair<string, T>> collection, int page = 0) : base(0, 0)
        {
            Collection = collection;
            Display(owner, page);
        }
        public BaseViewGump(Mobile owner, int page = 0) : base(0, 0)
        {
            OnLoad();
            Collection = Collection ?? new List<KeyValuePair<string, T>>();
            Display(owner, page);
        }
        public virtual void OnLoad()
        {
            throw new Exception("OnLoad not overloaded");
        }

        private void AddCustomizeEntry(int x, int y)
        {
            AddImageTiled(x, y, 161, 23, 0xA40);
            AddImageTiled(x, y, 159, 21, 0xBBC);
            AddTextEntry(x, y, 156, 21, 0, 0, "");
        }
        private void AddCustomizeLabel(int x, int y, string Name)
        {
            AddImageTiled(x, y, 161, 23, 0xA40);
            AddImageTiled(x, y, 159, 21, 0xBBC);
            AddLabel(x, y, 0, Name);
        }

        private void Display(Mobile owner, int page = 0)
        {
            _from = owner;
            Page = page;

            var bodyHeight = (HeightPerItem * perPage) + TopHeight;
            _totalPage = (Collection.Count / perPage);

            if (Collection.Count % perPage == 0)
                _totalPage--;

            AddBackground(0, 0, 290, bodyHeight + BottomHeight, BackGumpID);
            AddHtml(0, 20, 290, 20, $"<BASEFONT COLOR=#F4F4F4>  <CENTER>{Name}</CENTER> Editor </BASEFONT>");
            AddCustomizeEntry(55, 60);
            AddButton(90, 90, 5204, 5205, GetButtonID(1, 1));

            if (page > 0) AddButton(110, bodyHeight + 50, 0x15E3, 0x15E7, GetButtonID(2, 1));
            else AddImage(110, bodyHeight + 50, 0x25EA);

            if (page < _totalPage) AddButton(130, bodyHeight + 50, 0x15E1, 0x15E5, GetButtonID(3, 1));
            else AddImage(130, bodyHeight + 50, 0x25E6);

            var i = 0;
            var index = page * perPage;
            foreach (var item in Collection.Skip(index).Take(perPage))
            {
                AddButton(3, (i * HeightPerItem) + TopHeight, 0xFA5, 0xFA7, GetButtonID(4, i + index));

                if (TwoButton)
                {
                    AddButton(40, (i * HeightPerItem) + TopHeight, 4023, 4025, GetButtonID(5, i + index));
                }

                AddCustomizeLabel(80, (i * HeightPerItem) + TopHeight, item.Key);
                AddButton(250, (i * HeightPerItem) + TopHeight, 0xFA2, 0xFA4, GetButtonID(6, i + index)); // Delete
                i++;
            }
        }

        public virtual bool TwoButton => false;
        public void Close()
        {
            _from.CloseGump<BaseViewGump<T>>();
        }
        public virtual void ReOpen()
        {
            Close();
            _from.SendGump(Activator.CreateInstance(this.GetType(), _from, Page) as Gump);
        }

       

        public override void OnResponse(NetState state, RelayInfo info)
        {
            var buttonID = info.ButtonID - 1;
            var type = buttonID % _typeCount;
            var index = buttonID / _typeCount;

            switch (type)
            {
                case 0: // close
                case -1: // close
                    OnClose();
                    break;
                case 1://add
                    OnAdd(_from, info.GetTextEntry(0).Text);

                    break;
                case 2://prev
                    OnPage(_from, Page -= 1);

                    break;
                case 3://next
                    OnPage(_from, Page += 1);

                    break;
                case 4: //edit
                    if (index < Collection.Count)
                    {
                        OnEdit(_from, index);
                    }
                    break;
                case 5: //two button
                    if (index < Collection.Count)
                    {
                        OnPressTwoButton(_from, index);
                    }
                    break;
                case 6: //delete
                    if (index < Collection.Count)
                    {
                        OnDelete(_from, index);
                    }

                    break;
                default:
                    break;
            }
        }
    }
}
