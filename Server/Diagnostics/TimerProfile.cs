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
	public class TimerProfile : BaseProfile {
		private static Dictionary<string, TimerProfile> _profiles = new Dictionary<string, TimerProfile>();

		public static IEnumerable<TimerProfile> Profiles {
			get {
				return _profiles.Values;
			}
		}

		public static TimerProfile Acquire( string name ) {
			if ( !Core.Profiling ) {
				return null;
			}

			TimerProfile prof;

			if ( !_profiles.TryGetValue( name, out prof ) ) {
				_profiles.Add( name, prof = new TimerProfile( name ) );
			}

			return prof;
		}

		private long _created, _started, _stopped;

		public long Created {
			get {
				return _created;
			}
			set {
				_created = value;
			}
		}

		public long Started {
			get {
				return _started;
			}
			set {
				_started = value;
			}
		}

		public long Stopped {
			get {
				return _stopped;
			}
			set {
				_stopped = value;
			}
		}

		public TimerProfile( string name )
			: base( name ) {
		}

		public override void WriteTo( TextWriter op ) {
			base.WriteTo( op );

			op.Write( "\t{0,12:N0} {1,12:N0} {2,-12:N0}", _created, _started, _stopped );
		}
	}
}