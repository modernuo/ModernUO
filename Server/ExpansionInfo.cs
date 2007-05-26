/***************************************************************************
 *                          ExpansionInfo.cs
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
	public enum Expansion
	{
		None,
		/*
		T2A,
		UOR,
		LBR,
		UOTD,
		*/
		AOS,
		SE,
		ML
	}
	public class ExpansionInfo
	{
		private string m_Name;
		private int m_ID, m_NetStateFlag, m_SupportedFeatures, m_CharListFlags, m_CustomHousingFlag;

		private ClientVersion m_RequiredClient;	//Used as an alternative to the flags

		public string Name{ get{ return m_Name; } }
		public int ID{ get{ return m_ID; } }
		public int NetStateFlag{ get{ return m_NetStateFlag; } }
		public int SupportedFeatures{ get{ return m_SupportedFeatures; } }
		public int CharacterListFlags { get { return m_CharListFlags; } }
		public int CustomHousingFlag { get{ return m_CustomHousingFlag; } }
		public ClientVersion RequiredClient { get { return m_RequiredClient; } }

		public ExpansionInfo( int id, string name, int netStateFlag, int supportedFeatures, int charListFlags, int customHousingFlag )
		{
			m_Name = name;
			m_ID = id;
			m_NetStateFlag = netStateFlag;
			m_SupportedFeatures = supportedFeatures;
			m_CharListFlags = charListFlags;
			m_CustomHousingFlag = customHousingFlag;
		}

		public ExpansionInfo( int id, string name, ClientVersion requiredClient, int supportedFeatures, int charListFlags, int customHousingFlag )
		{
			m_Name = name;
			m_ID = id;
			m_SupportedFeatures = supportedFeatures;
			m_CharListFlags = charListFlags;
			m_CustomHousingFlag = customHousingFlag;
			m_RequiredClient = requiredClient;
		}

		public static ExpansionInfo[] Table { get { return m_Table; } }
		private static ExpansionInfo[] m_Table = new ExpansionInfo[]
			{
				new ExpansionInfo( 0, "None"			, 0x00,								0x0003, 0x008, 0x00 ),
				new ExpansionInfo( 1, "Age of Shadows"	, 0x08,								0x801F, 0x028, 0x20 ),
				new ExpansionInfo( 2, "Samurai Empire"	, 0x10,								0x805F, 0x0A8, 0x60 ),	//0x40 | 0x20 = 0x60
				new ExpansionInfo( 3, "Mondain's Legacy", new ClientVersion( "5.0.0a" ),	0x82DF, 0x1A8, 0x2E0 )	//0x280 | 0x60 = 0x2E0

				//0x200 + 0x400 for KR?
			};

		public static ExpansionInfo GetInfo( Expansion ex )
		{
			return GetInfo( (int)ex );
		}

		public static ExpansionInfo GetInfo( int ex )
		{
			int v = (int)ex;

			if( v < 0 || v >= m_Table.Length )
				v = 0;

			return m_Table[v];
		}

		public static ExpansionInfo CurrentExpansion { get { return GetInfo( Core.Expansion ); } }

		public override string ToString()
		{
			return m_Name;
		}
	}
}
