/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Timer.TimerWheel.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server
{
    public partial class Timer
    {
        private const int _ringSizePowerOf2 = 12;
        private const int _ringSize = 1 << _ringSizePowerOf2; // 4096
        private const int _ringLayers = 3;
        private const int _tickRatePowerOf2 = 3;
        private const int _tickRate = 1 << _tickRatePowerOf2; // 8ms

        private static long _lastTickTurned = -1;

        private static readonly Timer[][] _rings = new Timer[_ringLayers][];
        private static readonly int[] _ringIndexes = new int[_ringLayers];

        public static void Initialize(long tickCount)
        {
            _lastTickTurned = tickCount;

            for (int i = 0; i < _rings.Length; i++)
            {
                _rings[i] = new Timer[_ringSize];
                _ringIndexes[i] = 0;
            }
        }

        public static int Slice(long tickCount)
        {
            var deltaSinceTurn = tickCount - _lastTickTurned;
            var events = 0;
            while (deltaSinceTurn >= _tickRate)
            {
                deltaSinceTurn -= _tickRate;
                _lastTickTurned += _tickRate;
                events += Turn() ? 1 : 0;
            }

            return events;
        }

        private static bool Turn()
        {
            bool events = false;

            for (var i = 0; i < _ringLayers; i++)
            {
                // Increment the ring index, then get the timer.
                var ringIndex = ++_ringIndexes[i];
                bool turnNextWheel = ringIndex >= _ringSize;

                if (turnNextWheel)
                {
                    ringIndex = _ringIndexes[i] = 0;
                }

                var timer = _rings[i][ringIndex];

                if (timer != null)
                {
                    events = true;

                    if (i > 0 && timer._remaining > 0)
                    {
                        Promote(timer);
                    }
                    else
                    {
                        Execute(timer);
                    }

                    // Clear the slot
                    _rings[i][ringIndex] = null;
                }

                if (!turnNextWheel)
                {
                    break;
                }
            }

            return events;
        }

        private static void Execute(Timer timer)
        {
            do
            {
                var next = timer._nextTimer;
                var prof = timer.GetProfile();

                prof?.Start();
                timer.OnTick();
                prof?.Finish();

                if (timer.Running)
                {
                    RemoveTimer(timer);

                    if (timer.Count == 0 || ++timer.Index < timer.Count)
                    {
                        timer.Delay = timer.Interval;
                        timer.Next = Core.Now + timer.Interval;
                        AddTimer(timer, (long)timer.Delay.TotalMilliseconds);
                    }
                }

                timer = next;
            } while (timer != null);
        }

        private static void Promote(Timer timer)
        {
            do
            {
                var next = timer._nextTimer;
                RemoveTimer(timer);
                AddTimer(timer, timer._remaining);
                timer = next;
            } while (timer != null);
        }

        private static void AddTimer(Timer timer, long delay)
        {
            delay = Math.Max(0, delay);

            var resolutionPowerOf2 = _tickRatePowerOf2;
            for (var i = 0; i < _ringLayers; i++)
            {
                var resolution = 1 << resolutionPowerOf2;
                var nextResolutionPowerOf2 = resolutionPowerOf2 + _ringSizePowerOf2;
                long max = 1 << nextResolutionPowerOf2;
                if (delay < max)
                {
                    var remaining = delay & (resolution - 1);
                    var slot = (delay >> resolutionPowerOf2) + _ringIndexes[i] + (remaining > 0 ? 1 : 0);

                    // Round up if we have a delay of 0
                    if (delay == 0)
                    {
                        slot++;
                        remaining = 0;
                    }

                    if (slot >= _ringSize)
                    {
                        slot -= _ringSize;
                    }

                    timer._nextTimer = _rings[i][slot];
                    if (timer._nextTimer != null)
                    {
                        timer._nextTimer._prevTimer = timer;
                    }

                    timer._remaining = remaining;
                    timer._ring = i;
                    timer._slot = (int)slot;

                    _rings[i][slot] = timer;
                    break;
                }

                // The remaining amount until we turn this ring
                delay -= resolution * (_ringSize - _ringIndexes[i]);
                resolutionPowerOf2 = nextResolutionPowerOf2;
            }
        }

        private static void RemoveTimer(Timer timer)
        {
            if (timer._prevTimer != null)
            {
                timer._prevTimer._nextTimer = timer._nextTimer;
            }
            else
            {
                _rings[timer._ring][timer._slot] = timer._nextTimer;
            }

            if (timer._nextTimer != null)
            {
                timer._nextTimer._prevTimer = timer._prevTimer;
            }

            timer._nextTimer = null;
            timer._prevTimer = null;
        }

        public static void DumpInfo(TextWriter tw)
        {
            var now = DateTime.UtcNow;

            tw.WriteLine("Date: {0}", now);
            tw.WriteLine();

            var total = 0.0;
            var hash = new Dictionary<string, int>();

            for (var i = 0; i < _ringLayers; i++)
            {
                for (var j = 0; j < _ringSize; j++)
                {
                    var t = _rings[i][j];

                    var name = t.ToString();

                    hash.TryGetValue(name, out var count);
                    hash[name] = count + 1;

                    total++;
                }
            }

            foreach (var (name, count) in hash.OrderByDescending(o => o.Value))
            {
                tw.WriteLine($"Type: {name}; Count: {count}; Percent: {count / total}%");
            }

            tw.WriteLine();
            tw.WriteLine();
        }

        public static void ClearAllTimers(long tickCount)
        {
            _lastTickTurned = tickCount;

            foreach (var t in _rings)
            {
                for (var i = 0; i < _ringSize; i++)
                {
                    var node = t[i];
                    Timer next;

                    do
                    {
                        next = node?._nextTimer;
                        node?.Stop();
                    } while (next != null);
                }
            }
        }
    }
}
