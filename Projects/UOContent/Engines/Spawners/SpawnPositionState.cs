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
    private const int AttemptWindow = 25;

    // Failure tracking for auto-detection
    private int _spawnAttempts;
    private int _nonTransientFailures;

    // Spiral scan state
    public int SpiralRing;
    public int SpiralRingPosition;
    public bool SpiralComplete;

    /// <summary>
    /// Resets all state. Called when spawner moves or bounds change.
    /// </summary>
    public void Reset()
    {
        _spawnAttempts = 0;
        _nonTransientFailures = 0;
        SpiralRing = 0;
        SpiralRingPosition = 0;
        SpiralComplete = false;
    }

    /// <summary>
    /// Records a successful spawn for failure tracking.
    /// </summary>
    public void RecordSuccess()
    {
        _spawnAttempts++;
        if (_spawnAttempts >= AttemptWindow)
        {
            // Reset window
            _spawnAttempts = 0;
            _nonTransientFailures = 0;
        }
    }

    /// <summary>
    /// Records a non-transient spawn failure for auto-detection.
    /// </summary>
    public void RecordNonTransientFailure()
    {
        _spawnAttempts++;
        _nonTransientFailures++;
    }

    /// <summary>
    /// Returns true if the spawner should cache successful positions.
    /// </summary>
    public bool ShouldCachePositions(SpawnPositionMode mode) =>
        mode == SpawnPositionMode.Enabled || (mode == SpawnPositionMode.Automatic && _nonTransientFailures > 0);

    /// <summary>
    /// Returns true if the spawner should be marked as abandoned.
    /// </summary>
    public bool ShouldAbandon() =>
        // After spiral scan complete, if we still have 100% failure rate
        SpiralComplete && _spawnAttempts >= AttemptWindow && _nonTransientFailures >= _spawnAttempts;
}
