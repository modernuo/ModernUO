/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LocalArchiveDestination.cs                                      *
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

namespace Server.Saves;

/// <summary>
/// Built-in archive destination representing the local filesystem.
/// The archive is already on local disk, so SendArchive is a no-op.
/// Participates in the retention/pruning protocol via configurable counts.
/// </summary>
public class LocalArchiveDestination : IArchiveDestination
{
    public string Name => "Local Filesystem";

    private readonly int _hourlyRetention;
    private readonly int _dailyRetention;
    private readonly int _monthlyRetention;

    public LocalArchiveDestination(int hourlyRetention, int dailyRetention, int monthlyRetention)
    {
        _hourlyRetention = hourlyRetention;
        _dailyRetention = dailyRetention;
        _monthlyRetention = monthlyRetention;
    }

    public bool SendArchive(string archiveFilePath, ArchivePeriod period, DateTime rangeStart)
    {
        // Archive is already on local filesystem — nothing to do.
        return true;
    }

    public int GetRetentionCount(ArchivePeriod period) => period switch
    {
        ArchivePeriod.Hourly  => _hourlyRetention,
        ArchivePeriod.Daily   => _dailyRetention,
        ArchivePeriod.Monthly => _monthlyRetention,
        _                     => 0
    };
}
