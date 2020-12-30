using System;
using System.Collections.Generic;
using Server.Items;
using Server.Network;

namespace Server.Misc
{
    public class Weather
    {
        private static Map[] m_Facets;
        private static readonly Dictionary<Map, List<Weather>> m_WeatherByFacet = new();
        private bool m_Active;
        private bool m_ExtremeTemperature;

        private int m_Stage;

        public Weather(
            Map facet, Rectangle2D[] area, int temperature, int chanceOfPercipitation, int chanceOfExtremeTemperature,
            TimeSpan interval
        )
        {
            Facet = facet;
            Area = area;
            Temperature = temperature;
            ChanceOfPercipitation = chanceOfPercipitation;
            ChanceOfExtremeTemperature = chanceOfExtremeTemperature;

            var list = GetWeatherList(facet);

            list?.Add(this);

            Timer.DelayCall(
                TimeSpan.FromSeconds((0.2 + Utility.RandomDouble() * 0.8) * interval.TotalSeconds),
                interval,
                OnTick
            );
        }

        public Map Facet { get; }

        public Rectangle2D[] Area { get; set; }

        public int Temperature { get; set; }

        public int ChanceOfPercipitation { get; set; }

        public int ChanceOfExtremeTemperature { get; set; }

        // For dynamic weather:

        public Rectangle2D Bounds { get; set; }

        public int MoveSpeed { get; set; }

        public int MoveAngleX { get; set; }

        public int MoveAngleY { get; set; }

        public static void Initialize()
        {
            m_Facets = new[] { Map.Felucca, Map.Trammel };

            /* Static weather:
             *
             * Format:
             *   AddWeather( temperature, chanceOfPercipitation, chanceOfExtremeTemperature, <area ...> );
             */

            // ice island
            AddWeather(
                -15,
                100,
                5,
                new Rectangle2D(3850, 160, 390, 320),
                new Rectangle2D(3900, 480, 380, 180),
                new Rectangle2D(4160, 660, 150, 110)
            );

            // covetous entrance, around vesper and minoc
            AddWeather(+15, 50, 5, new Rectangle2D(2425, 725, 250, 250));

            // despise entrance, north of britain
            AddWeather(+15, 50, 5, new Rectangle2D(1245, 1045, 250, 250));

            /* Dynamic weather:
             *
             * Format:
             *   AddDynamicWeather( temperature, chanceOfPercipitation, chanceOfExtremeTemperature, moveSpeed, width, height, bounds );
             */

            for (var i = 0; i < 15; ++i)
            {
                AddDynamicWeather(+15, 100, 5, 8, 400, 400, new Rectangle2D(0, 0, 5120, 4096));
            }
        }

        public static List<Weather> GetWeatherList(Map facet)
        {
            if (facet == null)
            {
                return null;
            }

            if (!m_WeatherByFacet.TryGetValue(facet, out var list))
            {
                m_WeatherByFacet[facet] = list = new List<Weather>();
            }

            return list;
        }

        public static void AddDynamicWeather(
            int temperature, int chanceOfPercipitation, int chanceOfExtremeTemperature, int moveSpeed, int width, int height,
            Rectangle2D bounds
        )
        {
            for (var i = 0; i < m_Facets.Length; ++i)
            {
                var area = new Rectangle2D();
                var isValid = false;

                for (var j = 0; j < 10; ++j)
                {
                    area = new Rectangle2D(
                        bounds.X + Utility.Random(bounds.Width - width),
                        bounds.Y + Utility.Random(bounds.Height - height),
                        width,
                        height
                    );

                    if (!CheckWeatherConflict(m_Facets[i], null, area))
                    {
                        isValid = true;
                    }

                    if (isValid)
                    {
                        break;
                    }
                }

                if (!isValid)
                {
                    continue;
                }

                new Weather(
                        m_Facets[i],
                        new[] { area },
                        temperature,
                        chanceOfPercipitation,
                        chanceOfExtremeTemperature,
                        TimeSpan.FromSeconds(30.0)
                    )
                    { Bounds = bounds, MoveSpeed = moveSpeed };
            }
        }

        public static void AddWeather(
            int temperature, int chanceOfPercipitation, int chanceOfExtremeTemperature, params Rectangle2D[] area
        )
        {
            for (var i = 0; i < m_Facets.Length; ++i)
            {
                new Weather(
                    m_Facets[i],
                    area,
                    temperature,
                    chanceOfPercipitation,
                    chanceOfExtremeTemperature,
                    TimeSpan.FromSeconds(30.0)
                );
            }
        }

