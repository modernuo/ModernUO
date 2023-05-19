using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions;

public class BaseRegion : Region
{
    private static readonly List<Rectangle3D> m_RectBuffer1 = new();
    private static readonly List<Rectangle3D> m_RectBuffer2 = new();

    public BaseRegion(string name, Map map, int priority, params Rectangle2D[] area) : base(name, map, priority, area)
    {
    }

    [JsonConstructor] // Don't include parent, since it is special
    public BaseRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public BaseRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public BaseRegion(string name, Map map, Region parent, params Rectangle2D[] area) : base(name, map, parent, area)
    {
    }

    public BaseRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public bool ExcludeFromParentSpawns { get; set; }

    public Rectangle3D[] Rectangles { get; private set; }
    public int[] RectangleWeights { get; private set; }
    public int TotalWeight { get; private set; }

    public virtual bool YoungProtected => true;
    public virtual bool YoungMayEnter => true;
    public virtual bool MountsAllowed => true;
    public virtual bool DeadMayEnter => true;
    public virtual bool ResurrectionAllowed => true;
    public virtual bool LogoutAllowed => true;

    public string RuneName { get; set; }

    public bool NoLogoutDelay { get; set; }

    public static string GetRuneNameFor(Region region)
    {
        while (region != null)
        {
            var br = region as BaseRegion;

            if (br?.RuneName != null)
            {
                return br.RuneName;
            }

            region = region.Parent;
        }

        return null;
    }

    public override TimeSpan GetLogoutDelay(Mobile m) =>
        NoLogoutDelay && m.Aggressors.Count == 0 && m.Aggressed.Count == 0 && !m.Criminal
            ? TimeSpan.Zero
            : base.GetLogoutDelay(m);

    public override void OnEnter(Mobile m)
    {
        if (m is PlayerMobile mobile && mobile.Young && !YoungProtected)
        {
            mobile.SendGump(new YoungDungeonWarning());
        }
    }

    public override bool AcceptsSpawnsFrom(Region region) =>
        (region == this || !ExcludeFromParentSpawns) && base.AcceptsSpawnsFrom(region);

    // TODO: Clean this up
    public void InitRectangles()
    {
        if (Rectangles != null)
        {
            return;
        }

        // Test if area rectangles are overlapping, and in that case break them into smaller non overlapping rectangles
        for (var i = 0; i < Area.Length; i++)
        {
            m_RectBuffer2.Add(Area[i]);

            for (var j = 0; j < m_RectBuffer1.Count && m_RectBuffer2.Count > 0; j++)
            {
                var comp = m_RectBuffer1[j];

                for (var k = m_RectBuffer2.Count - 1; k >= 0; k--)
                {
                    var rect = m_RectBuffer2[k];

                    int l1 = rect.Start.X, r1 = rect.End.X, t1 = rect.Start.Y, b1 = rect.End.Y;
                    int l2 = comp.Start.X, r2 = comp.End.X, t2 = comp.Start.Y, b2 = comp.End.Y;

                    if (l1 < r2 && r1 > l2 && t1 < b2 && b1 > t2)
                    {
                        m_RectBuffer2.RemoveAt(k);

                        var sz = rect.Start.Z;
                        var ez = rect.End.X;

                        if (l1 < l2)
                        {
                            m_RectBuffer2.Add(new Rectangle3D(new Point3D(l1, t1, sz), new Point3D(l2, b1, ez)));
                        }

                        if (r1 > r2)
                        {
                            m_RectBuffer2.Add(new Rectangle3D(new Point3D(r2, t1, sz), new Point3D(r1, b1, ez)));
                        }

                        if (t1 < t2)
                        {
                            m_RectBuffer2.Add(
                                new Rectangle3D(
                                    new Point3D(Math.Max(l1, l2), t1, sz),
                                    new Point3D(Math.Min(r1, r2), t2, ez)
                                )
                            );
                        }

                        if (b1 > b2)
                        {
                            m_RectBuffer2.Add(
                                new Rectangle3D(
                                    new Point3D(Math.Max(l1, l2), b2, sz),
                                    new Point3D(Math.Min(r1, r2), b1, ez)
                                )
                            );
                        }
                    }
                }
            }

            m_RectBuffer1.AddRange(m_RectBuffer2);
            m_RectBuffer2.Clear();
        }

        Rectangles = m_RectBuffer1.ToArray();
        m_RectBuffer1.Clear();

        RectangleWeights = new int[Rectangles.Length];
        for (var i = 0; i < Rectangles.Length; i++)
        {
            var rect = Rectangles[i];
            var weight = rect.Width * rect.Height;

            RectangleWeights[i] = weight;
            TotalWeight += weight;
        }
    }

    public override string ToString() => Name ?? RuneName ?? GetType().Name;

    public virtual bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType, out TextDefinition message)
    {
        if (Parent is BaseRegion parent)
        {
            return parent.CheckTravel(m, newLocation, travelType, out message);
        }

        message = null;
        return true;
    }
}
