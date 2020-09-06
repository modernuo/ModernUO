/***************************************************************************
 *                                IEntity.cs
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

namespace Server
{
    public interface IEntity : IPoint3D, IComparable<IEntity>
    {
        Serial Serial { get; }
        Point3D Location { get; }
        Map Map { get; }
        bool Deleted { get; }
        void MoveToWorld(Point3D location, Map map);

        void Delete();
        void ProcessDelta();

        bool InRange(Point2D p, int range);

        bool InRange(Point3D p, int range);

        bool InRange(IPoint2D p, int range);
    }

    public class Entity : IEntity, IComparable<Entity>
    {
        public Entity(Serial serial, Point3D loc, Map map)
        {
            Serial = serial;
            Location = loc;
            Map = map;
            Deleted = false;
        }

        public int CompareTo(Entity other) => CompareTo((IEntity)other);

        public int CompareTo(IEntity other) => other == null ? -1 : Serial.CompareTo(other.Serial);

        public Serial Serial { get; }

        public Point3D Location { get; private set; }

        public int X => Location.X;

        public int Y => Location.Y;

        public int Z => Location.Z;

        public Map Map { get; private set; }

        public virtual void MoveToWorld(Point3D newLocation, Map map)
        {
            Location = newLocation;
            Map = map;
        }

        public bool Deleted { get; }

        public void Delete()
        {
        }

        public void ProcessDelta()
        {
        }

        public bool InRange(Point2D p, int range) =>
            p.m_X >= Location.m_X - range
            && p.m_X <= Location.m_X + range
            && p.m_Y >= Location.m_Y - range
            && p.m_Y <= Location.m_Y + range;

        public bool InRange(Point3D p, int range) =>
            p.m_X >= Location.m_X - range
            && p.m_X <= Location.m_X + range
            && p.m_Y >= Location.m_Y - range
            && p.m_Y <= Location.m_Y + range;

        public bool InRange(IPoint2D p, int range) =>
            p.X >= Location.m_X - range
            && p.X <= Location.m_X + range
            && p.Y >= Location.m_Y - range
            && p.Y <= Location.m_Y + range;
    }
}
