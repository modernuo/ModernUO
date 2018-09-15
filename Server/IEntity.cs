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
  public interface IEntity : IPoint3D, IComparable, IComparable<IEntity>
  {
    Serial Serial{ get; }
    Point3D Location{ get; }
    Map Map{ get; }
    bool Deleted{ get; }
    void MoveToWorld(Point3D location, Map map);

    void Delete();
    void ProcessDelta();
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

    public int CompareTo(Entity other)
    {
      return CompareTo((IEntity)other);
    }

    public int CompareTo(IEntity other)
    {
      if (other == null)
        return -1;

      return Serial.CompareTo(other.Serial);
    }

    public int CompareTo(object other)
    {
      if (other == null || other is IEntity)
        return CompareTo((IEntity)other);

      throw new ArgumentException();
    }

    public Serial Serial{ get; }

    public Point3D Location{ get; private set; }

    public int X => Location.X;

    public int Y => Location.Y;

    public int Z => Location.Z;

    public Map Map{ get; private set; }

    public virtual void MoveToWorld(Point3D newLocation, Map map)
    {
      Location = newLocation;
      Map = map;
    }

    public bool Deleted{ get; }

    public void Delete()
    {
    }

    public void ProcessDelta()
    {
    }
  }
}