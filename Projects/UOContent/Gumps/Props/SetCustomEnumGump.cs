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

        public override void OnResponse(NetState sender, in RelayInfo relayInfo)
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
                    var info = m_Property.PropertyType.GetMethod("Parse", [typeof(string)]);

                    if (info != null)
                    {
                        Properties.ApplyFromGump(
                            m_Mobile, m_Object, m_Property, info.Invoke(null, [m_Names[index]]), m_PropertiesGump
                        );
                    }
                    else if (m_Property.PropertyType == typeof(Enum) || m_Property.PropertyType.IsSubclassOf(typeof(Enum)))
                    {
                        Properties.ApplyFromGump(
                            m_Mobile,
                            m_Object,
                            m_Property,
                            Enum.Parse(m_Property.PropertyType, m_Names[index], false),
                            m_PropertiesGump
                        );
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
