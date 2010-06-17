/***************************************************************************
 *                               SecureTrade.cs
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
	public class SecureTrade
	{
		private SecureTradeInfo m_From, m_To;
		private bool m_Valid;

		public SecureTradeInfo From
		{
			get
			{
				return m_From;
			}
		}

		public SecureTradeInfo To
		{
			get
			{
				return m_To;
			}
		}

		public bool Valid
		{
			get
			{
				return m_Valid;
			}
		}

		public void Cancel()
		{
			if ( !m_Valid )
				return;

			List<Item> list = m_From.Container.Items;

			for ( int i = list.Count - 1; i >= 0; --i )
			{
				if ( i < list.Count )
				{
					Item item = list[i];

					item.OnSecureTrade( m_From.Mobile, m_To.Mobile, m_From.Mobile, false );

					if ( !item.Deleted )
						m_From.Mobile.AddToBackpack( item );
				}
			}

			list = m_To.Container.Items;

			for ( int i = list.Count - 1; i >= 0; --i )
			{
				if ( i < list.Count )
				{
					Item item = list[i];

					item.OnSecureTrade( m_To.Mobile, m_From.Mobile, m_To.Mobile, false );

					if ( !item.Deleted )
						m_To.Mobile.AddToBackpack( item );
				}
			}

			Close();
		}

		public void Close()
		{
			if ( !m_Valid )
				return;

			m_From.Mobile.Send( new CloseSecureTrade( m_From.Container ) );
			m_To.Mobile.Send( new CloseSecureTrade( m_To.Container ) );

			m_Valid = false;

			NetState ns = m_From.Mobile.NetState;

			if ( ns != null )
				ns.RemoveTrade( this );

			ns = m_To.Mobile.NetState;

			if ( ns != null )
				ns.RemoveTrade( this );

			Timer.DelayCall( TimeSpan.Zero, delegate{ m_From.Container.Delete(); } );
			Timer.DelayCall( TimeSpan.Zero, delegate{ m_To.Container.Delete(); } );
		}

		public void Update()
		{
			if ( !m_Valid )
				return;

			if ( m_From.Accepted && m_To.Accepted )
			{
				List<Item> list = m_From.Container.Items;

				bool allowed = true;

				for ( int i = list.Count - 1; allowed && i >= 0; --i )
				{
					if ( i < list.Count )
					{
						Item item = list[i];

						if ( !item.AllowSecureTrade( m_From.Mobile, m_To.Mobile, m_To.Mobile, true ) )
							allowed = false;
					}
				}

				list = m_To.Container.Items;

				for ( int i = list.Count - 1; allowed && i >= 0; --i )
				{
					if ( i < list.Count )
					{
						Item item = list[i];

						if ( !item.AllowSecureTrade( m_To.Mobile, m_From.Mobile, m_From.Mobile, true ) )
							allowed = false;
					}
				}

				if ( !allowed )
				{
					m_From.Accepted = false;
					m_To.Accepted = false;

					m_From.Mobile.Send( new UpdateSecureTrade( m_From.Container, m_From.Accepted, m_To.Accepted ) );
					m_To.Mobile.Send( new UpdateSecureTrade( m_To.Container, m_To.Accepted, m_From.Accepted ) );

					return;
				}

				list = m_From.Container.Items;

				for ( int i = list.Count - 1; i >= 0; --i )
				{
					if ( i < list.Count )
					{
						Item item = list[i];

						item.OnSecureTrade( m_From.Mobile, m_To.Mobile, m_To.Mobile, true );

						if ( !item.Deleted )
							m_To.Mobile.AddToBackpack( item );
					}
				}

				list = m_To.Container.Items;

				for ( int i = list.Count - 1; i >= 0; --i )
				{
					if ( i < list.Count )
					{
						Item item = list[i];

						item.OnSecureTrade( m_To.Mobile, m_From.Mobile, m_From.Mobile, true );

						if ( !item.Deleted )
							m_From.Mobile.AddToBackpack( item );
					}
				}

				Close();
			}
			else
			{
				m_From.Mobile.Send( new UpdateSecureTrade( m_From.Container, m_From.Accepted, m_To.Accepted ) );
				m_To.Mobile.Send( new UpdateSecureTrade( m_To.Container, m_To.Accepted, m_From.Accepted ) );
			}
		}

		public SecureTrade( Mobile from, Mobile to )
		{
			m_Valid = true;

			m_From = new SecureTradeInfo( this, from, new SecureTradeContainer( this ) );
			m_To = new SecureTradeInfo( this, to, new SecureTradeContainer( this ) );

			bool from6017 = ( from.NetState == null ? false : from.NetState.ContainerGridLines );
			bool to6017   = ( to.NetState == null ? false : to.NetState.ContainerGridLines );

			from.Send( new MobileStatus( from, to ) );
			from.Send( new UpdateSecureTrade( m_From.Container, false, false ) );
			if ( from6017 )
				from.Send( new SecureTradeEquip6017( m_To.Container, to ) );
			else
				from.Send( new SecureTradeEquip( m_To.Container, to ) );
			from.Send( new UpdateSecureTrade( m_From.Container, false, false ) );
			if ( from6017 )
				from.Send( new SecureTradeEquip6017( m_From.Container, from ) );
			else
				from.Send( new SecureTradeEquip( m_From.Container, from ) );
			from.Send( new DisplaySecureTrade( to, m_From.Container, m_To.Container, to.Name ) );
			from.Send( new UpdateSecureTrade( m_From.Container, false, false ) );

			to.Send( new MobileStatus( to, from ) );
			to.Send( new UpdateSecureTrade( m_To.Container, false, false ) );
			if ( to6017 )
				to.Send( new SecureTradeEquip6017( m_From.Container, from ) );
			else
				to.Send( new SecureTradeEquip( m_From.Container, from ) );
			to.Send( new UpdateSecureTrade( m_To.Container, false, false ) );
			if ( to6017 )
				to.Send( new SecureTradeEquip6017( m_To.Container, to ) );
			else
				to.Send( new SecureTradeEquip( m_To.Container, to ) );
			to.Send( new DisplaySecureTrade( from, m_To.Container, m_From.Container, from.Name ) );
			to.Send( new UpdateSecureTrade( m_To.Container, false, false ) );
		}
	}

	public class SecureTradeInfo
	{
		private SecureTrade m_Owner;
		private Mobile m_Mobile;
		private SecureTradeContainer m_Container;
		private bool m_Accepted;

		public SecureTradeInfo( SecureTrade owner, Mobile m, SecureTradeContainer c )
		{
			m_Owner = owner;
			m_Mobile = m;
			m_Container = c;

			m_Mobile.AddItem( m_Container );
		}

		public SecureTrade Owner
		{
			get
			{
				return m_Owner;
			}
		}

		public Mobile Mobile
		{
			get
			{
				return m_Mobile;
			}
		}

		public SecureTradeContainer Container
		{
			get
			{
				return m_Container;
			}
		}

		public bool Accepted
		{
			get
			{
				return m_Accepted;
			}
			set
			{
				m_Accepted = value;
			}
		}
	}
}