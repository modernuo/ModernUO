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
using System.Collections;
using System.Collections.Generic;
using Server.Items;
using Server.Network;

namespace Server
{
	public class RegionRect : IComparable
	{
		private Region m_Region;
		private Rectangle3D m_Rect;

		public Region Region{ get{ return m_Region; } }
		public Rectangle3D Rect{ get{ return m_Rect; } }

		public RegionRect( Region region, Rectangle3D rect )
		{
			m_Region = region;
			m_Rect = rect;
		}

		public bool Contains( Point3D loc )
		{
			return m_Rect.Contains( loc );
		}

		int IComparable.CompareTo( object obj )
		{
			if ( obj == null )
				return 1;

			RegionRect regRect = obj as RegionRect;

			if ( regRect == null )
				throw new ArgumentException( "obj is not a RegionRect", "obj" );

			return ((IComparable)m_Region).CompareTo( regRect.m_Region );
		}
	}


	public class Sector
	{
		private int m_X, m_Y;
		private Map m_Owner;
		private List<Mobile> m_Mobiles;
		private List<Mobile> m_Players;
		private List<Item> m_Items;
		private List<NetState> m_Clients;
		private List<BaseMulti> m_Multis;
		private List<RegionRect> m_RegionRects;
		private bool m_Active;

		// TODO: Can we avoid this?
		private static List<Mobile> m_DefaultMobileList = new List<Mobile>();
		private static List<Item> m_DefaultItemList = new List<Item>();
		private static List<NetState> m_DefaultClientList = new List<NetState>();
		private static List<BaseMulti> m_DefaultMultiList = new List<BaseMulti>();
		private static List<RegionRect> m_DefaultRectList = new List<RegionRect>();

		public Sector( int x, int y, Map owner )
		{
			m_X = x;
			m_Y = y;
			m_Owner = owner;
			m_Active = false;
		}

		public void OnClientChange( NetState oldState, NetState newState )
		{
			if ( m_Clients != null )
			{
				m_Clients.Remove( oldState );
				
				if ( newState == null && m_Clients.Count == 0 )
				{
					m_Clients = null;
					return;
				}
			}

			if ( newState != null )
			{
				if ( m_Clients == null )
					m_Clients = new List<NetState>();

				m_Clients.Add( newState );
			}
		}

		public void OnEnter( Mobile m )
		{
			if ( m_Mobiles == null )
				m_Mobiles = new List<Mobile>();

			m_Mobiles.Add( m );

			if ( m.NetState != null )
			{
				if ( m_Clients == null )
					m_Clients = new List<NetState>();

				m_Clients.Add( m.NetState );
			}
	
			if ( m.Player )
			{
				if ( m_Players == null )
				{
					m_Players = new List<Mobile>();
					m_Owner.ActivateSectors( m_X, m_Y );
				}

				m_Players.Add( m );		
			}
		}

		public void OnEnter( Item item )
		{
			if ( m_Items == null )
				m_Items = new List<Item>();

			m_Items.Add( item );
		}

		public void OnEnter( Region r, Rectangle3D rect )
		{
			if ( m_RegionRects == null )
				m_RegionRects = new List<RegionRect>();

			RegionRect regRect = new RegionRect( r, rect );

			m_RegionRects.Add( regRect );
			m_RegionRects.Sort();

			if ( m_Mobiles != null && m_Mobiles.Count > 0 )
			{
				List<Mobile> list = new List<Mobile>( m_Mobiles );

				for ( int i = 0; i < list.Count; ++i )
					list[i].UpdateRegion();
			}
		}

		public void OnMultiEnter( BaseMulti m )
		{
			if ( m_Multis == null )
				m_Multis = new List<BaseMulti>();

			m_Multis.Add( m );
		}

		public void OnMultiLeave( BaseMulti m )
		{
			m_Multis.Remove( m );

			if ( m_Multis.Count == 0 )
				m_Multis = null;
		}

		public void OnLeave( Region r )
		{
			for ( int i = m_RegionRects.Count - 1; i >= 0; i-- )
			{
				RegionRect regRect = m_RegionRects[i];

				if ( regRect.Region == r )
					m_RegionRects.RemoveAt( i );
			}

			if ( m_Mobiles != null && m_Mobiles.Count > 0 )
			{
				List<Mobile> list = new List<Mobile>( m_Mobiles );

				for ( int i = 0; i < list.Count; ++i )
					list[i].UpdateRegion();
			}

			if ( m_RegionRects.Count == 0 )
				m_RegionRects = null;
		}

		public void OnLeave( Mobile m )
		{
			m_Mobiles.Remove( m );

			if ( m_Clients != null && m.NetState != null )
			{
				m_Clients.Remove( m.NetState );

				if ( m_Clients.Count == 0 )
					m_Clients = null;
			}

			if ( m.Player )
			{
				m_Players.Remove( m );

				if ( m_Players.Count == 0 )
				{
					m_Owner.DeactivateSectors( m_X, m_Y );
					m_Players = null;
				}
			}

			if ( m_Mobiles.Count == 0 )
				m_Mobiles = null;
		}

		public void OnLeave( Item item )
		{
			m_Items.Remove( item );
			
			if ( m_Items.Count == 0 )
				m_Items = null;
		}

		public void Activate()
		{
			if ( !Active && m_Owner != Map.Internal )
			{
				for ( int i = 0; m_Items != null && i < m_Items.Count; i++ )
					m_Items[i].OnSectorActivate();

				for ( int i = 0; m_Mobiles != null && i < m_Mobiles.Count; i++ )
					m_Mobiles[i].OnSectorActivate();

				m_Active = true;
			}
		}

		public void Deactivate()
		{
			if ( Active )
			{
				for ( int i = 0; m_Items != null && i < m_Items.Count; i++ )
					m_Items[i].OnSectorDeactivate();

				for ( int i = 0; m_Mobiles != null && i < m_Mobiles.Count; i++ )
					m_Mobiles[i].OnSectorDeactivate();

				m_Active = false;
			}
		}

		public List<RegionRect> RegionRects
		{
			get
			{
				if ( m_RegionRects == null )
					return m_DefaultRectList;

				return m_RegionRects;
			}
		}

		public List<BaseMulti> Multis
		{
			get
			{
				if ( m_Multis == null )
					return m_DefaultMultiList;

				return m_Multis;
			}
		}

		public List<Mobile> Mobiles
		{
			get
			{
				if ( m_Mobiles == null )
					return m_DefaultMobileList;

				return m_Mobiles;
			}
		}

		public List<Item> Items
		{
			get
			{
				if ( m_Items == null )
					return m_DefaultItemList;

				return m_Items;
			}
		}

		public List<NetState> Clients
		{
			get
			{
				if ( m_Clients == null )
					return m_DefaultClientList;

				return m_Clients;
			}
		}

		public List<Mobile> Players
		{
			get
			{
				if ( m_Players == null )
					return m_DefaultMobileList;

				return m_Players;
			}
		}

		public bool Active 
		{ 
			get
			{ 
				return ( m_Active && m_Owner != Map.Internal ); 
			} 
		}

		public Map Owner
		{
			get
			{
				return m_Owner;
			}
		}

		public int X
		{
			get
			{
				return m_X;
			}
		}

		public int Y
		{
			get
			{
				return m_Y;
			}
		}
	}
}