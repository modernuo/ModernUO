/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
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

        private static readonly List<DungeonChampionSpawn> DungeonSpawns = new();
        private static readonly List<LLChampionSpawn> LLSpawns = new();
        private static DateTime SliceTime;

        public static CannedEvilTimer Instance;

        public static void AddSpawn(DungeonChampionSpawn spawn)
        {
            DungeonSpawns.Add(spawn);
            Instance?.OnSlice(DungeonSpawns, false);
        }

        public static void AddSpawn(LLChampionSpawn spawn)
        {
            LLSpawns.Add(spawn);
            Instance?.OnSlice(LLSpawns, false);
        }

        public static void RemoveSpawn(DungeonChampionSpawn spawn)
        {
            DungeonSpawns.Remove(spawn);
            Instance?.OnSlice(DungeonSpawns, false);
        }

        public static void RemoveSpawn(LLChampionSpawn spawn)
        {
            LLSpawns.Remove(spawn);
            Instance?.OnSlice(LLSpawns, false);
        }

        public CannedEvilTimer() : base(TimeSpan.Zero, TimeSpan.FromMinutes(1.0))
        {
            Priority = TimerPriority.OneMinute;
            SliceTime = DateTime.Now;
        }

        public void OnSlice<T>(List<T> list) where T : ChampionSpawn
        {
            OnSlice(list, true);
        }

        public void OnSlice<T>(List<T> list, bool rotate) where T : ChampionSpawn
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
            if (!AutoRestart.Restarting && DateTime.Now >= SliceTime)
            {
                OnSlice(DungeonSpawns);
                OnSlice(LLSpawns);

                SliceTime = DateTime.Now.Date + TimeSpan.FromDays(1.0);
            }
        }
    }
}
