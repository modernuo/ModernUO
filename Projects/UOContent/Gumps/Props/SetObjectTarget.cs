using System;
using System.Reflection;
using Server.Commands;
using Server.Items;
using Server.Targeting;

namespace Server.Gumps
{
    public class SetObjectTarget : Target
    {
        private readonly Mobile m_Mobile;
        private readonly object m_Object;
        private readonly PropertyInfo m_Property;
        private readonly Type m_Type;
        private readonly PropertiesGump m_PropertiesGump;

        public SetObjectTarget(
            PropertyInfo prop, Mobile mobile, object o, Type type, PropertiesGump propertiesGump
        ) : base(-1, false, TargetFlags.None)
        {
            m_PropertiesGump = propertiesGump;
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;
            m_Type = type;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            try
            {
                if (m_Type == typeof(Type))
                {
                    targeted = targeted.GetType();
                }
                else if ((m_Type == typeof(BaseAddon) || m_Type.IsAssignableFrom(typeof(BaseAddon))) &&
                         targeted is AddonComponent addonComponent)
                {
                    targeted = addonComponent.Addon;
                }

                if (m_Type.IsInstanceOfType(targeted))
                {
                    CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, targeted.ToString());
                    m_Property.SetValue(m_Object, targeted, null);
                    m_PropertiesGump.OnValueChanged(m_Object, m_Property);
                }
                else
                {
                    m_Mobile.SendMessage($"That cannot be assigned to a property of type : {m_Type.Name}");
                }
            }
            catch
            {
                m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
            }
        }

        protected override void OnTargetFinish(Mobile from)
        {
            if (m_Type == typeof(Type))
            {
                m_PropertiesGump.SendPropertiesGump();
            }
            else
            {
                from.SendGump(new SetObjectGump(m_Property, m_Mobile, m_Object, m_Type, m_PropertiesGump));
            }
        }
    }
}
