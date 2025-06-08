/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CannedEvilTimer.cs                                              *
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
using System.Collections.Generic;
using Server.Collections;
using Server.Misc;

namespace Server.Engines.CannedEvil
{
    public class CannedEvilTimer : Timer
    {
        public static void Initialize()
        {
            // TODO: Needs configuration
            Instance = new CannedEvilTimer();
            Instance.Start();
            Instance.OnTick();
        }

        private static readonly HashSet<ChampionSpawn> _dungeonSpawns = new();
        private static readonly HashSet<ChampionSpawn> _lostLandsSpawns = new();
        private static DateTime _sliceTime;

        public static CannedEvilTimer Instance { get; private set; }

        public static void AddSpawn(DungeonChampionSpawn spawn)
        {
            _dungeonSpawns.Add(spawn);
            OnSlice(_dungeonSpawns, false);
        }

        public static void AddSpawn(LLChampionSpawn spawn)
        {
            _lostLandsSpawns.Add(spawn);
            OnSlice(_lostLandsSpawns, false);
        }

        public static void RemoveSpawn(DungeonChampionSpawn spawn)
        {
            _dungeonSpawns.Remove(spawn);
            OnSlice(_dungeonSpawns, false);
        }

        public static void RemoveSpawn(LLChampionSpawn spawn)
        {
            _lostLandsSpawns.Remove(spawn);
            OnSlice(_lostLandsSpawns, false);
        }

        public CannedEvilTimer() : base(TimeSpan.Zero, TimeSpan.FromMinutes(1.0))
        {
            _sliceTime = Core.Now;
        }

        public static void OnSlice(HashSet<ChampionSpawn> spawns, bool rotate = true)
        {
            if (spawns.Count <= 0)
            {
                return;
            }

            using var queue = rotate ? PooledRefQueue<Item>.Create() : default;

            foreach (var spawn in spawns)
            {
                if (spawn.AlwaysActive && !spawn.Active)
                {
                    spawn.ReadyToActivate = true;
                }
                else if (rotate && (!spawn.Active || spawn.Kills == 0 && spawn.Level == 0))
                {
                    spawn.Active = false;
                    spawn.ReadyToActivate = false;

                    queue.Enqueue(spawn);
                }
            }

            if (rotate && queue.Count > 0)
            {
                ((ChampionSpawn)queue.PeekRandom()).ReadyToActivate = true;
            }
        }

        protected override void OnTick()
        {
            if (!AutoRestart.Restarting && Core.Now >= _sliceTime)
            {
                OnSlice(_dungeonSpawns);
                OnSlice(_lostLandsSpawns);

                _sliceTime = Core.Now.Date + TimeSpan.FromDays(1.0);
            }
        }
    }
}
