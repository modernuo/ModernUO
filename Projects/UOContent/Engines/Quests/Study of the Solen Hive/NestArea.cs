namespace Server.Engines.Quests.Naturalist
{
    public class NestArea
    {
        private static readonly NestArea[] m_Areas =
        {
            new(false, new Rectangle2D(5861, 1787, 26, 25)),

            new(
                false,
                new Rectangle2D(5734, 1788, 14, 50),
                new Rectangle2D(5748, 1800, 3, 34),
                new Rectangle2D(5751, 1808, 2, 20)
            ),

            new(false, new Rectangle2D(5907, 1908, 19, 43)),

            new(
                false,
                new Rectangle2D(5721, 1926, 24, 29),
                new Rectangle2D(5745, 1935, 7, 22)
            ),

            new(
                true,
                new Rectangle2D(5651, 1853, 21, 32),
                new Rectangle2D(5672, 1857, 6, 20)
            )
        };

        private readonly Rectangle2D[] m_Rects;

        private NestArea(bool special, params Rectangle2D[] rects)
        {
            Special = special;
            m_Rects = rects;
        }

        public static int NonSpecialCount
        {
            get
            {
                int count = 0;
                foreach (var area in m_Areas)
                {
                    if (!area.Special)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool Special { get; }

        public int ID
        {
            get
            {
                for (var i = 0; i < m_Areas.Length; i++)
                {
                    if (m_Areas[i] == this)
                    {
                        return i;
                    }
                }

                return 0;
            }
        }

        public static NestArea Find(Point3D p)
        {
            foreach (var area in m_Areas)
            {
                if (area.Contains(p))
                {
                    return area;
                }
            }

            return null;
        }

        public static NestArea GetByID(int id)
        {
            if (id >= 0 && id < m_Areas.Length)
            {
                return m_Areas[id];
            }

            return null;
        }

        public bool Contains(Point3D p)
        {
            var x = p.X;
            var y = p.Y;
            foreach (var rect in m_Rects)
            {
                if (rect.Contains(x, y))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
