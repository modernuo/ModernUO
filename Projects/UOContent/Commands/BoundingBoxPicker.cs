using Server.Targeting;

namespace Server
{
    public delegate void BoundingBoxCallback(Map map, Point3D start, Point3D end);

    public static class BoundingBoxPicker
    {
        public static void Begin(Mobile from, BoundingBoxCallback callback)
        {
            from.SendMessage("Target the first location of the bounding box.");
            from.Target = new PickTarget(callback);
        }

        private class PickTarget : Target
        {
            private readonly BoundingBoxCallback m_Callback;
            private readonly bool m_First;
            private readonly Map m_Map;
            private readonly Point3D m_Store;

            public PickTarget(BoundingBoxCallback callback) : this(Point3D.Zero, true, null, callback)
            {
            }

            public PickTarget(Point3D store, bool first, Map map, BoundingBoxCallback callback) : base(
                -1,
                true,
                TargetFlags.None
            )
            {
                m_Store = store;
                m_First = first;
                m_Map = map;
                m_Callback = callback;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is not IPoint3D ip)
                {
                    return;
                }

                Point3D p = ip switch
                {
                    Item item => item.GetWorldTop(),
                    Mobile m  => m.Location,
                    _         => new Point3D(ip)
                };

                if (m_First)
                {
                    from.SendMessage("Target another location to complete the bounding box.");
                    from.Target = new PickTarget(p, false, from.Map, m_Callback);
                }
                else if (from.Map != m_Map)
                {
                    from.SendMessage("Both locations must reside on the same map.");
                }
                else if (m_Map != null && m_Map != Map.Internal && m_Callback != null)
                {
                    var start = m_Store;
                    var end = p;

                    Utility.FixPoints(ref start, ref end);

                    m_Callback(m_Map, start, end);
                }
            }
        }
    }
}
