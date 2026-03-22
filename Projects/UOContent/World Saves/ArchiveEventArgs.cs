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

public class ArchiveCompletedEventArgs
{
    public string ArchiveFilePath { get; }
    public ArchivePeriod Period { get; }
    public DateTime RangeStart { get; }
    public long FileSizeBytes { get; }
    public double ElapsedSeconds { get; }

    public ArchiveCompletedEventArgs(
        string archiveFilePath,
        ArchivePeriod period,
        DateTime rangeStart,
        long fileSizeBytes,
        double elapsedSeconds)
    {
        ArchiveFilePath = archiveFilePath;
        Period = period;
        RangeStart = rangeStart;
        FileSizeBytes = fileSizeBytes;
        ElapsedSeconds = elapsedSeconds;
    }
}

public class ArchiveFailedEventArgs
{
    public ArchivePeriod Period { get; }
    public DateTime RangeStart { get; }
    public Exception Exception { get; }

    public ArchiveFailedEventArgs(ArchivePeriod period, DateTime rangeStart, Exception exception)
    {
        Period = period;
        RangeStart = rangeStart;
        Exception = exception;
    }
}
