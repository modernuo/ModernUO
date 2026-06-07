using System.Reflection;
using Server.Commands;
using Server.Network;
using Server.Targeting;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class SetPoint3DGump : Gump
    {
        private const int CoordWidth = 70;
        private const int EntryWidth = CoordWidth + OffsetSize + CoordWidth + OffsetSize + CoordWidth;

        private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

        private readonly Mobile m_Mobile;
        private readonly object m_Object;
        private readonly PropertyInfo m_Property;
        private readonly PropertiesGump m_PropertiesGump;

        public SetPoint3DGump(PropertyInfo prop, Mobile mobile, object o, PropertiesGump propertiesGump)
            : base(GumpOffsetX, GumpOffsetY)
        {
            m_PropertiesGump = propertiesGump;
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;

            var p = (Point3D)(prop?.GetValue(o, null) ?? new Point3D());

            AddPage(0);

            this.AddPropsFrame(TotalWidth, 4, out var x, out var y);
            this.AddPropsEntryLabel(ref x, ref y, EntryWidth, prop?.Name);
            PropsLayout.NextRow(ref x, ref y);
            this.AddPropsEntryButton(ref x, ref y, EntryWidth, "Use your location", true, 1);
            PropsLayout.NextRow(ref x, ref y);
            this.AddPropsEntryButton(ref x, ref y, EntryWidth, "Target a location", true, 2);
            PropsLayout.NextRow(ref x, ref y);

            AddImageTiled(x, y, CoordWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, CoordWidth - TextOffsetX, EntryHeight, TextHue, "X:");
            AddTextEntry(x + 16, y, CoordWidth - 16, EntryHeight, TextHue, 0, p.X.ToString());
            x += CoordWidth + OffsetSize;

            AddImageTiled(x, y, CoordWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, CoordWidth - TextOffsetX, EntryHeight, TextHue, "Y:");
            AddTextEntry(x + 16, y, CoordWidth - 16, EntryHeight, TextHue, 1, p.Y.ToString());
            x += CoordWidth + OffsetSize;

            AddImageTiled(x, y, CoordWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, CoordWidth - TextOffsetX, EntryHeight, TextHue, "Z:");
            AddTextEntry(x + 16, y, CoordWidth - 16, EntryHeight, TextHue, 2, p.Z.ToString());
            x += CoordWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 3);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            Point3D toSet;
            bool shouldSet, shouldSend;

            switch (info.ButtonID)
            {
                case 1: // Current location
                    {
                        toSet = m_Mobile.Location;
                        shouldSet = true;
                        shouldSend = true;

                        break;
                    }
                case 2: // Pick location
                    {
                        m_Mobile.Target = new InternalTarget(m_Property, m_Mobile, m_Object, m_PropertiesGump);

                        toSet = Point3D.Zero;
                        shouldSet = false;
                        shouldSend = false;

                        break;
                    }
                case 3: // Use values
                    {
                        toSet = new Point3D(
                            Utility.ToInt32(info.GetTextEntry(0)),
                            Utility.ToInt32(info.GetTextEntry(1)),
                            Utility.ToInt32(info.GetTextEntry(2))
                        );
                        shouldSet = true;
                        shouldSend = true;

                        break;
                    }
                default:
                    {
                        toSet = Point3D.Zero;
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

        private class InternalTarget : Target
        {
            private readonly Mobile m_Mobile;
            private readonly object m_Object;
            private readonly PropertyInfo m_Property;
            private readonly PropertiesGump m_PropertiesGump;

            public InternalTarget(PropertyInfo prop, Mobile mobile, object o, PropertiesGump propertiesGump)
                : base(-1, true, TargetFlags.None)
            {
                m_PropertiesGump = propertiesGump;
                m_Property = prop;
                m_Mobile = mobile;
                m_Object = o;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is IPoint3D p)
                {
                    try
                    {
                        CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, new Point3D(p).ToString());
                        m_Property.SetValue(m_Object, new Point3D(p), null);
                        m_PropertiesGump.OnValueChanged(m_Object, m_Property);
                    }
                    catch
                    {
                        m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                    }
                }
            }

            protected override void OnTargetFinish(Mobile from) => m_PropertiesGump.SendPropertiesGump();
        }
    }
}
