/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IArchiveDestination.cs                                          *
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
/// Represents a destination for completed archive files.
/// Implementations run on a background thread (ThreadPool).
/// </summary>
public interface IArchiveDestination
{
    /// <summary>
    /// A human-readable name for logging (e.g., "Local Filesystem", "S3 us-east-1").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called after a local archive file is successfully created and verified.
    /// Must be safe to call from a ThreadPool thread.
    /// </summary>
    /// <param name="archiveFilePath">Full path to the verified .tar.zst archive.</param>
    /// <param name="period">The archive period (Hourly, Daily, Monthly).</param>
    /// <param name="rangeStart">The start of the time range this archive covers.</param>
    /// <returns>True if the destination successfully received the archive.</returns>
    bool SendArchive(string archiveFilePath, ArchivePeriod period, DateTime rangeStart);

    /// <summary>
    /// Returns the number of local archives to retain for this period.
    /// Destinations that keep their own copies may return 0.
    /// </summary>
    int GetRetentionCount(ArchivePeriod period);
}
