/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ArchiveEventArgs.cs                                             *
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

public class ArchiveCompletedEventArgs(
    string archiveFilePath,
    ArchivePeriod period,
    DateTime rangeStart,
    long fileSizeBytes,
    double elapsedSeconds
)
{
    public string ArchiveFilePath { get; } = archiveFilePath;
    public ArchivePeriod Period { get; } = period;
    public DateTime RangeStart { get; } = rangeStart;
    public long FileSizeBytes { get; } = fileSizeBytes;
    public double ElapsedSeconds { get; } = elapsedSeconds;
}

public class ArchiveFailedEventArgs(ArchivePeriod period, DateTime rangeStart, Exception exception)
{
    public ArchivePeriod Period { get; } = period;
    public DateTime RangeStart { get; } = rangeStart;
    public Exception Exception { get; } = exception;
}
