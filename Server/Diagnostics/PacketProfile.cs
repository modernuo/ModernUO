/***************************************************************************
 *                              PacketProfile.cs
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
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Server.Diagnostics {
	public abstract class BasePacketProfile : BaseProfile {
		private long _totalLength;

		public long TotalLength {
			get {
				return _totalLength;
			}
		}

		public double AverageLength {
			get {
				return ( double ) _totalLength / Math.Max( 1, this.Count );
			}
		}

		protected BasePacketProfile(string name)
			: base( name ) {
		}

		public void Finish( int length ) {
			Finish();

			_totalLength += length;
		}

		public override void WriteTo( TextWriter op ) {
			base.WriteTo( op );

			op.Write( "\t{0,12:F2} {1,-12:N0}", AverageLength, TotalLength );
		}
	}

	public class PacketSendProfile : BasePacketProfile {
		private static Dictionary<Type, PacketSendProfile> _profiles = new Dictionary<Type, PacketSendProfile>();

		public static IEnumerable<PacketSendProfile> Profiles {
			get {
				return _profiles.Values;
			}
		}

		public static PacketSendProfile Acquire( Type type ) {
			if ( !Core.Profiling ) {
				return null;
			}

			PacketSendProfile prof;

			if ( !_profiles.TryGetValue( type, out prof ) ) {
				_profiles.Add( type, prof = new PacketSendProfile( type ) );
			}

			return prof;
		}

		private long _created;

		public long Created {
			get {
				return _created;
			}
			set {
				_created = value;
			}
		}

		public PacketSendProfile( Type type )
			: base( type.FullName ) {
		}

		public override void WriteTo( TextWriter op ) {
			base.WriteTo( op );

			op.Write( "\t{0,12:N0}", Created );
		}
	}

	public class PacketReceiveProfile : BasePacketProfile {
		private static Dictionary<int, PacketReceiveProfile> _profiles = new Dictionary<int, PacketReceiveProfile>();

		public static IEnumerable<PacketReceiveProfile> Profiles {
			get {
				return _profiles.Values;
			}
		}

		public static PacketReceiveProfile Acquire( int packetId ) {
			if ( !Core.Profiling ) {
				return null;
			}

			PacketReceiveProfile prof;

			if ( !_profiles.TryGetValue( packetId, out prof ) ) {
				_profiles.Add( packetId, prof = new PacketReceiveProfile( packetId ) );
			}

			return prof;
		}

		public PacketReceiveProfile( int packetId )
			: base( String.Format( "0x{0:X2}", packetId ) ) {
		}
	}
}