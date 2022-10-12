/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

        private static readonly HashSet<DungeonChampionSpawn> _dungeonSpawns = new();
        private static readonly HashSet<LLChampionSpawn> _lostLandsSpawns = new();
        private static DateTime _sliceTime;

        public static CannedEvilTimer Instance { get; private set; }

        public static void AddSpawn(DungeonChampionSpawn spawn)
        {
            _dungeonSpawns.Add(spawn);
            Instance?.OnSlice(_dungeonSpawns, false);
        }

        public static void AddSpawn(LLChampionSpawn spawn)
        {
            _lostLandsSpawns.Add(spawn);
            Instance?.OnSlice(_lostLandsSpawns, false);
        }

        public static void RemoveSpawn(DungeonChampionSpawn spawn)
        {
            _dungeonSpawns.Remove(spawn);
            Instance?.OnSlice(_dungeonSpawns, false);
        }

        public static void RemoveSpawn(LLChampionSpawn spawn)
        {
            _lostLandsSpawns.Remove(spawn);
            Instance?.OnSlice(_lostLandsSpawns, false);
        }

        public CannedEvilTimer() : base(TimeSpan.Zero, TimeSpan.FromMinutes(1.0))
        {
            _sliceTime = Core.Now;
        }

        public void OnSlice<T>(ICollection<T> list, bool rotate = true) where T : ChampionSpawn
        {
            if (list.Count > 0)
            {
                List<T> valid = new List<T>();

                foreach (T spawn in list)
                {
                    if (spawn.AlwaysActive && !spawn.Active)
                    {
                        spawn.ReadyToActivate = true;
                    }
                    else if (rotate && (!spawn.Active || spawn.Kills == 0 && spawn.Level == 0))
                    {
                        spawn.Active = false;
                        spawn.ReadyToActivate = false;

                        valid.Add(spawn);
                    }
                }

                if (valid.Count > 0)
                {
                    valid[Utility.Random(valid.Count)].ReadyToActivate = true;
                }
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
