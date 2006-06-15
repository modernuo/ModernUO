/***************************************************************************
 *                              PacketProfile.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: PacketProfile.cs 20 2006-01-15 23:50:35Z asayre $
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

namespace Server.Network
{
	public class PacketProfile
	{
		private int m_Constructed;
		private int m_Count;
		private int m_TotalByteLength;
		private TimeSpan m_TotalProcTime;
		private TimeSpan m_PeakProcTime;
		private bool m_Outgoing;

		[CommandProperty( AccessLevel.Administrator )]
		public bool Outgoing
		{
			get{ return m_Outgoing; }
		}

		[CommandProperty( AccessLevel.Administrator )]
		public int Constructed
		{
			get{ return m_Constructed; }
		}

		[CommandProperty( AccessLevel.Administrator )]
		public int TotalByteLength
		{
			get{ return m_TotalByteLength; }
		}

		[CommandProperty( AccessLevel.Administrator )]
		public TimeSpan TotalProcTime
		{
			get{ return m_TotalProcTime; }
		}

		[CommandProperty( AccessLevel.Administrator )]
		public TimeSpan PeakProcTime
		{
			get{ return m_PeakProcTime; }
		}

		[CommandProperty( AccessLevel.Administrator )]
		public int Count
		{
			get{ return m_Count; }
		}

		[CommandProperty( AccessLevel.Administrator )]
		public double AverageByteLength
		{
			get
			{
				if ( m_Count == 0 )
					return 0;

				return Math.Round( (double) m_TotalByteLength / m_Count, 2 );
			}
		}

		[CommandProperty( AccessLevel.Administrator )]
		public TimeSpan AverageProcTime
		{
			get
			{
				if ( m_Count == 0 )
					return TimeSpan.Zero;

				return TimeSpan.FromTicks( m_TotalProcTime.Ticks / m_Count );
			}
		}

		public void Record( int byteLength, TimeSpan processTime )
		{
			++m_Count;
			m_TotalByteLength += byteLength;
			m_TotalProcTime += processTime;

			if ( processTime > m_PeakProcTime )
				m_PeakProcTime = processTime;
		}

		public void RegConstruct()
		{
			++m_Constructed;
		}

		public PacketProfile( bool outgoing )
		{
			m_Outgoing = outgoing;
		}

		private static PacketProfile[] m_OutgoingProfiles;
		private static PacketProfile[] m_IncomingProfiles;

		public static PacketProfile GetOutgoingProfile( int packetID )
		{
			if ( !Core.Profiling )
				return null;

			PacketProfile prof = m_OutgoingProfiles[packetID];

			if ( prof == null )
				m_OutgoingProfiles[packetID] = prof = new PacketProfile( true );

			return prof;
		}

		public static PacketProfile GetIncomingProfile( int packetID )
		{
			if ( !Core.Profiling )
				return null;

			PacketProfile prof = m_IncomingProfiles[packetID];

			if ( prof == null )
				m_IncomingProfiles[packetID] = prof = new PacketProfile( false );

			return prof;
		}

		public static PacketProfile[] OutgoingProfiles
		{
			get{ return m_OutgoingProfiles; }
		}

		public static PacketProfile[] IncomingProfiles
		{
			get{ return m_IncomingProfiles; }
		}

		static PacketProfile()
		{
			m_OutgoingProfiles = new PacketProfile[0x100];
			m_IncomingProfiles = new PacketProfile[0x100];
		}
	}
}