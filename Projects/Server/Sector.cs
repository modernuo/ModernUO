/***************************************************************************
 *                                 Sector.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using Server.Items;
using Server.Network;

namespace Server
{
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
        private static readonly List<Mobile> m_DefaultMobileList = new List<Mobile>();
        private static readonly List<Item> m_DefaultItemList = new List<Item>();
        private static readonly List<NetState> m_DefaultClientList = new List<NetState>();
        private static readonly List<BaseMulti> m_DefaultMultiList = new List<BaseMulti>();
        private static readonly List<RegionRect> m_DefaultRectList = new List<RegionRect>();
        private bool m_Active;
        private List<NetState> m_Clients;
        private List<Item> m_Items;
        private List<Mobile> m_Mobiles;
        private List<BaseMulti> m_Multis;
        private List<Mobile> m_Players;
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

        public List<Mobile> Players => m_Players ?? m_DefaultMobileList;

        public bool Active => m_Active && Owner != Map.Internal;

        public Map Owner { get; }

        public int X { get; }

        public int Y { get; }

        private void Add<T>(ref List<T> list, T value)
        {
            list ??= new List<T>();

            list.Add(value);
        }

        private void Remove<T>(ref List<T> list, T value)
        {
            if (list != null)
            {
                list.Remove(value);

                if (list.Count == 0) list = null;
            }
        }

        private void Replace<T>(ref List<T> list, T oldValue, T newValue)
        {
            if (oldValue != null && newValue != null)
            {
                var index = list?.IndexOf(oldValue) ?? -1;

                if (index >= 0)
                    list[index] = newValue;
                else
                    Add(ref list, newValue);
            }
            else if (oldValue != null)
            {
                Remove(ref list, oldValue);
            }
            else if (newValue != null)
            {
                Add(ref list, newValue);
            }
        }

        public void OnClientChange(NetState oldState, NetState newState)
        {
            Replace(ref m_Clients, oldState, newState);
        }

        public void OnEnter(Item item)
        {
            Add(ref m_Items, item);
        }

        public void OnLeave(Item item)
        {
            Remove(ref m_Items, item);
        }

        public void OnEnter(Mobile mob)
        {
            Add(ref m_Mobiles, mob);

            if (mob.NetState != null) Add(ref m_Clients, mob.NetState);

            if (mob.Player)
            {
                if (m_Players == null) Owner.ActivateSectors(X, Y);

                Add(ref m_Players, mob);
            }
        }

        public void OnLeave(Mobile mob)
        {
            Remove(ref m_Mobiles, mob);

            if (mob.NetState != null) Remove(ref m_Clients, mob.NetState);

            if (mob.Player && m_Players != null)
            {
                Remove(ref m_Players, mob);

                if (m_Players == null) Owner.DeactivateSectors(X, Y);
            }
        }

        public void OnEnter(Region region, Rectangle3D rect)
        {
            Add(ref m_RegionRects, new RegionRect(region, rect));

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

                    if (regRect.Region == region) m_RegionRects.RemoveAt(i);
                }

                if (m_RegionRects.Count == 0) m_RegionRects = null;
            }

            UpdateMobileRegions();
        }

        private void UpdateMobileRegions()
        {
            if (m_Mobiles != null)
            {
                var sandbox = new List<Mobile>(m_Mobiles);

                foreach (var mob in sandbox) mob.UpdateRegion();
            }
        }

        public void OnMultiEnter(BaseMulti multi)
        {
            Add(ref m_Multis, multi);
        }

        public void OnMultiLeave(BaseMulti multi)
        {
            Remove(ref m_Multis, multi);
        }

        public void Activate()
        {
            if (!Active && Owner != Map.Internal)
            {
                if (m_Items != null)
                    foreach (var item in m_Items)
                        item.OnSectorActivate();

                if (m_Mobiles != null)
                    foreach (var mob in m_Mobiles)
                        mob.OnSectorActivate();

                m_Active = true;
            }
        }

        public void Deactivate()
        {
            if (Active)
            {
                if (m_Items != null)
                    foreach (var item in m_Items)
                        item.OnSectorDeactivate();

                if (m_Mobiles != null)
                    foreach (var mob in m_Mobiles)
                        mob.OnSectorDeactivate();

                m_Active = false;
            }
        }
    }
}
