/***************************************************************************
 *                                 IMenu.cs
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
using Server.Network;

namespace Server.Menus
{
	public interface IMenu
	{
		int Serial{ get; }
		int EntryLength{ get; }
		void SendTo( NetState state );
		void OnCancel( NetState state );
		void OnResponse( NetState state, int index );
	}
}