using System.Reflection;
using Server.Commands;
using Server.Commands.Generic;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class SetListOptionGump : Gump
    {
        private static readonly int EntryWidth = 212;
        private static readonly int EntryCount = 13;

        private static readonly int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

        private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;

        private static readonly bool PrevLabel = OldStyle;
        private static readonly bool NextLabel = OldStyle;

        private static readonly int PrevLabelOffsetX = PrevWidth + 1;
        private static readonly int PrevLabelOffsetY = 0;

        private static readonly int NextLabelOffsetX = -29;
        private static readonly int NextLabelOffsetY = 0;

        private readonly object[] m_Values;
        protected Mobile m_Mobile;
        protected object m_Object;
        protected PropertyInfo m_Property;
        protected PropertiesGump m_PropertiesGump;

        public SetListOptionGump(
            PropertyInfo prop, Mobile mobile, object o, PropertiesGump propertiesGump, string[] names, object[] values
        ) : base(GumpOffsetX, GumpOffsetY)
        {
            m_PropertiesGump = propertiesGump;
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;

            m_Values = values;

            var pages = (names.Length + EntryCount - 1) / EntryCount;
            var index = 0;

            for (var page = 1; page <= pages; ++page)
            {
                AddPage(page);

                var start = (page - 1) * EntryCount;
                var count = names.Length - start;

                if (count > EntryCount)
                {
                    count = EntryCount;
                }

                var totalHeight = OffsetSize + (count + 2) * (EntryHeight + OffsetSize);
                var backHeight = BorderSize + totalHeight + BorderSize;

                AddBackground(0, 0, BackWidth, backHeight, BackGumpID);
                AddImageTiled(
                    BorderSize,
                    BorderSize,
                    TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0),
                    totalHeight,
                    OffsetGumpID
                );

                var x = BorderSize + OffsetSize;
                var y = BorderSize + OffsetSize;

                var emptyWidth = TotalWidth - PrevWidth - NextWidth - OffsetSize * 4 -
                                 (OldStyle ? SetWidth + OffsetSize : 0);

                AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);

                if (page > 1)
                {
                    AddButton(
                        x + PrevOffsetX,
                        y + PrevOffsetY,
                        PrevButtonID1,
                        PrevButtonID2,
                        0,
                        GumpButtonType.Page,
                        page - 1
                    );

                    if (PrevLabel)
                    {
                        AddLabel(x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous");
                    }
                }

                x += PrevWidth + OffsetSize;

                if (!OldStyle)
                {
                    AddImageTiled(
                        x - (OldStyle ? OffsetSize : 0),
                        y,
                        emptyWidth + (OldStyle ? OffsetSize * 2 : 0),
                        EntryHeight,
                        HeaderGumpID
                    );
                }

                x += emptyWidth + OffsetSize;

                if (!OldStyle)
                {
                    AddImageTiled(x, y, NextWidth, EntryHeight, HeaderGumpID);
                }

                if (page < pages)
                {
                    AddButton(
                        x + NextOffsetX,
                        y + NextOffsetY,
                        NextButtonID1,
                        NextButtonID2,
                        0,
                        GumpButtonType.Page,
                        page + 1
                    );

                    if (NextLabel)
                    {
                        AddLabel(x + NextLabelOffsetX, y + NextLabelOffsetY, TextHue, "Next");
                    }
                }

                AddRect(0, prop.Name, 0);

                for (var i = 0; i < count; ++i)
                {
                    AddRect(i + 1, names[index], ++index);
                }
            }
        }

        private void AddRect(int index, string str, int button)
        {
            var x = BorderSize + OffsetSize;
            var y = BorderSize + OffsetSize + (index + 1) * (EntryHeight + OffsetSize);

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, str);

            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            if (button != 0)
            {
                AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, button);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;

            if (!BaseCommand.IsAccessible(from, m_Object))
            {
                from.SendMessage("You may no longer access their properties.");
                return;
            }

            var index = info.ButtonID - 1;

            if (index >= 0 && index < m_Values.Length)
            {
                try
                {
                    var toSet = m_Values[index];

                    var result = Properties.SetDirect(
                        m_Mobile,
                        m_Object,
                        m_Object,
                        m_Property,
                        m_Property.Name,
                        toSet,
                        true
                    );

                    m_Mobile.SendMessage(result);

                    if (result == "Property has been set.")
                    {
                        m_PropertiesGump.OnValueChanged(m_Object, m_Property);
                    }
                }
                catch
                {
                    m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                }
            }

            m_PropertiesGump.SendPropertiesGump();
        }
    }
}
