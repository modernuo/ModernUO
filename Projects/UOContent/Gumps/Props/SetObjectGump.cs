using System;
using System.Reflection;
using Server.Commands;
using Server.Commands.Generic;
using Server.Network;
using Server.Prompts;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class SetObjectGump : Gump
    {
        private static readonly int EntryWidth = 212;

        private static readonly int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
        private static readonly int TotalHeight = OffsetSize + 5 * (EntryHeight + OffsetSize);

        private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;
        private static readonly int BackHeight = BorderSize + TotalHeight + BorderSize;
        private readonly Mobile m_Mobile;
        private readonly object m_Object;
        private readonly PropertyInfo m_Property;
        private readonly Type m_Type;
        private readonly PropertiesGump m_PropertiesGump;

        public SetObjectGump(
            PropertyInfo prop, Mobile mobile, object o, Type type, PropertiesGump propertiesGump
        ) : base(GumpOffsetX, GumpOffsetY)
        {
            m_PropertiesGump = propertiesGump;
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;
            m_Type = type;

            var initialText = PropertiesGump.ValueToString(o, prop);

            AddPage(0);

            AddBackground(0, 0, BackWidth, BackHeight, BackGumpID);
            AddImageTiled(
                BorderSize,
                BorderSize,
                TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0),
                TotalHeight,
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
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, initialText);
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 1);

            x = BorderSize + OffsetSize;
            y += EntryHeight + OffsetSize;

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Change by Serial");
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 2);

            x = BorderSize + OffsetSize;
            y += EntryHeight + OffsetSize;

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Nullify");
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 3);

            x = BorderSize + OffsetSize;
            y += EntryHeight + OffsetSize;

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "View Properties");
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 4);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var shouldSend = true;
            object viewProps = null;

            switch (info.ButtonID)
            {
                case 0: // closed
                    {
                        m_PropertiesGump.SendPropertiesGump();
                        shouldSend = false;
                        break;
                    }
                case 1: // Change by Target
                    {
                        m_Mobile.Target = new SetObjectTarget(
                            m_Property,
                            m_Mobile,
                            m_Object,
                            m_Type,
                            m_PropertiesGump
                        );
                        shouldSend = false;
                        break;
                    }
                case 2: // Change by Serial
                    {
                        shouldSend = false;

                        m_Mobile.SendMessage("Enter the serial you wish to find:");
                        m_Mobile.Prompt = new InternalPrompt(
                            m_Property,
                            m_Mobile,
                            m_Object,
                            m_Type,
                            m_PropertiesGump
                        );

                        break;
                    }
                case 3: // Nullify
                    {
                        try
                        {
                            CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, "(null)");
                            m_Property.SetValue(m_Object, null, null);
                            m_PropertiesGump.OnValueChanged(m_Object, m_Property);
                        }
                        catch
                        {
                            m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                        }

                        break;
                    }
                case 4: // View Properties
                    {
                        var obj = m_Property.GetValue(m_Object, null);

                        if (obj == null)
                        {
                            m_Mobile.SendMessage("The property is null and so you cannot view its properties.");
                        }
                        else if (!BaseCommand.IsAccessible(m_Mobile, obj))
                        {
                            m_Mobile.SendMessage("You may not view their properties.");
                        }
                        else
                        {
                            viewProps = obj;
                        }

                        break;
                    }
            }

            if (shouldSend)
            {
                m_Mobile.SendGump(new SetObjectGump(m_Property, m_Mobile, m_Object, m_Type, m_PropertiesGump));
            }

            if (viewProps != null)
            {
                m_Mobile.SendGump(new PropertiesGump(m_Mobile, viewProps));
            }
        }

        private class InternalPrompt : Prompt
        {
            private readonly Mobile m_Mobile;
            private readonly object m_Object;
            private readonly PropertyInfo m_Property;
            private readonly Type m_Type;
            private readonly PropertiesGump m_PropertiesGump;

            public InternalPrompt(
                PropertyInfo prop, Mobile mobile, object o, Type type, PropertiesGump propertiesGump
            )
            {
                m_PropertiesGump = propertiesGump;
                m_Property = prop;
                m_Mobile = mobile;
                m_Object = o;
                m_Type = type;
            }

            public override void OnCancel(Mobile from)
            {
                m_Mobile.SendGump(new SetObjectGump(m_Property, m_Mobile, m_Object, m_Type, m_PropertiesGump));
            }

            public override void OnResponse(Mobile from, string text)
            {
                try
                {
                    var serial = (Serial)Utility.ToUInt32(text);

                    var toSet = World.FindEntity(serial);

                    if (toSet == null)
                    {
                        m_Mobile.SendMessage("No object with that serial was found.");
                    }
                    else if (!m_Type.IsInstanceOfType(toSet))
                    {
                        m_Mobile.SendMessage(
                            $"The object with that serial could not be assigned to a property of type : {m_Type.Name}"
                        );
                    }
                    else
                    {
                        try
                        {
                            CommandLogging.LogChangeProperty(
                                m_Mobile,
                                m_Object,
                                m_Property.Name,
                                toSet.ToString()
                            );
                            m_Property.SetValue(m_Object, toSet, null);
                            m_PropertiesGump.OnValueChanged(m_Object, m_Property);
                        }
                        catch
                        {
                            m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                        }
                    }
                }
                catch
                {
                    m_Mobile.SendMessage("Bad format");
                }

                m_Mobile.SendGump(new SetObjectGump(m_Property, m_Mobile, m_Object, m_Type, m_PropertiesGump));
            }
        }
    }
}
