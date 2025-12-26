/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ISpawnerEntry.cs                                                *
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

namespace Server.Engines.Spawners;

/// <summary>
/// Defines the contract for a spawner entry that tracks spawn configuration and spawned entities.
/// </summary>
public interface ISpawnerEntry
{
    /// <summary>
    /// Gets or sets the type name of the entity to spawn.
    /// </summary>
    string SpawnedName { get; set; }

    /// <summary>
    /// Gets or sets the weighted probability of this entry being selected for spawning.
    /// </summary>
    int SpawnedProbability { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of entities this entry can spawn.
    /// </summary>
    int SpawnedMaxCount { get; set; }

    /// <summary>
    /// Gets or sets the property assignments to apply after entity creation.
    /// Format: "PropertyName value PropertyName value ..."
    /// </summary>
    string Properties { get; set; }

    /// <summary>
    /// Gets or sets the constructor parameters for entity creation.
    /// Format: "param1 param2 ..."
    /// </summary>
    string Parameters { get; set; }

    /// <summary>
    /// Gets the list of currently spawned entities for this entry.
    /// </summary>
    IReadOnlyList<ISpawnable> Spawned { get; }

    /// <summary>
    /// Gets or sets the validation flags indicating any issues with this entry.
    /// </summary>
    EntryFlags Valid { get; set; }

    /// <summary>
    /// Gets whether this entry has reached its maximum spawn count.
    /// </summary>
    bool IsFull { get; }

    /// <summary>
    /// Adds a spawned entity to this entry's tracking list.
    /// </summary>
    void AddToSpawned(ISpawnable spawnable);

    /// <summary>
    /// Removes a spawned entity from this entry's tracking list.
    /// </summary>
    void RemoveFromSpawned(ISpawnable spawnable);

    /// <summary>
    /// Performs defragmentation by removing invalid spawned entities.
    /// </summary>
    void Defrag(BaseSpawner parent);
}
