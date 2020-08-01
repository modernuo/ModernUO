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

    public double AverageLength => (double)TotalLength / Math.Max(Count, 1);

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
    private static readonly Dictionary<Type, PacketSendProfile> _profiles = new Dictionary<Type, PacketSendProfile>();

    private long _created;

    public PacketSendProfile(Type type) : base(type.FullName)
    {
    }

    public static IEnumerable<PacketSendProfile> Profiles => _profiles.Values;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static PacketSendProfile Acquire(Type type)
    {
      if (!_profiles.TryGetValue(type, out var prof))
        _profiles.Add(type, prof = new PacketSendProfile(type));

      return prof;
    }

    public void Increment()
    {
      Interlocked.Increment(ref _created);
    }

    public override void WriteTo(TextWriter op)
    {
      base.WriteTo(op);

      op.Write("\t{0,12:N0}", _created);
    }
  }

  public class PacketReceiveProfile : BasePacketProfile
  {
    private static readonly Dictionary<int, PacketReceiveProfile> _profiles = new Dictionary<int, PacketReceiveProfile>();

    public PacketReceiveProfile(int packetId)
      : base($"0x{packetId:X2}")
    {
    }

    public static IEnumerable<PacketReceiveProfile> Profiles => _profiles.Values;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static PacketReceiveProfile Acquire(int packetId)
    {
      if (!_profiles.TryGetValue(packetId, out var prof))
        _profiles.Add(packetId, prof = new PacketReceiveProfile(packetId));

      return prof;
    }
  }
}
