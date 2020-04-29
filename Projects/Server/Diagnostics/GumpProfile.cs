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
  public class GumpProfile : BaseProfile
  {
    private static readonly Dictionary<Type, GumpProfile> m_Profiles = new Dictionary<Type, GumpProfile>();

    public GumpProfile(Type type) : base(type.FullName)
    {
    }

    public static IEnumerable<GumpProfile> Profiles => m_Profiles.Values;

    public static GumpProfile Acquire(Type type)
    {
      if (!Core.Profiling)
        return null;

      if (!m_Profiles.TryGetValue(type, out var prof))
        m_Profiles.Add(type, prof = new GumpProfile(type));

      return prof;
    }
  }
}
