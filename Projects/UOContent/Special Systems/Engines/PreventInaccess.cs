using System.Collections.Generic;

namespace Server.Misc
{
    /*
     * This system prevents the inability for server staff to
     * access their server due to data overflows during login.
     *
     * Whenever a staff character's NetState is disposed right after
     * the login process, the character is moved to and logged out
     * at a "safe" alternative.
     *
     * The location the character was moved from will be reported
     * to the player upon the next successful login.
     *
     * This system does not affect non-staff players.
     */
    public static class PreventInaccess
    {
        public static readonly bool Enabled = true;

        private static readonly LocationInfo[] m_Destinations =
        {
            new(new Point3D(5275, 1163, 0), Map.Felucca), // Jail
            new(new Point3D(5275, 1163, 0), Map.Trammel),
            new(new Point3D(5445, 1153, 0), Map.Felucca), // Green acres
            new(new Point3D(5445, 1153, 0), Map.Trammel)
        };

        private static Dictionary<Mobile, LocationInfo> m_MoveHistory;

        public static void Initialize()
        {
            m_MoveHistory = new Dictionary<Mobile, LocationInfo>();

            if (Enabled)
            {
                EventSink.Login += OnLogin;
            }
        }

        public static void OnLogin(Mobile from)
        {
            if (from == null || from.AccessLevel < AccessLevel.Counselor)
            {
                return;
            }

            if (HasDisconnected(from))
            {
                if (!m_MoveHistory.ContainsKey(from))
                {
                    m_MoveHistory[from] = new LocationInfo(from.Location, from.Map);
                }

                var dest = GetRandomDestination();

                from.Location = dest.Location;
                from.Map = dest.Map;
            }
            else if (m_MoveHistory.Remove(from, out var orig))
            {
                from.SendMessage(
                    $"Your character was moved from {orig.Location} ({orig.Map}) due to a detected client crash."
                );
            }
        }

        private static bool HasDisconnected(Mobile m) => m.NetState?.Connection == null;

        private static LocationInfo GetRandomDestination() => m_Destinations.RandomElement();

        private class LocationInfo
        {
            public LocationInfo(Point3D loc, Map map)
            {
                Location = loc;
                Map = map;
            }

            public Point3D Location { get; }

            public Map Map { get; }
        }
    }
}
