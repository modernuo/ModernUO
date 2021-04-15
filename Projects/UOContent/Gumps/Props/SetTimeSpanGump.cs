using System;
using System.Reflection;
using Server.Commands;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class SetTimeSpanGump : Gump
    {
        private static readonly int EntryWidth = 212;

        private static readonly int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
        private static readonly int TotalHeight = OffsetSize + 7 * (EntryHeight + OffsetSize);

        private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;
        private static readonly int BackHeight = BorderSize + TotalHeight + BorderSize;
        private readonly Mobile m_Mobile;
        private readonly object m_Object;
        private readonly PropertyInfo m_Property;
        private readonly PropertiesGump m_PropertiesGump;

        public SetTimeSpanGump(
            PropertyInfo prop, Mobile mobile, object o, PropertiesGump propertiesGump
        )
            : base(GumpOffsetX, GumpOffsetY)
        {
            m_PropertiesGump = propertiesGump;
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;

            var ts = (TimeSpan)(prop?.GetValue(o, null) ?? new TimeSpan());

            AddPage(0);

            AddBackground(0, 0, BackWidth, BackHeight, BackGumpID);
            AddImageTiled(
                BorderSize,
                BorderSize,
                TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0),
                TotalHeight,
                OffsetGumpID
            );

            AddRect(0, prop?.Name, 0, -1);
            AddRect(1, ts.ToString(), 0, -1);
            AddRect(2, "Zero", 1, -1);
            AddRect(3, "From H:M:S", 2, -1);
            AddRect(4, "H:", 3, 0);
            AddRect(5, "M:", 4, 1);
            AddRect(6, "S:", 5, 2);
        }

        private void AddRect(int index, string str, int button, int text)
        {
            var x = BorderSize + OffsetSize;
            var y = BorderSize + OffsetSize + index * (EntryHeight + OffsetSize);

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, str);

            if (text != -1)
            {
                AddTextEntry(x + 16 + TextOffsetX, y, EntryWidth - TextOffsetX - 16, EntryHeight, TextHue, text, "");
            }

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
            TimeSpan toSet;
            bool shouldSet, shouldSend;

            var h = info.GetTextEntry(0);
            var m = info.GetTextEntry(1);
            var s = info.GetTextEntry(2);

            switch (info.ButtonID)
            {
                case 1: // Zero
                    {
                        toSet = TimeSpan.Zero;
                        shouldSet = true;
                        shouldSend = true;

                        break;
                    }
                case 2: // From H:M:S
                    {
                        var successfulParse = false;
                        if (h != null && m != null && s != null)
                        {
                            successfulParse = TimeSpan.TryParse($"{h.Text}:{m.Text}:{s.Text}", out toSet);
                        }
                        else
                        {
                            toSet = TimeSpan.Zero;
                        }

                        shouldSet = shouldSend = successfulParse;

                        break;
                    }
                case 3: // From H
                    {
                        if (h != null)
                        {
                            try
                            {
                                toSet = TimeSpan.FromHours(Utility.ToDouble(h.Text));
                                shouldSet = true;
                                shouldSend = true;

                                break;
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        toSet = TimeSpan.Zero;
                        shouldSet = false;
                        shouldSend = false;

                        break;
                    }
                case 4: // From M
                    {
                        if (m != null)
                        {
                            try
                            {
                                toSet = TimeSpan.FromMinutes(Utility.ToDouble(m.Text));
                                shouldSet = true;
                                shouldSend = true;

                                break;
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        toSet = TimeSpan.Zero;
                        shouldSet = false;
                        shouldSend = false;

                        break;
                    }
                case 5: // From S
                    {
                        if (s != null)
                        {
                            try
                            {
                                toSet = TimeSpan.FromSeconds(Utility.ToDouble(s.Text));
                                shouldSet = true;
                                shouldSend = true;

                                break;
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        toSet = TimeSpan.Zero;
                        shouldSet = false;
                        shouldSend = false;

                        break;
                    }
                default:
                    {
                        toSet = TimeSpan.Zero;
                        shouldSet = false;
                        shouldSend = true;

                        break;
                    }
            }

            if (shouldSet)
            {
                try
                {
                    CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, toSet.ToString());
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
    }
}
