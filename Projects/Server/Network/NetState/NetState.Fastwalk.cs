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
                index += maxSteps;
            }

            var now = Core.TickCount;
            var lastMove = Mobile.LastMoveTime;

            while (index < _stepIndex)
            {
                var step = _stepDelays[index++];
                if (lastMove + step < now)
                {
                    _stepCount--;
                }

                if (index > length)
                {
                    index = 0;
                }
            }

            if (_stepCount >= maxSteps)
            {
                return false;
            }

            var delay = Mobile.ComputeMovementSpeed(d);
            index = _stepIndex++;
            _stepDelays[index > length ? 0 : index] = delay;
            _stepCount++;

            return true;
        }
    }
}
