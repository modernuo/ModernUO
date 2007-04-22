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

namespace Server.Diagnostics {
	public class GumpProfile : BaseProfile {
		private static Dictionary<Type, GumpProfile> _profiles = new Dictionary<Type, GumpProfile>();

		public static IEnumerable<GumpProfile> Profiles {
			get {
				return _profiles.Values;
			}
		}

		public static GumpProfile Acquire( Type type ) {
			if ( !Core.Profiling ) {
				return null;
			}

			GumpProfile prof;

			if ( !_profiles.TryGetValue( type, out prof ) ) {
				_profiles.Add( type, prof = new GumpProfile( type ) );
			}

			return prof;
		}

		public GumpProfile( Type type )
			: base( type.FullName ) {
		}
	}
}