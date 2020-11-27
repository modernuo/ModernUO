/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NetState.Fastwalk.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using CalcMoves = Server.Movement.Movement;

namespace Server.Network
{
    public partial class NetState
    {
        private int _stepIndex;
        private int _stepCount;
        private long _startDelay;
        private long[] _stepDelays;

        public bool AddStep(Direction d)
        {
            if (Mobile == null)
            {
                return false;
            }

            var maxSteps = CalcMoves.MaxSteps;

            _stepDelays ??= new long[maxSteps];
            var length = _stepDelays.Length;

            var index = _stepIndex - _stepCount;
            if (index < 0)
            {
                index += length;
            }

            var now = Core.TickCount;
            var last = _startDelay;

            // Discard old steps by decrementing the step counter
            while (index != _stepIndex || _stepCount >= maxSteps)
            {
                var step = _stepDelays[index++];
                if (now - last < step)
                {
                    break;
                }

                last += step;
                _stepCount--;
                if (index >= length)
                {
                    index = 0;
                }
            }

            _startDelay = last;

            // If we are out of steps, fail
            if (_stepCount >= maxSteps)
            {
                return false;
            }

            var delay = Mobile.ComputeMovementSpeed(d);

            // Add the delay
            _stepDelays[_stepIndex++] = delay;

            if (_stepIndex >= length)
            {
                _stepIndex = 0;
            }

            if (_stepCount == 0)
            {
                _startDelay = now;
            }
            _stepCount++;

            return true;
        }
    }
}
