using System.Reflection;
using Server.Commands;
using Server.Network;
using Server.Targeting;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class SetPoint2DGump : Gump
    {
        private static readonly int CoordWidth = 105;
        private static readonly int EntryWidth = CoordWidth + OffsetSize + CoordWidth;

        private static readonly int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
        private static readonly int TotalHeight = OffsetSize + 4 * (EntryHeight + OffsetSize);

        private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;
        private static readonly int BackHeight = BorderSize + TotalHeight + BorderSize;
        private readonly Mobile m_Mobile;
        private readonly object m_Object;
        private readonly PropertyInfo m_Property;
        private readonly PropertiesGump m_PropertiesGump;

        public SetPoint2DGump(
            PropertyInfo prop, Mobile mobile, object o, PropertiesGump propertiesGump
        )
            : base(GumpOffsetX, GumpOffsetY)
        {
            m_PropertiesGump = propertiesGump;
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;

            var p = (Point2D)(prop?.GetValue(o, null) ?? new Point2D());

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
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, prop?.Name);
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            x = BorderSize + OffsetSize;
            y += EntryHeight + OffsetSize;

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Use your location");
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 1);

            x = BorderSize + OffsetSize;
            y += EntryHeight + OffsetSize;

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Target a location");
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 2);

            x = BorderSize + OffsetSize;
            y += EntryHeight + OffsetSize;

            AddImageTiled(x, y, CoordWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, CoordWidth - TextOffsetX, EntryHeight, TextHue, "X:");
            AddTextEntry(x + 16, y, CoordWidth - 16, EntryHeight, TextHue, 0, p.X.ToString());
            x += CoordWidth + OffsetSize;

            AddImageTiled(x, y, CoordWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, CoordWidth - TextOffsetX, EntryHeight, TextHue, "Y:");
            AddTextEntry(x + 16, y, CoordWidth - 16, EntryHeight, TextHue, 1, p.Y.ToString());
            x += CoordWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 3);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Point2D toSet;
            bool shouldSet, shouldSend;

            switch (info.ButtonID)
            {
                case 1: // Current location
                    {
                        toSet = new Point2D(m_Mobile.Location.X, m_Mobile.Location.Y);
                        shouldSet = true;
                        shouldSend = true;

                        break;
                    }
                case 2: // Pick location
                    {
                        m_Mobile.Target = new InternalTarget(m_Property, m_Mobile, m_Object, m_PropertiesGump);

                        toSet = Point2D.Zero;
                        shouldSet = false;
                        shouldSend = false;

                        break;
                    }
                case 3: // Use values
                    {
                        var x = info.GetTextEntry(0);
                        var y = info.GetTextEntry(1);

                        toSet = new Point2D(
                            x == null ? 0 : Utility.ToInt32(x.Text),
                            y == null ? 0 : Utility.ToInt32(y.Text)
                        );
                        shouldSet = true;
                        shouldSend = true;

                        break;
                    }
                default:
                    {
                        toSet = Point2D.Zero;
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

            public InternalTarget(
                PropertyInfo prop, Mobile mobile, object o, PropertiesGump propertiesGump
            ) : base(-1, true, TargetFlags.None)
            {
                m_Property = prop;
                m_Mobile = mobile;
                m_Object = o;
                m_PropertiesGump = propertiesGump;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is IPoint3D point3D)
                {
                    try
                    {
                        var p = new Point2D(point3D.X, point3D.Y);
                        CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, p.ToString());
                        m_Property.SetValue(m_Object, p, null);
                        m_PropertiesGump.OnValueChanged(m_Object, m_Property);
                    }
                    catch
                    {
                        m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                    }
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_PropertiesGump.SendPropertiesGump();
            }
        }
    }
}
