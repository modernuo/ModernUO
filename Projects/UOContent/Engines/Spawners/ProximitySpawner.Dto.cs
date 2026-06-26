/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ProximitySpawner.Dto.cs                                         *
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

public partial class ProximitySpawner
{
    public override SpawnerDto ToDto()
    {
        var homeRange = DtoHomeRange;
        return new ProximitySpawnerDto
        {
            Guid = Guid,
            Name = DtoName,
            Location = Location,
            Map = Map,
            Count = Count,
            MinDelay = MinDelay,
            MaxDelay = MaxDelay,
            Team = Team,
            WalkingRange = DtoWalkingRange,
            Entries = Entries,
            SpawnLocationIsHome = SpawnLocationIsHome,
            SpawnPositionMode = DtoSpawnPositionMode,
            MaxSpawnAttempts = DtoMaxSpawnAttempts,
            HomeRange = homeRange,
            SpawnBounds = homeRange >= 0 ? default : SpawnBounds,
            TriggerRange = TriggerRange,
            SpawnMessage = SpawnMessage,
            Instant = InstantFlag
        };
    }
}
