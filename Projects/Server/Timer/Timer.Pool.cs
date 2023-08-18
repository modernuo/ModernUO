/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using System.Threading.Tasks;

namespace Server;

public partial class Timer
{
    private static int _maxPoolCapacity;
    private static int _poolCapacity;
    private static int _poolCount;
    private static DelayCallTimer _poolHead;
    private static bool _isRefilling;

    public static void CheckTimerPool()
    {
        if (_poolCount > 0 || _isRefilling)
        {
            return;
        }

        var amountToGrow = _poolCapacity * 2;
        var amountToRefill = Math.Min(_maxPoolCapacity, amountToGrow);

        var maximumHit = amountToGrow > amountToRefill ? " Maximum pool size has been reached." : "";
        var warningMessage = $"Refilling timer pool with {{AmountRefill}}.{maximumHit}";

        logger.Warning(warningMessage, amountToRefill);
        RefillPoolAsync(amountToRefill);
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
        _isRefilling = true;

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
        _isRefilling = false;
    }
}
