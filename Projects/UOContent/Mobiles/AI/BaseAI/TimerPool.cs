/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TimerPool.cs                                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using System;
using System.Collections.Generic;
using Server.Mobiles;

public class TimerPool
{
     private readonly SortedSet<ScheduledAI> _queue = new SortedSet<ScheduledAI>();

     public void Add(BaseAI ai, long nextTick)
     {
          _queue.Add(new ScheduledAI(ai, nextTick));
     }

     public void Remove(BaseAI ai)
     {
          _queue.RemoveWhere(s => s.AI == ai);
     }

     public void Tick(long now)
     {
          int maxPerTick = 10;
          int processed = 0;

          while (_queue.Count > 0 && _queue.Min.NextTick <= now && processed < maxPerTick)
          {
               var scheduled = _queue.Min;
               _queue.Remove(scheduled);

               var nextInterval = scheduled.AI.OnPoolTick();
               var nextTick = now + nextInterval;

               _queue.Add(new ScheduledAI(scheduled.AI, nextTick));
               
               processed++;
          }
     }

     private record ScheduledAI(BaseAI AI, long NextTick) : IComparable<ScheduledAI>
     {
          public int CompareTo(ScheduledAI other)
          {
               if (NextTick != other.NextTick)
               {
                    return NextTick.CompareTo(other.NextTick);
               }
               else
               {
                    return AI.GetHashCode().CompareTo(other.AI.GetHashCode());
               }
          }
     }
}