/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseMulti.cs                                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Runtime.CompilerServices;

namespace Server.Items;

public abstract partial class BaseMulti : Item
{
    public BaseMulti(int itemID) : base(itemID) => Movable = false;

    public BaseMulti(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public override int ItemID
    {
        get => base.ItemID;
        set
        {
            if (base.ItemID != value)
            {
                var facet = Parent == null ? Map : null;

                facet?.OnLeave(this);

                base.ItemID = value;

                facet?.OnEnter(this);
            }
        }
    }

    public override int LabelNumber
    {
        get
        {
            var mcl = Components;

            if (mcl.List.Length > 0)
            {
                int id = mcl.List[0].ItemId;

                if (id < 0x4000)
                {
                    return 1020000 + id;
                }

                return 1078872 + id;
            }

            return base.LabelNumber;
        }
    }

    public virtual bool AllowsRelativeDrop => false;

    public virtual MultiComponentList Components => MultiData.GetComponents(ItemID);

    [Obsolete("Replace with calls to OnLeave and OnEnter surrounding component invalidation.", true)]
    public virtual void RefreshComponents()
    {
        if (Parent == null)
        {
            var facet = Map;

            if (facet != null)
            {
                facet.OnLeave(this);
                facet.OnEnter(this);
            }
        }
    }

    public override int GetMaxUpdateRange() => 22;

    public override int GetUpdateRange(Mobile m) => 22;

    public virtual bool Contains(Point2D p) => Contains(p.m_X, p.m_Y);

    public virtual bool Contains(Point3D p) => Contains(p.m_X, p.m_Y);

    public virtual bool Contains(IPoint3D p) => Contains(p.X, p.Y);

    public virtual bool Contains(int x, int y)
    {
        var mcl = Components;

        x -= X + mcl.Min.m_X;
        y -= Y + mcl.Min.m_Y;

        return x >= 0
               && x < mcl.Width
               && y >= 0
               && y < mcl.Height
               && mcl.Tiles[x][y].Length > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Mobile m) => m.Map == Map && Contains(m.X, m.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Item item) => item.Map == Map && Contains(item.X, item.Y);

    public bool Intersects(Rectangle2D bounds)
    {
        if (bounds.X > Location.X + Components.Max.X || bounds.X + bounds.Width < Location.X + Components.Min.X)
        {
            return false;
        }

        if (bounds.Y > Location.Y + Components.Max.Y || bounds.Y + bounds.Height < Location.Y + Components.Min.Y)
        {
            return false;
        }

        int minX = Math.Max(bounds.X, Location.X + Components.Min.X);
        int maxX = Math.Min(bounds.X + bounds.Width, Location.X + Components.Max.X);
        int minY = Math.Max(bounds.Y, Location.Y + Components.Min.Y);
        int maxY = Math.Min(bounds.Y + bounds.Height, Location.Y + Components.Max.Y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                int offsetX = x - Location.X - Components.Min.X;
                int offsetY = y - Location.Y - Components.Min.Y;

                if (offsetX < 0 || offsetY < 0 || offsetX >= Components.Width || offsetY >= Components.Height)
                {
                    continue;
                }

                // TODO: Use a ref struct
                var tiles = Components.Tiles[offsetX][offsetY];
                if (tiles.Length > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(1); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        if (version == 0)
        {
            if (ItemID >= 0x4000)
            {
                ItemID -= 0x4000;
            }
        }
    }
}
