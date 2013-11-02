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
		bool Deleted { get; }

		void Delete();
		void ProcessDelta();
	}

	public class Entity : IEntity, IComparable<Entity>
	{
		public int CompareTo( IEntity other )
		{
			if ( other == null )
				return -1;

			return m_Serial.CompareTo( other.Serial );
		}

		public int CompareTo( Entity other )
		{
			return this.CompareTo( (IEntity) other );
		}

		public int CompareTo( object other )
		{
			if ( other == null || other is IEntity )
				return this.CompareTo( (IEntity) other );

			throw new ArgumentException();
		}

		private Serial m_Serial;
		private Point3D m_Location;
		private Map m_Map;
		private bool m_Deleted;

		public Entity( Serial serial, Point3D loc, Map map )
		{
			m_Serial = serial;
			m_Location = loc;
			m_Map = map;
			m_Deleted = false;
		}

		public Serial Serial {
			get {
				return m_Serial;
			}
		}

		public Point3D Location {
			get {
				return m_Location;
			}
		}

		public int X {
			get {
				return m_Location.X;
			}
		}

		public int Y {
			get {
				return m_Location.Y;
			}
		}

		public int Z {
			get {
				return m_Location.Z;
			}
		}

		public Map Map {
			get {
				return m_Map;
			}
		}

		public bool Deleted {
			get {
				return m_Deleted;
			}
		}

		public void Delete()
		{
		}

		public void ProcessDelta()
		{
		}
	}
}