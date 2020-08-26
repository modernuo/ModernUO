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

namespace Server.Diagnostics
{
    public class TargetProfile : BaseProfile
    {
        private static readonly Dictionary<Type, TargetProfile> _profiles = new Dictionary<Type, TargetProfile>();

        public TargetProfile(Type type)
            : base(type.FullName)
        {
        }

        public static IEnumerable<TargetProfile> Profiles => _profiles.Values;

        public static TargetProfile Acquire(Type type)
        {
            if (!Core.Profiling)
                return null;

            if (!_profiles.TryGetValue(type, out var prof))
                _profiles.Add(type, prof = new TargetProfile(type));

            return prof;
        }
    }
}
