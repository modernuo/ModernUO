using System;
using System.Reflection;
using Server.Commands;
using Server.Commands.Generic;
using Server.Network;

namespace Server.Gumps
{
    public class SetCustomEnumGump : SetListOptionGump
    {
        private readonly string[] m_Names;

        public SetCustomEnumGump(
            PropertyInfo prop, Mobile mobile, object o, PropertiesGump propertiesGump, string[] names
        ) : base(prop, mobile, o, propertiesGump, names, null) =>
            m_Names = names;

        public override void OnResponse(NetState sender, RelayInfo relayInfo)
        {
            var from = sender.Mobile;

            if (!BaseCommand.IsAccessible(from, m_Object))
            {
                from.SendMessage("You may no longer access their properties.");
                return;
            }

            var index = relayInfo.ButtonID - 1;

            if (index >= 0 && index < m_Names.Length)
            {
                try
                {
                    var info = m_Property.PropertyType.GetMethod("Parse", new[] { typeof(string) });

                    string result;

                    if (info != null)
                    {
                        result = Properties.SetDirect(
                            m_Mobile,
                            m_Object,
                            m_Object,
                            m_Property,
                            m_Property.Name,
                            info.Invoke(null, new object[] { m_Names[index] }),
                            true
                        );
                    }
                    else if (m_Property.PropertyType == typeof(Enum) || m_Property.PropertyType.IsSubclassOf(typeof(Enum)))
                    {
                        result = Properties.SetDirect(
                            m_Mobile,
                            m_Object,
                            m_Object,
                            m_Property,
                            m_Property.Name,
                            Enum.Parse(m_Property.PropertyType, m_Names[index], false),
                            true
                        );
                    }
                    else
                    {
                        result = "";
                    }

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
