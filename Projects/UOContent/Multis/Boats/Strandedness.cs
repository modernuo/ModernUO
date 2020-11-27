namespace Server.Misc
{
    public static class Strandedness
    {
        private static readonly Point2D[] m_Felucca =
        {
            new(2528, 3568), new(2376, 3400), new(2528, 3896),
            new(2168, 3904), new(1136, 3416), new(1432, 3648),
            new(1416, 4000), new(4512, 3936), new(4440, 3120),
            new(4192, 3672), new(4720, 3472), new(3744, 2768),
            new(3480, 2432), new(3560, 2136), new(3792, 2112),
            new(2800, 2296), new(2736, 2016), new(4576, 1456),
            new(4680, 1152), new(4304, 1104), new(4496, 984),
            new(4248, 696), new(4040, 616), new(3896, 248),
            new(4176, 384), new(3672, 1104), new(3520, 1152),
            new(3720, 1360), new(2184, 2152), new(1952, 2088),
            new(2056, 1936), new(1720, 1992), new(472, 2064),
            new(656, 2096), new(3008, 3592), new(2784, 3472),
            new(5456, 2400), new(5976, 2424), new(5328, 3112),
            new(5792, 3152), new(2120, 3616), new(2136, 3128),
            new(1632, 3528), new(1328, 3160), new(1072, 3136),
            new(1128, 2976), new(960, 2576), new(752, 1832),
            new(184, 1488), new(592, 1440), new(368, 1216),
            new(232, 752), new(696, 744), new(304, 1000),
            new(840, 376), new(1192, 624), new(1200, 192),
            new(1512, 240), new(1336, 456), new(1536, 648),
            new(1104, 952), new(1864, 264), new(2136, 200),
            new(2160, 528), new(1904, 512), new(2240, 784),
            new(2536, 776), new(2488, 216), new(2336, 72),
            new(2648, 288), new(2680, 576), new(2896, 88),
            new(2840, 344), new(3136, 72), new(2968, 520),
            new(3192, 328), new(3448, 208), new(3432, 608),
            new(3184, 752), new(2800, 704), new(2768, 1016),
            new(2448, 1232), new(2272, 920), new(2072, 1080),
            new(2048, 1264), new(1808, 1528), new(1496, 1880),
            new(1656, 2168), new(2096, 2320), new(1816, 2528),
            new(1840, 2640), new(1928, 2952), new(2120, 2712)
        };

        private static readonly Point2D[] m_Trammel = m_Felucca;

        private static readonly Point2D[] m_Ilshenar =
        {
            new(1252, 1180), new(1562, 1090), new(1444, 1016),
            new(1324, 968), new(1418, 806), new(1722, 874),
            new(1456, 684), new(1036, 866), new(612, 476),
            new(1476, 372), new(762, 472), new(812, 1162),
            new(1422, 1144), new(1254, 1066), new(1598, 870),
            new(1358, 866), new(510, 302), new(510, 392)
        };

        private static readonly Point2D[] m_Tokuno =
        {
            // Makoto-Jima
            new(837, 1351), new(941, 1241), new(959, 1185),
            new(923, 1091), new(904, 983), new(845, 944),
            new(829, 896), new(794, 852), new(766, 821),
            new(695, 814), new(576, 835), new(518, 840),
            new(519, 902), new(502, 950), new(503, 1045),
            new(547, 1131), new(518, 1204), new(506, 1243),
            new(526, 1271), new(562, 1295), new(616, 1335),
            new(789, 1347), new(712, 1359),

            // Homare-Jima
            new(202, 498), new(116, 600), new(107, 699),
            new(162, 799), new(158, 889), new(169, 989),
            new(194, 1101), new(250, 1163), new(295, 1176),
            new(280, 1194), new(286, 1102), new(250, 1000),
            new(260, 906), new(360, 838), new(389, 763),
            new(415, 662), new(500, 597), new(570, 572),
            new(631, 577), new(692, 500), new(723, 445),
            new(672, 379), new(626, 332), new(494, 291),
            new(371, 336), new(324, 334), new(270, 362),

            // Isamu-Jima
            new(1240, 1076), new(1189, 1115), new(1046, 1039),
            new(1025, 885), new(907, 809), new(840, 506),
            new(799, 396), new(720, 258), new(744, 158),
            new(904, 37), new(974, 91), new(1020, 187),
            new(1035, 288), new(1104, 395), new(1215, 462),
            new(1275, 488), new(1348, 611), new(1363, 739),
            new(1364, 765), new(1364, 876), new(1300, 936),
            new(1240, 1003)
        };

        public static void Initialize()
        {
            EventSink.Login += EventSink_Login;
        }

        private static bool IsStranded(Mobile from)
        {
            var map = from.Map;

            if (map == null)
            {
                return false;
            }

            var surface = map.GetTopSurface(from.Location);

            if (surface is LandTile tile)
            {
                var id = tile.ID;

                return id >= 168 && id <= 171
                       || id >= 310 && id <= 311;
            }

            if (surface is StaticTile staticTile)
            {
                var id = staticTile.ID;

                return id >= 0x1796 && id <= 0x17B2;
            }

            return false;
        }

        public static void EventSink_Login(Mobile from)
        {
            if (!IsStranded(from))
            {
                return;
            }

            var map = from.Map;

            Point2D[] list;

            if (map == Map.Felucca)
            {
                list = m_Felucca;
            }
            else if (map == Map.Trammel)
            {
                list = m_Trammel;
            }
            else if (map == Map.Ilshenar)
            {
                list = m_Ilshenar;
            }
            else if (map == Map.Tokuno)
            {
                list = m_Tokuno;
            }
            else
            {
                return;
            }

            var p = Point2D.Zero;
            var pdist = double.MaxValue;

            for (var i = 0; i < list.Length; ++i)
            {
                var dist = from.GetDistanceToSqrt(list[i]);

                if (dist < pdist)
                {
                    p = list[i];
                    pdist = dist;
                }
            }

            int x = p.X, y = p.Y;
            int z;
            bool canFit;

            z = map.GetAverageZ(x, y);
            canFit = map.CanSpawnMobile(x, y, z);

            for (var i = 1; !canFit && i <= 40; i += 2)
            {
                for (var xo = -1; !canFit && xo <= 1; ++xo)
                {
                    for (var yo = -1; !canFit && yo <= 1; ++yo)
                    {
                        if (xo == 0 && yo == 0)
                        {
                            continue;
                        }

                        x = p.X + xo * i;
                        y = p.Y + yo * i;
                        z = map.GetAverageZ(x, y);
                        canFit = map.CanSpawnMobile(x, y, z);
                    }
                }
            }

            if (canFit)
            {
                from.Location = new Point3D(x, y, z);
            }
        }
    }
}
