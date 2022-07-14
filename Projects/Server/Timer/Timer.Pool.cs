/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Timer.Pool.cs                                                   *
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
using System.Threading;
using System.Threading.Tasks;

namespace Server;

public partial class Timer
{
    private const int _timerPoolDepletionThreshold = 128; // Maximum timers allocated in a single tick before we force adjust
    private static int _timerPoolDepletionAmount;         // Amount the pool has been depleted by
    private static int _maxPoolCapacity;
    private static int _poolCapacity;
    private static int _poolCount;
    private static DelayCallTimer _poolHead;
    private static int _isRefilling;

    public static void CheckTimerPool()
    {
        // Anything less than this threshold and we are ok with the number of allocations.
        if (_timerPoolDepletionAmount < _timerPoolDepletionThreshold)
        {
            _timerPoolDepletionAmount = 0;
            return;
        }

        var growthFactor = Math.DivRem(_timerPoolDepletionAmount, _poolCapacity, out var rem);
        var amountToGrow = _poolCapacity * (growthFactor + (rem > 0 ? 1 : 0));
        var amountToRefill = Math.Min(_maxPoolCapacity, amountToGrow);

        var maximumHit = amountToGrow > amountToRefill ? " Maximum pool size has been reached." : "";
        var warningMessage = $"Timer pool depleted by {{Amount}}. Refilling with {{AmountRefill}}.{maximumHit}";

        logger.Warning(warningMessage, _timerPoolDepletionAmount, amountToRefill);
        RefillPoolAsync(amountToRefill);
        _timerPoolDepletionAmount = 0;
    }

    public static void ConfigureTimerPool()
    {
        _poolCapacity = ServerConfiguration.GetOrUpdateSetting("timer.initialPoolCapacity", 1024);
        _maxPoolCapacity = ServerConfiguration.GetOrUpdateSetting("timer.maxPoolCapacity", _poolCapacity * 16);

        RefillPool(_poolCapacity, out var head, out var tail);
        ReturnToPool(_poolCapacity, head, tail);
    }

    private static void ReturnToPool(int amount, DelayCallTimer head, DelayCallTimer tail)
    {
        tail.Attach(_poolHead);
        _poolHead = head;
        _poolCount += amount;
#if DEBUG_TIMERS
        logger.Information("Returning to pool. ({Count} / {Capacity})", _poolCount, _poolCapacity);
#endif
    }

    private static DelayCallTimer GetFromPool()
    {
        if (_poolHead == null)
        {
            return null;
        }

        var timer = _poolHead;
        _poolHead = _poolHead._nextTimer as DelayCallTimer;
        timer.Detach();
        _poolCount--;

        return timer;
    }

    internal static void RefillPool(int amount, out DelayCallTimer head, out DelayCallTimer tail)
    {
#if DEBUG_TIMERS
        logger.Information("Filling pool with {Amount} timers.", amount);
#endif

        head = null;
        tail = null;

        for (var i = 0; i < amount; i++)
        {
            var timer = new DelayCallTimer(TimeSpan.Zero, TimeSpan.Zero, 0, null);
            timer.Attach(head);

            if (i == 0)
            {
                tail = timer;
            }

            head = timer;
        }
    }

    internal static async void RefillPoolAsync(int amountToRefill)
    {
        if (Interlocked.CompareExchange(ref _isRefilling, 0, 1) == 1)
        {
            return;
        }

        var (headTimer, tailTimer) = await Task.Run(
            () =>
            {
                RefillPool(amountToRefill, out var head, out var tail);
                return (head, tail);
            },
            Core.ClosingTokenSource.Token
        );

        ReturnToPool(amountToRefill, headTimer, tailTimer);
        _poolCapacity = amountToRefill;
        _isRefilling = 0;
    }
}
