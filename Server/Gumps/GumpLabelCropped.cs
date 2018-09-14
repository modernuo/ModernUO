/***************************************************************************
 *                            GumpLabelCropped.cs
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
	public class GumpLabelCropped : GumpEntry
	{
		private int m_X, m_Y;
		private int m_Width, m_Height;
		private int m_Hue;
		private string m_Text;

		public int X
		{
			get => m_X;
			set => Delta( ref m_X, value );
		}

		public int Y
		{
			get => m_Y;
			set => Delta( ref m_Y, value );
		}

		public int Width
		{
			get => m_Width;
			set => Delta( ref m_Width, value );
		}

		public int Height
		{
			get => m_Height;
			set => Delta( ref m_Height, value );
		}

		public int Hue
		{
			get => m_Hue;
			set => Delta( ref m_Hue, value );
		}

		public string Text
		{
			get => m_Text;
			set => Delta( ref m_Text, value );
		}

		public GumpLabelCropped( int x, int y, int width, int height, int hue, string text )
		{
			m_X = x;
			m_Y = y;
			m_Width = width;
			m_Height = height;
			m_Hue = hue;
			m_Text = text;
		}

		public override string Compile( NetState ns )
		{
			return $"{{ croppedtext {m_X} {m_Y} {m_Width} {m_Height} {m_Hue} {Parent.Intern(m_Text)} }}";
		}

		private static byte[] m_LayoutName = Gump.StringToBuffer( "croppedtext" );

		public override void AppendTo( NetState ns, IGumpWriter disp )
		{
			disp.AppendLayout( m_LayoutName );
			disp.AppendLayout( m_X );
			disp.AppendLayout( m_Y );
			disp.AppendLayout( m_Width );
			disp.AppendLayout( m_Height );
			disp.AppendLayout( m_Hue );
			disp.AppendLayout( Parent.Intern( m_Text ) );
		}
	}
}