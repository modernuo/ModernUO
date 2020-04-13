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
using System.Diagnostics;
using System.IO;

namespace Server.Diagnostics
{
  public abstract class BaseProfile
  {
    private readonly Stopwatch _stopwatch;

    protected BaseProfile(string name)
    {
      Name = name;

      _stopwatch = new Stopwatch();
    }

    public string Name{ get; }

    public long Count{ get; private set; }

    public TimeSpan AverageTime => TimeSpan.FromTicks(TotalTime.Ticks / Math.Max(1, Count));

    public TimeSpan PeakTime{ get; private set; }

    public TimeSpan TotalTime{ get; private set; }

    public static void WriteAll<T>(TextWriter op, IEnumerable<T> profiles) where T : BaseProfile
    {
      List<T> list = new List<T>(profiles);

      list.Sort(delegate(T a, T b) { return -a.TotalTime.CompareTo(b.TotalTime); });

      foreach (T prof in list)
      {
        prof.WriteTo(op);
        op.WriteLine();
      }
    }

    public virtual void Start()
    {
      if (_stopwatch.IsRunning) _stopwatch.Reset();

      _stopwatch.Start();
    }

    public virtual void Finish()
    {
      TimeSpan elapsed = _stopwatch.Elapsed;

      TotalTime += elapsed;

      if (elapsed > PeakTime) PeakTime = elapsed;

      Count++;

      _stopwatch.Reset();
    }

    public virtual void WriteTo(TextWriter op)
    {
      op.Write("{0,-100} {1,12:N0} {2,12:F5} {3,-12:F5} {4,12:F5}", Name, Count, AverageTime.TotalSeconds,
        PeakTime.TotalSeconds, TotalTime.TotalSeconds);
    }
  }
}