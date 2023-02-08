/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

using System.Runtime.CompilerServices;
using CalcMoves = Server.Movement.Movement;

namespace Server.Network;

public partial class NetState
{
    // The next step
    private int _stepIndex;
    // The last index to expire
    private int _expiredIndex;

    private long[] _steps;

    public bool AddStep(Direction d)
    {
        if (Mobile == null)
        {
            return false;
        }

        _steps ??= new long[CalcMoves.MaxSteps + 1]; // Extra index as a sentinel
        var stepsLength = _steps.Length;

        var now = Core.TickCount;

        var lastIndex = -1;

        // Expire old steps
        while (_expiredIndex != _stepIndex)
        {
            var step = _steps[_expiredIndex];

            // Is the step ahead of us, or the next step rolled over and we didn't yet
            if (step > now || lastIndex > -1 && _steps[lastIndex] > step)
            {
                break;
            }

            lastIndex = _expiredIndex++;

            if (_expiredIndex == stepsLength)
            {
                _expiredIndex -= stepsLength;
            }
        }

        var stepsTaken = (_stepIndex < _expiredIndex ? _stepIndex + stepsLength : _stepIndex) - _expiredIndex;
        var maxSteps = _steps.Length - 1;

        // Can we take a step?
        if (stepsTaken >= maxSteps)
        {
            return false;
        }

        var delay = Mobile.ComputeMovementSpeed(d);

        var prev = _stepIndex - 1;
        if (prev < 0)
        {
            prev += stepsLength;
        }

        // Give a 5% buffer on the first step
        _steps[_stepIndex++] = stepsTaken > 0 ? _steps[prev] + delay : now + delay * 950 / 1000;

        if (_stepIndex == stepsLength)
        {
            _stepIndex -= stepsLength;
        }

        // If CalcMoves.MaxSteps is modified, we need to adjust accordingly
        AdjustSteps(CalcMoves.MaxSteps);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AdjustSteps(int maxSteps)
    {
        var stepsLength = maxSteps + 1;

        if (_steps.Length == stepsLength)
        {
            return;
        }

        var oldSteps = _steps;
        _steps = new long[stepsLength];

        var expiredIndex = _expiredIndex;
        var newStepIndex = 0;
        while (newStepIndex < maxSteps && expiredIndex != _stepIndex)
        {
            _steps[newStepIndex++] = oldSteps[expiredIndex++];
            if (expiredIndex >= oldSteps.Length)
            {
                expiredIndex -= oldSteps.Length;
            }
        }

        _expiredIndex = 0;
        _stepIndex = newStepIndex;
    }
}
