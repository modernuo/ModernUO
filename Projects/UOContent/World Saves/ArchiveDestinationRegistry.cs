/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ArchiveDestinationRegistry.cs                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;

namespace Server.Saves;

/// <summary>
/// Registration point for archive destinations. Register during Configure() phase.
/// No locks needed — registration is single-threaded, reading happens on one
/// ThreadPool thread at a time (guarded by the archive concurrency flag).
/// </summary>
public static class ArchiveDestinationRegistry
{
    private static readonly List<IArchiveDestination> _destinations = [];

    public static IReadOnlyList<IArchiveDestination> Destinations => _destinations;

    public static void Register(IArchiveDestination destination) => _destinations.Add(destination);

    public static void Unregister(IArchiveDestination destination) => _destinations.Remove(destination);
}
