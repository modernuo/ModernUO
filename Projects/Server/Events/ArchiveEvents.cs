/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ArchiveEvents.cs                                                *
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
using System.Runtime.CompilerServices;
using Server.Saves;

namespace Server;

public static partial class EventSink
{
    public static event Action<ArchiveCompletedEventArgs> ArchiveCompleted;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeArchiveCompleted(ArchiveCompletedEventArgs args) =>
        ArchiveCompleted?.Invoke(args);

    public static event Action<ArchiveFailedEventArgs> ArchiveFailed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeArchiveFailed(ArchiveFailedEventArgs args) =>
        ArchiveFailed?.Invoke(args);
}
