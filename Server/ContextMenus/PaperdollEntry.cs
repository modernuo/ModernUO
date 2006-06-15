/***************************************************************************
 *                             PaperdollEntry.cs
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

namespace Server.ContextMenus
{
	public class PaperdollEntry : ContextMenuEntry
	{
		private Mobile m_Mobile;

		public PaperdollEntry( Mobile m ) : base( 6123, 18 )
		{
			m_Mobile = m;
		}

		public override void OnClick()
		{
			if ( m_Mobile.CanPaperdollBeOpenedBy( Owner.From ) )
				m_Mobile.DisplayPaperdollTo( Owner.From );
		}
	}
}