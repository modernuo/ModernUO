/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpawnPositionState.cs                                           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Engines.Spawners;

/// <summary>
/// Runtime state for spawn position optimization.
/// Not serialized - resets on server restart.
/// </summary>
public sealed class SpawnPositionState
{
    // Tunable thresholds for auto-detection
    private const int FailureThreshold = 5;
    private const int AbandonThreshold = 25;

    // Failure tracking for auto-detection
    private int _nonTransientFailures;

    // Abandon tracking - counts consecutive cache misses or Location-only results
    private int _consecutiveUselessResults;

    // Spiral scan state
    public int SpiralRing;
    public int SpiralRingPosition;
    public bool SpiralComplete;

    /// <summary>
    /// Resets all state. Called when spawner moves or bounds change.
    /// </summary>
    public void Reset()
    {
        _nonTransientFailures = 0;
        _consecutiveUselessResults = 0;
        SpiralRing = 0;
        SpiralRingPosition = 0;
        SpiralComplete = false;
    }

    /// <summary>
    /// Records a useful cache hit (position other than spawner's own location).
    /// Resets the abandon counter.
    /// </summary>
    public void RecordUsefulCacheHit() => _consecutiveUselessResults = 0;

    /// <summary>
    /// Records a useless result (cache miss or cache returned spawner's location).
    /// Counts toward abandonment threshold.
    /// </summary>
    public void RecordUselessResult() => _consecutiveUselessResults++;

    /// <summary>
    /// Records a non-transient spawn failure for auto-detection.
    /// </summary>
    public void RecordNonTransientFailure() => _nonTransientFailures++;

    /// <summary>
    /// Returns true if the spawner should cache successful positions.
    /// </summary>
    public bool ShouldCachePositions(SpawnPositionMode mode) =>
        mode == SpawnPositionMode.Enabled || mode == SpawnPositionMode.Automatic && _nonTransientFailures > FailureThreshold;

    /// <summary>
    /// Returns true if the spawner should be marked as abandoned.
    /// Triggers after spiral completes, and we get consecutive useless results
    /// (cache misses or cache only returning spawner's own location).
    /// </summary>
    public bool ShouldAbandon() => SpiralComplete && _consecutiveUselessResults >= AbandonThreshold;
}
