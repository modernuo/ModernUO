/***************************************************************************
 *                                Serial.cs
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
	public struct Serial : IComparable, IComparable<Serial>
	{
		public static Serial LastMobile { get; private set; } = Zero;

		public static Serial LastItem { get; private set; } = 0x40000000;

		public static readonly Serial MinusOne = new Serial( -1 );
		public static readonly Serial Zero = new Serial( 0 );

		public static Serial NewMobile
		{
			get
			{
				while ( World.FindMobile( LastMobile = (LastMobile + 1) ) != null );

				return LastMobile;
			}
		}

		public static Serial NewItem
		{
			get
			{
				while ( World.FindItem( LastItem = (LastItem + 1) ) != null );

				return LastItem;
			}
		}

		private Serial( int serial )
		{
			Value = serial;
		}

		public int Value { get; }

		public bool IsMobile => ( Value > 0 && Value < 0x40000000 );

		public bool IsItem => ( Value >= 0x40000000 && Value <= 0x7FFFFFFF );

		public bool IsValid => ( Value > 0 );

		public override int GetHashCode()
		{
			return Value;
		}

		public int CompareTo( Serial other )
		{
			return Value.CompareTo( other.Value );
		}

		public int CompareTo( object other )
		{
			if ( other is Serial serial )
				return CompareTo( serial );

			if ( other == null )
				return -1;

			throw new ArgumentException();
		}

		public override bool Equals( object o )
		{
			if ( !(o is Serial serial) )
				return false;

			return serial.Value == Value;
		}

		public static bool operator == ( Serial l, Serial r )
		{
			return l.Value == r.Value;
		}

		public static bool operator != ( Serial l, Serial r )
		{
			return l.Value != r.Value;
		}

		public static bool operator > ( Serial l, Serial r )
		{
			return l.Value > r.Value;
		}

		public static bool operator < ( Serial l, Serial r )
		{
			return l.Value < r.Value;
		}

		public static bool operator >= ( Serial l, Serial r )
		{
			return l.Value >= r.Value;
		}

		public static bool operator <= ( Serial l, Serial r )
		{
			return l.Value <= r.Value;
		}

		/*public static Serial operator ++ ( Serial l )
		{
			return new Serial( l + 1 );
		}*/

		public override string ToString()
		{
			return $"0x{Value:X8}";
		}

		public static implicit operator int( Serial a )
		{
			return a.Value;
		}

		public static implicit operator Serial( int a )
		{
			return new Serial( a );
		}
	}
}
