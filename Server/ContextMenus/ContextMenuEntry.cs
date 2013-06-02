/***************************************************************************
 *                            ContextMenuEntry.cs
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

namespace Server.ContextMenus
{
	/// <summary>
	/// Represents a single entry of a <see cref="ContextMenu">context menu</see>.
	/// <seealso cref="ContextMenu" />
	/// </summary>
	public class ContextMenuEntry
	{
		private int m_Number;
		private int m_Color;
		private bool m_Enabled;
		private int m_Range;
		private CMEFlags m_Flags;
		private ContextMenu m_Owner;

		/// <summary>
		/// Gets or sets additional <see cref="CMEFlags">flags</see> used in client communication.
		/// </summary>
		public CMEFlags Flags
		{
			get{ return m_Flags; }
			set{ m_Flags = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="ContextMenu" /> that owns this entry.
		/// </summary>
		public ContextMenu Owner
		{
			get{ return m_Owner; }
			set{ m_Owner = value; }
		}

		/// <summary>
		/// Gets or sets the localization number containing the name of this entry.
		/// </summary>
		public int Number
		{
			get{ return m_Number; }
			set{ m_Number = value; }
		}

		/// <summary>
		/// Gets or sets the maximum range at which this entry may be used, in tiles. A value of -1 signifies no maximum range.
		/// </summary>
		public int Range
		{
			get{ return m_Range; }
			set{ m_Range = value; }
		}

		/// <summary>
		/// Gets or sets the color for this entry. Format is A1-R5-G5-B5.
		/// </summary>
		public int Color
		{
			get{ return m_Color; }
			set{ m_Color = value; }
		}

		/// <summary>
		/// Gets or sets whether this entry is enabled. When false, the entry will appear in a gray hue and <see cref="OnClick" /> will never be invoked.
		/// </summary>
		public bool Enabled
		{
			get{ return m_Enabled; }
			set{ m_Enabled = value; }
		}

		/// <summary>
		/// Gets a value indicating if non local use of this entry is permitted.
		/// </summary>
		public virtual bool NonLocalUse
		{
			get{ return false; }
		}

		/// <summary>
		/// Instantiates a new ContextMenuEntry with a given <see cref="Number">localization number</see> (<paramref name="number" />). No <see cref="Range">maximum range</see> is used.
		/// </summary>
		/// <param name="number">
		/// The localization number containing the name of this entry.
		/// <seealso cref="Number" />
		/// </param>
		public ContextMenuEntry( int number ) : this( number, -1 )
		{
		}

		/// <summary>
		/// Instantiates a new ContextMenuEntry with a given <see cref="Number">localization number</see> (<paramref name="number" />) and <see cref="Range">maximum range</see> (<paramref name="range" />).
		/// </summary>
		/// <param name="number">
		/// The localization number containing the name of this entry.
		/// <seealso cref="Number" />
		/// </param>
		/// <param name="range">
		/// The maximum range at which this entry can be used.
		/// <seealso cref="Range" />
		/// </param>
		public ContextMenuEntry( int number, int range )
		{
			if ( number <= 0x7FFF ) // Legacy code support
				m_Number = 3000000 + number;
			else
				m_Number = number;

			m_Range = range;
			m_Enabled = true;
			m_Color = 0xFFFF;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the entry is clicked.
		/// </summary>
		public virtual void OnClick()
		{
		}
	}
}