        public static bool CheckWeatherConflict(Map facet, Weather exclude, Rectangle2D area)
        {
            var list = GetWeatherList(facet);

            if (list == null)
            {
                return false;
            }

            for (var i = 0; i < list.Count; ++i)
            {
                var w = list[i];

                if (w != exclude && w.IntersectsWith(area))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CheckIntersection(Rectangle2D r1, Rectangle2D r2) => r1.X < r2.X + r2.Width &&
                                                                                r2.X < r1.X + r1.Width &&
                                                                                r1.Y < r2.Y + r2.Height &&
                                                                                r2.Y < r1.Y + r1.Height;

        public static bool CheckContains(Rectangle2D big, Rectangle2D small) =>
            small.X >= big.X && small.Y >= big.Y && small.X + small.Width <= big.X + big.Width
            && small.Y + small.Height <= big.Y + big.Height;

        public virtual bool IntersectsWith(Rectangle2D area)
        {
            for (var i = 0; i < Area.Length; ++i)
            {
                if (CheckIntersection(area, Area[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void Reposition()
        {
            if (Area.Length == 0)
            {
                return;
            }

            var width = Area[0].Width;
            var height = Area[0].Height;

            var area = new Rectangle2D();
            var isValid = false;

            for (var j = 0; j < 10; ++j)
            {
                area = new Rectangle2D(
                    Bounds.X + Utility.Random(Bounds.Width - width),
                    Bounds.Y + Utility.Random(Bounds.Height - height),
                    width,
                    height
                );

                if (!CheckWeatherConflict(Facet, this, area))
                {
                    isValid = true;
                }

                if (isValid)
                {
                    break;
                }
            }

            if (!isValid)
            {
                return;
            }

            Area[0] = area;
        }

        public virtual void RecalculateMovementAngle()
        {
            var angle = Utility.RandomDouble() * Math.PI * 2.0;

            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            MoveAngleX = (int)(100 * cos);
            MoveAngleY = (int)(100 * sin);
        }

        public virtual void MoveForward()
        {
            if (Area.Length == 0)
            {
                return;
            }

            for (var i = 0; i < 5; ++i) // try 5 times to find a valid spot
            {
                var xOffset = MoveSpeed * MoveAngleX / 100;
                var yOffset = MoveSpeed * MoveAngleY / 100;

                var oldArea = Area[0];
                var newArea = new Rectangle2D(oldArea.X + xOffset, oldArea.Y + yOffset, oldArea.Width, oldArea.Height);

                if (!CheckWeatherConflict(Facet, this, newArea) && CheckContains(Bounds, newArea))
                {
                    Area[0] = newArea;
                    break;
                }

                RecalculateMovementAngle();
            }
        }

        public virtual void OnTick()
        {
            if (m_Stage == 0)
            {
                m_Active = ChanceOfPercipitation > Utility.Random(100);
                m_ExtremeTemperature = ChanceOfExtremeTemperature > Utility.Random(100);

                if (MoveSpeed > 0)
                {
                    Reposition();
                    RecalculateMovementAngle();
                }
            }

            if (m_Active)
            {
                if (m_Stage > 0 && MoveSpeed > 0)
                {
                    MoveForward();
                }

                int type, density;
                var temperature = Temperature;

                if (m_ExtremeTemperature)
                {
                    temperature *= -1;
                }

                if (m_Stage < 15)
                {
                    density = m_Stage * 5;
                }
                else
                {
                    density = Math.Clamp(150 - m_Stage * 5, 10, 70);
                }

                if (density == 0)
                {
                    type = 0xFE;
                }
                else if (temperature > 0)
                {
                    type = 0;
                }
                else
                {
                    type = 2;
                }

                foreach (var ns in TcpServer.Instances)
                {
                    var mob = ns.Mobile;

                    if (mob == null || mob.Map != Facet)
                    {
                        continue;
                    }

                    var contains = Area.Length == 0;

                    for (var j = 0; !contains && j < Area.Length; ++j)
                    {
                        contains = Area[j].Contains(mob.Location);
                    }

                    if (!contains)
                    {
                        continue;
                    }

                    ns.SendWeather((byte)type, (byte)density, (byte)temperature);
                }
            }

            m_Stage++;
            m_Stage %= 30;
        }
    }

    public class WeatherMap : MapItem
    {
        [Constructible]
        public WeatherMap()
        {
            SetDisplay(0, 0, 5119, 4095, 400, 400);
        }

        public WeatherMap(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "weather map";

        public override void OnDoubleClick(Mobile from)
        {
            var facet = from.Map;

            if (facet == null)
            {
                return;
            }

            var list = Weather.GetWeatherList(facet);

            ClearPins();

            for (var i = 0; i < list.Count; ++i)
            {
                var w = list[i];

                for (var j = 0; j < w.Area.Length; ++j)
                {
                    AddWorldPin(w.Area[j].X + w.Area[j].Width / 2, w.Area[j].Y + w.Area[j].Height / 2);
                }
            }

            base.OnDoubleClick(from);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
