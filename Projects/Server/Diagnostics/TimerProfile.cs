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

using System.Collections.Generic;
using System.IO;

namespace Server.Diagnostics
{
    public class TimerProfile : BaseProfile
    {
        private static readonly Dictionary<string, TimerProfile> _profiles = new Dictionary<string, TimerProfile>();

        public TimerProfile(string name)
            : base(name)
        {
        }

        public static IEnumerable<TimerProfile> Profiles => _profiles.Values;

        public long Created { get; set; }

        public long Started { get; set; }

        public long Stopped { get; set; }

        public static TimerProfile Acquire(string name)
        {
            if (!Core.Profiling)
                return null;

            if (!_profiles.TryGetValue(name, out var prof))
                _profiles.Add(name, prof = new TimerProfile(name));

            return prof;
        }

        public override void WriteTo(TextWriter op)
        {
            base.WriteTo(op);

            op.Write("\t{0,12:N0} {1,12:N0} {2,-12:N0}", Created, Started, Stopped);
        }
    }
}
