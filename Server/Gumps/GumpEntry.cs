/***************************************************************************
 *                                GumpEntry.cs
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

namespace Server.Gumps
{
	public abstract class GumpEntry
	{
		private Gump m_Parent;

		protected GumpEntry()
		{
		}

		protected void Delta( ref int var, int val )
		{
			if ( var != val )
			{
				var = val;

				if ( m_Parent != null )
				{
					m_Parent.Invalidate();
				}
			}
		}

		protected void Delta( ref bool var, bool val )
		{
			if ( var != val )
			{
				var = val;

				if ( m_Parent != null )
				{
					m_Parent.Invalidate();
				}
			}
		}

		protected void Delta( ref string var, string val )
		{
			if ( var != val )
			{
				var = val;

				if ( m_Parent != null )
				{
					m_Parent.Invalidate();
				}
			}
		}

		public Gump Parent {
			get {
				return m_Parent;
			}
			set {
				if (m_Parent != value) {
					if (m_Parent != null)
						m_Parent.Remove(this);

					m_Parent = value;

					if (m_Parent != null)
						m_Parent.Add(this);
				}
			}
		}

		public abstract string Compile();
		public abstract void AppendTo( IGumpWriter disp );
	}
}