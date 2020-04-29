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
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Server.Diagnostics
{
  public abstract class BasePacketProfile : BaseProfile
  {
    protected BasePacketProfile(string name) : base(name)
    {
    }

    public long TotalLength { get; private set; }

    public double AverageLength => (double)TotalLength / Math.Max(1, Count);

    public void Finish(long length)
    {
      Finish();

      TotalLength += length;
    }

    public override void WriteTo(TextWriter op)
    {
      base.WriteTo(op);

      op.Write("\t{0,12:F2} {1,-12:N0}", AverageLength, TotalLength);
    }
  }

  public class PacketSendProfile : BasePacketProfile
  {
    private static readonly Dictionary<Type, PacketSendProfile> m_Profiles = new Dictionary<Type, PacketSendProfile>();

    private long m_Created;

    public PacketSendProfile(Type type) : base(type.FullName)
    {
    }

    public static IEnumerable<PacketSendProfile> Profiles => m_Profiles.Values;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static PacketSendProfile Acquire(Type type)
    {
      if (!m_Profiles.TryGetValue(type, out var prof))
        m_Profiles.Add(type, prof = new PacketSendProfile(type));

      return prof;
    }

    public void Increment()
    {
      Interlocked.Increment(ref m_Created);
    }

    public override void WriteTo(TextWriter op)
    {
      base.WriteTo(op);

      op.Write("\t{0,12:N0}", m_Created);
    }
  }

  public class PacketReceiveProfile : BasePacketProfile
  {
    private static readonly Dictionary<int, PacketReceiveProfile> m_Profiles = new Dictionary<int, PacketReceiveProfile>();

    public PacketReceiveProfile(int packetId)
      : base($"0x{packetId:X2}")
    {
    }

    public static IEnumerable<PacketReceiveProfile> Profiles => m_Profiles.Values;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static PacketReceiveProfile Acquire(int packetId)
    {
      if (!m_Profiles.TryGetValue(packetId, out var prof))
        m_Profiles.Add(packetId, prof = new PacketReceiveProfile(packetId));

      return prof;
    }
  }
}
