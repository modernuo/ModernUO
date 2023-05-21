using System;
using System.Collections.Generic;
using Server.Collections;
using Server.Items;
using Server.Network;

namespace Server;

public class RegionRect : IComparable<RegionRect>
{
    private Rectangle3D m_Rect;

    public RegionRect(Region region, Rectangle3D rect)
    {
        Region = region;
        m_Rect = rect;
    }

    public Region Region { get; }

    public Rectangle3D Rect => m_Rect;

    public int CompareTo(RegionRect regRect) => regRect == null ? 1 : Region.CompareTo(regRect.Region);

    public bool Contains(Point3D loc) => m_Rect.Contains(loc);
}

public class Sector
{
    // TODO: Can we avoid this?
    private static readonly List<Mobile> m_DefaultMobileList = new();
    private static readonly List<Item> m_DefaultItemList = new();
    private static readonly List<NetState> m_DefaultClientList = new();
    private static readonly List<BaseMulti> m_DefaultMultiList = new();
    private static readonly List<RegionRect> m_DefaultRectList = new();
    private bool m_Active;
    private List<NetState> m_Clients;
    private List<Item> m_Items;
    private List<Mobile> m_Mobiles;
    private List<BaseMulti> m_Multis;
    private List<RegionRect> m_RegionRects;

    public Sector(int x, int y, Map owner)
    {
        X = x;
        Y = y;
        Owner = owner;
        m_Active = false;
    }

    public List<RegionRect> RegionRects => m_RegionRects ?? m_DefaultRectList;

    public List<BaseMulti> Multis => m_Multis ?? m_DefaultMultiList;

    public List<Mobile> Mobiles => m_Mobiles ?? m_DefaultMobileList;

    public List<Item> Items => m_Items ?? m_DefaultItemList;

    public List<NetState> Clients => m_Clients ?? m_DefaultClientList;

    public bool Active => m_Active && Owner != Map.Internal;

    public Map Owner { get; }

    public int X { get; }

    public int Y { get; }

    public void OnClientChange(NetState oldState, NetState newState)
    {
        Utility.Replace(ref m_Clients, oldState, newState);
    }

    public void OnEnter(Item item)
    {
        Utility.Add(ref m_Items, item);
    }

    public void OnLeave(Item item)
    {
        Utility.Remove(ref m_Items, item);
    }

    public void OnEnter(Mobile mob)
    {
        Utility.Add(ref m_Mobiles, mob);

        if (mob.NetState != null)
        {
            Utility.Add(ref m_Clients, mob.NetState);

            Owner.ActivateSectors(X, Y);
        }
    }

    public void OnLeave(Mobile mob)
    {
        Utility.Remove(ref m_Mobiles, mob);

        if (mob.NetState != null)
        {
            Utility.Remove(ref m_Clients, mob.NetState);

            Owner.DeactivateSectors(X, Y);
        }
    }

    public void OnEnter(Region region, Rectangle3D rect)
    {
        Utility.Add(ref m_RegionRects, new RegionRect(region, rect));

        m_RegionRects.Sort();

        UpdateMobileRegions();
    }

    public void OnLeave(Region region)
    {
        if (m_RegionRects != null)
        {
            for (var i = m_RegionRects.Count - 1; i >= 0; i--)
            {
                var regRect = m_RegionRects[i];

                if (regRect.Region == region)
                {
                    m_RegionRects.RemoveAt(i);
                }
            }

            if (m_RegionRects.Count == 0)
            {
                m_RegionRects = null;
            }
        }

        UpdateMobileRegions();
    }

    private void UpdateMobileRegions()
    {
        if (m_Mobiles != null)
        {
            using var queue = PooledRefQueue<Mobile>.Create(m_Mobiles.Count);
            foreach (var mob in m_Mobiles)
            {
                queue.Enqueue(mob);
            }

            while (queue.Count > 0)
            {
                queue.Dequeue().UpdateRegion();
            }
        }
    }

    public void OnMultiEnter(BaseMulti multi)
    {
        Utility.Add(ref m_Multis, multi);
    }

    public void OnMultiLeave(BaseMulti multi)
    {
        Utility.Remove(ref m_Multis, multi);
    }

    public void Activate()
    {
        if (!Active)
        {
            if (m_Items != null)
            {
                foreach (var item in m_Items)
                {
                    item.OnSectorActivate();
                }
            }

            if (m_Mobiles != null)
            {
                foreach (var mob in m_Mobiles)
                {
                    mob.OnSectorActivate();
                }
            }

            m_Active = true;
        }
    }

    public void Deactivate()
    {
        if (Active)
        {
            if (m_Items != null)
            {
                foreach (var item in m_Items)
                {
                    item.OnSectorDeactivate();
                }
            }

            if (m_Mobiles != null)
            {
                foreach (var mob in m_Mobiles)
                {
                    mob.OnSectorDeactivate();
                }
            }

            m_Active = false;
        }
    }
}
