/***************************************************************************
 *                               QuestArrow.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: QuestArrow.cs 20 2006-01-15 23:50:35Z asayre $
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
		private bool m_Running;

		public Mobile Mobile
		{
			get
			{
				return m_Mobile;
			}
		}

		public bool Running
		{
			get
			{
				return m_Running;
			}
		}

		public void Update( int x, int y )
		{
			if ( m_Running )
				m_Mobile.Send( new SetArrow( x, y ) );
		}

		public void Stop()
		{
			if ( !m_Running )
				return;

			m_Mobile.ClearQuestArrow();

			m_Mobile.Send( new CancelArrow() );
			m_Running = false;

			OnStop();
		}

		public virtual void OnStop()
		{
		}

		public virtual void OnClick( bool rightClick )
		{
		}

		public QuestArrow( Mobile m )
		{
			m_Running = true;
			m_Mobile = m;
		}

		public QuestArrow( Mobile m, int x, int y ) : this( m )
		{
			Update( x, y );
		}
	}
}