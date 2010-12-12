/***************************************************************************
 *                               QuestArrow.cs
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
using Server;
using Server.Network;

namespace Server
{
	public class QuestArrow
	{
		private Mobile m_Mobile;
		private Mobile m_Target;
		private bool m_Running;

		public Mobile Mobile
		{
			get
			{
				return m_Mobile;
			}
		}

		public Mobile Target
		{
			get
			{
				return m_Target;
			}
		}

		public bool Running
		{
			get
			{
				return m_Running;
			}
		}

		public void Update()
		{
			Update( m_Target.X, m_Target.Y );
		}

		public void Update( int x, int y )
		{
			if ( !m_Running )
				return;

			NetState ns = m_Mobile.NetState;

			if ( ns == null )
				return;

			if ( ns.HighSeas )
				ns.Send( new SetArrowHS( x, y, m_Target.Serial ) );
			else
				ns.Send( new SetArrow( x, y ) );
		}

		public void Stop()
		{
			Stop( m_Target.X, m_Target.Y );
		}

		public void Stop( int x, int y )
		{
			if ( !m_Running )
				return;

			m_Mobile.ClearQuestArrow();

			NetState ns = m_Mobile.NetState;

			if ( ns != null ) {
				if ( ns.HighSeas )
					ns.Send( new CancelArrowHS( x, y, m_Target.Serial ) );
				else
					ns.Send( new CancelArrow() );
			}

			m_Running = false;
			OnStop();
		}

		public virtual void OnStop()
		{
		}

		public virtual void OnClick( bool rightClick )
		{
		}

		public QuestArrow( Mobile m, Mobile t )
		{
			m_Running = true;
			m_Mobile = m;
			m_Target = t;
		}

		public QuestArrow( Mobile m, Mobile t, int x, int y ) : this( m, t )
		{
			Update( x, y );
		}
	}
}