using System.Reflection;
using Server.Commands;
using Server.HuePickers;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class SetGump : Gump
    {
        private static readonly int EntryWidth = 212;

        private static readonly int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
        private static readonly int TotalHeight = OffsetSize + 2 * (EntryHeight + OffsetSize);

        private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;
        private static readonly int BackHeight = BorderSize + TotalHeight + BorderSize;
        private readonly Mobile m_Mobile;
        private readonly object m_Object;
        private readonly PropertyInfo m_Property;
        private readonly PropertiesGump m_PropertiesGump;

        public SetGump(PropertyInfo prop, Mobile from, object o, PropertiesGump propertiesGump) : base(GumpOffsetX, GumpOffsetY)
        {
            m_Mobile = from;
            m_Object = o;
            m_Property = prop;
            m_PropertiesGump = propertiesGump;

            var canNull = !prop.PropertyType.IsValueType;
            var canDye = prop.IsDefined(typeof(HueAttribute), false);

            var val = prop.GetValue(m_Object, null);
            var initialText = val switch
            {
                null => "",
                TextDefinition definition => definition.GetValue(),
                _ => val.ToString()
            };

            AddPage(0);

            AddBackground(
                0,
                0,
                BackWidth,
                BackHeight + (canNull ? EntryHeight + OffsetSize : 0) + (canDye ? EntryHeight + OffsetSize : 0),
                BackGumpID
            );
            AddImageTiled(
                BorderSize,
                BorderSize,
                TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0),
                TotalHeight + (canNull ? EntryHeight + OffsetSize : 0) + (canDye ? EntryHeight + OffsetSize : 0),
                OffsetGumpID
            );

            var x = BorderSize + OffsetSize;
            var y = BorderSize + OffsetSize;

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, prop.Name);
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            x = BorderSize + OffsetSize;
            y += EntryHeight + OffsetSize;

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddTextEntry(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, 0, initialText);
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 1);

            if (canNull)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
                AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Null");
                x += EntryWidth + OffsetSize;

                if (SetGumpID != 0)
                {
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                }

                AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 2);
            }

            if (canDye)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
                AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Hue Picker");
                x += EntryWidth + OffsetSize;

                if (SetGumpID != 0)
                {
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                }

                AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 3);
            }
        }


        public override void OnResponse(NetState sender, RelayInfo info)
        {
            object toSet = null;
            bool shouldSet, shouldSend = true;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        var text = info.GetTextEntry(0);

                        if (text != null)
                        {
                            try
                            {
                                Types.TryParse(m_Property.PropertyType, text.Text, out toSet);
                                shouldSet = true;
                            }
                            catch
                            {
                                toSet = null;
                                shouldSet = false;
                                m_Mobile.SendMessage("Bad format");
                            }
                        }
                        else
                        {
                            toSet = null;
                            shouldSet = false;
                        }

                        break;
                    }
                case 2: // Null
                    {
                        shouldSet = true;
                        break;
                    }
                case 3: // Hue Picker
                    {
                        shouldSet = false;
                        shouldSend = false;

                        m_Mobile.SendHuePicker(new InternalPicker(m_Property, m_Mobile, m_Object, m_PropertiesGump));
                        break;
                    }
                default:
                    {
                        shouldSet = false;
                        break;
                    }
            }

            if (shouldSet)
            {
                try
                {
                    CommandLogging.LogChangeProperty(
                        m_Mobile,
                        m_Object,
                        m_Property.Name,
                        toSet?.ToString() ?? "(null)"
                    );
                    m_Property.SetValue(m_Object, toSet, null);
                    m_PropertiesGump.OnValueChanged(m_Object, m_Property);
                }
                catch
                {
                    m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                }
            }

            if (shouldSend)
            {
                m_PropertiesGump.SendPropertiesGump();
            }
        }

        private class InternalPicker : HuePicker
        {
            private readonly Mobile m_Mobile;
            private readonly object m_Object;
            private readonly PropertyInfo m_Property;
            private readonly PropertiesGump m_PropertiesGump;

            public InternalPicker(
                PropertyInfo prop, Mobile mobile, object o, PropertiesGump propertiesGump) : base(((IHued)o).HuedItemID)
            {
                m_Property = prop;
                m_Mobile = mobile;
                m_Object = o;
                m_PropertiesGump = propertiesGump;
            }

            public override void OnResponse(int hue)
            {
                try
                {
                    CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, hue.ToString());
                    m_Property.SetValue(m_Object, hue, null);
                    m_PropertiesGump.OnValueChanged(m_Object, m_Property);
                }
                catch
                {
                    m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                }

                m_PropertiesGump.SendPropertiesGump();
            }
        }
    }
}
