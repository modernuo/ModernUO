/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpeedHackPrevention.cs                                          *
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
using Server.Collections;

namespace Server.Network;

public static class SpeedHackPrevention
{
    private static readonly SortedSet<NetState> _sortedMovement = new(new NextMoveComparer());

    private static bool _shouldCheck;
    private static bool _speedHackEnabled;
    private static int _maxQueuedMovement;

    public static int MaxQueuedMovement
    {
        get => _maxQueuedMovement;
        private set => _maxQueuedMovement = Math.Clamp(value, 0, 256);
    }

    public static void Configure()
    {
        _speedHackEnabled = ServerConfiguration.GetOrUpdateSetting("netstate.speedhackEnabled", true);
        MaxQueuedMovement = ServerConfiguration.GetOrUpdateSetting("netstate.maxQueuedMovement", 25);

        EventSink.Logout += EventSink_Logout;
    }

    private static void EventSink_Logout(Mobile m)
    {
        var ns = m.NetState;

        ns.Sequence = 0;
        ns._readMovementSeqIndex = 0;
        ns._writeMovementSeqIndex = 0;

        _sortedMovement.Remove(ns);
    }

    public static bool ValidateSpeedHack(Mobile m, Direction d, byte seq)
    {
        // System is disabled
        if (!_speedHackEnabled)
        {
            return true;
        }

        var ns = m.NetState;

        // If we are queued, then we cannot go out of order!
        if (m.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        var maxMovements = ns._movementSequences.Length;
        var currentWrite = ns._writeMovementSeqIndex + 1;
        if (currentWrite >= maxMovements)
        {
            currentWrite = 0;
        }

        var movements = currentWrite - ns._readMovementSeqIndex;
        if (movements < 0)
        {
            movements += maxMovements;
        }

        var now = Core.TickCount;
        var diff = ns._nextMove - now;

        // We haven't moved, or at least recently
        if (movements == 0 || diff < 400)
        {
            if (diff < 0)
            {
                ns._nextMove = now;
            }

            return true;
        }

        ns._writeMovementSeqIndex = (byte)currentWrite;

        if (currentWrite == ns._readMovementSeqIndex)
        {
            ns.Disconnect($"Queued movements exceeded {maxMovements} packets.");
            return false;
        }

        m.Say($"Movement queued {movements}/{maxMovements}");
        ns._movementSequences[currentWrite] = (seq, d);
        _sortedMovement.Add(ns);
        _shouldCheck = true;
        return false;
    }

    internal static void Slice()
    {
        if (!_shouldCheck || _sortedMovement.Count == 0)
        {
            return;
        }

        var now = Core.TickCount;

        using var queue = PooledRefQueue<NetState>.Create(_sortedMovement.Count);

        foreach (var ns in _sortedMovement)
        {
            var from = ns.Mobile;

            // Staff have unlimited speed! Otherwise, we are done processing.
            if (from.AccessLevel == AccessLevel.Player && ns._nextMove - now > 25)
            {
                break;
            }

            // Read from the sequence circular array and increment
            var (seq, d) = ns._movementSequences[ns._readMovementSeqIndex];

            ns.TryMove(d, seq);
            queue.Enqueue(ns);
        }

        if (queue.Count == 0)
        {
            return;
        }

        _shouldCheck = false;

        while (queue.Count > 0)
        {
            var ns = queue.Dequeue();
            _sortedMovement.Remove(ns);

            // They were rejected!
            if (ns.Sequence == 0)
            {
                continue;
            }

            ns._readMovementSeqIndex++;
            if (ns._readMovementSeqIndex > ns._movementSequences.Length)
            {
                ns._readMovementSeqIndex = 0;
            }

            // Check if we are finished, then increment
            if (ns._writeMovementSeqIndex != ns._readMovementSeqIndex)
            {
                // Adding now executes after calling TryMove(), which has the new NextMove time
                _sortedMovement.Add(ns);
                _shouldCheck = true;
            }
        }
    }

    public static void TryMove(this NetState state, Direction dir, byte seq)
    {
        var from = state.Mobile;

        if (state.Sequence == 0 && seq != 0 || !from.Move(dir))
        {
            state.SendMovementRej(seq, from);
            state.Sequence = 0;
            state._readMovementSeqIndex = 0;
            state._writeMovementSeqIndex = 0;
        }
        else
        {
            state.SetNextSequence(seq);
        }
    }

    private class NextMoveComparer : IComparer<NetState>
    {
        public int Compare(NetState x, NetState y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (ReferenceEquals(null, y))
            {
                return 1;
            }

            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            var a = x._nextMove;
            var aRoll = a < 0;
            var b = y._nextMove;
            var bRoll = b < 0;

            if (aRoll && !bRoll)
            {
                return b.CompareTo(-a); // Reverse the check, and flip a
            }

            if (bRoll && !aRoll)
            {
                return a.CompareTo(-b); // Reverse the check, and flip b
            }

            return a.CompareTo(b);
        }
    }
}
