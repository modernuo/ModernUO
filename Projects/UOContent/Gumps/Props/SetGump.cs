using System.Reflection;
using Server.Commands;
using Server.HuePickers;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class SetGump : Gump
    {
        private const int EntryWidth = 212;

        private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

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

            var rowCount = 2 + (canNull ? 1 : 0) + (canDye ? 1 : 0);

            AddPage(0);

            this.AddPropsFrame(TotalWidth, rowCount, out var x, out var y);
            this.AddPropsEntryLabel(ref x, ref y, EntryWidth, prop.Name);
            PropsLayout.NextRow(ref x, ref y);
            this.AddPropsEntryTextInput(ref x, ref y, EntryWidth, 0, initialText, true, 1);

            if (canNull)
            {
                PropsLayout.NextRow(ref x, ref y);
                this.AddPropsEntryButton(ref x, ref y, EntryWidth, "Null", true, 2);
            }

            if (canDye)
            {
                PropsLayout.NextRow(ref x, ref y);
                this.AddPropsEntryButton(ref x, ref y, EntryWidth, "Hue Picker", true, 3);
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            object toSet = null;
            bool shouldSet, shouldSend = true;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        var text = info.GetTextEntry(0)?.Trim();

                        if (text?.Length > 0)
                        {
                            try
                            {
                                Types.TryParse(m_Property.PropertyType, text, out toSet);
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

            public InternalPicker(PropertyInfo prop, Mobile mobile, object o, PropertiesGump propertiesGump)
                : base(((IHued)o).HuedItemID)
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
