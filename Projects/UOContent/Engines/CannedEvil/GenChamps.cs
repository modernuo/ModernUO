/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GenChamps.cs                                                    *
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
using Server.Logging;

namespace Server.Engines.CannedEvil
{
    public static class ChampionGenerator
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(ChampionGenerator));

        public static void Initialize()
        {
            CommandSystem.Register("GenChamps", AccessLevel.Owner, ChampGen_OnCommand);
        }

        private static readonly ChampionEntry[] LLLocations = {
            new(typeof(LLChampionSpawn), new Point3D(5511, 2360, 42), Map.Felucca, new Point3D(5439, 2323, 26 ), Map.Felucca),
            new(typeof(LLChampionSpawn), new Point3D(6038, 2401, 47), Map.Felucca, new Point3D(5988, 2340, 24), Map.Felucca),
            new(typeof(LLChampionSpawn), new Point3D(5549, 2640, 16), Map.Felucca, new Point3D(5645, 2696, -8), Map.Felucca),
            new(typeof(LLChampionSpawn), new Point3D(5636, 2916, 37), Map.Felucca, new Point3D(5721, 2949, 28), Map.Felucca),
            new(typeof(LLChampionSpawn), new Point3D(6035, 2943, 50), Map.Felucca, new Point3D(6098, 2997, 17), Map.Felucca),
            new(typeof(LLChampionSpawn), new Point3D(5265, 3171, 105), Map.Felucca, new Point3D(5314, 3232, 2), Map.Felucca),
            new(typeof(LLChampionSpawn), new Point3D(5282, 3368, 50), Map.Felucca, new Point3D(5215, 3318, 3), Map.Felucca),
            new(typeof(LLChampionSpawn), new Point3D(5207, 3637, 20), Map.Felucca, new Point3D(5263, 3687, 0), Map.Felucca),
            new(typeof(LLChampionSpawn), new Point3D(5954, 3475, 25), Map.Felucca, new Point3D(6013, 3529, 0), Map.Felucca),
            new(typeof(LLChampionSpawn), new Point3D(5982, 3882, 20), Map.Felucca, new Point3D(5929, 3820, -1), Map.Felucca),
            new(typeof(LLChampionSpawn), new Point3D(5724, 3991, 41), Map.Felucca, new Point3D(5774, 4041, 26), Map.Felucca),
            new(typeof(LLChampionSpawn), ChampionSpawnType.ForestLord, new Point3D(5559, 3757, 21), Map.Felucca, new Point3D(5513, 3878, 3), Map.Felucca),
        };

        private static readonly ChampionEntry[] DungeonLocations = {
            new(typeof(DungeonChampionSpawn), ChampionSpawnType.UnholyTerror, new Point3D(5179, 709, 20), Map.Felucca, new Point3D(4111, 432, 5), Map.Felucca),
            new(typeof(DungeonChampionSpawn), ChampionSpawnType.VerminHorde, new Point3D(5557, 827, 65), Map.Felucca, new Point3D(5580, 632, 30), Map.Felucca),
            new(typeof(DungeonChampionSpawn), ChampionSpawnType.ColdBlood, new Point3D(5259, 837, 64), Map.Felucca, new Point3D(1176, 2637, 0), Map.Felucca),
            new(typeof(DungeonChampionSpawn), ChampionSpawnType.Abyss, new Point3D(5815, 1352, 5), Map.Felucca, new Point3D(2923, 3406, 8), Map.Felucca),
            new(typeof(DungeonChampionSpawn), ChampionSpawnType.Arachnid, new Point3D(5190, 1607, 20), Map.Felucca, new Point3D(5482, 3161, -54), Map.Felucca),
        };

        [Usage("GenChamps")]
        [Description("Generates champions for Felucca Dungeons & Lost Lands.")]
        private static void ChampGen_OnCommand(CommandEventArgs e)
        {
            /*
            //We take the assumption that we are spawning managed champions
            for (int i = CannedEvilTimer.DungeonSpawns.Count - 1; i >= 0; i--)
                CannedEvilTimer.DungeonSpawns[i].Delete();

            for (int i = CannedEvilTimer.LLSpawns.Count - 1; i >= 0; i--)
                CannedEvilTimer.LLSpawns[i].Delete();
            */

            //We assume that all champion spawns are generated here.
            List<ChampionSpawn> spawns = new List<ChampionSpawn>();
            foreach (Item item in World.Items.Values)
            {
                if (item is ChampionSpawn spawn)
                {
                    spawns.Add(spawn);
                }
            }

            for (int i = spawns.Count - 1; i >= 0; i--)
            {
                spawns[i].Delete();
            }

            Process(DungeonLocations);
            Process(LLLocations);
            //ProcessIlshenar();
            //ProcessTokuno();
        }

        private static void Process(ChampionEntry[] entries)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                ChampionEntry entry = entries[i];

                try
                {
                    if (Activator.CreateInstance(entry.m_ChampType) is ChampionSpawn spawn)
                    {
                        spawn.RandomizeType = entry.m_RandomizeType;
                        spawn.Type = entry.m_Type;
                        spawn.MoveToWorld(entry.m_SignLocation, entry.m_Map);
                        spawn.EjectLocation = entry.m_EjectLocation;
                        spawn.EjectMap = entry.m_EjectMap;
                        if (spawn.AlwaysActive)
                        {
                            spawn.ReadyToActivate = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Error(
                        e,
                        "Failed to generate champion \"{Type}\" at {Location} ({Map}).",
                        entry.m_ChampType.FullName,
                        entry.m_SignLocation,
                        entry.m_Map
                    );
                }
            }
        }
    }
}
