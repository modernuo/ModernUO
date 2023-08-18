/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ChampionCommands.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Targeting;

namespace Server.Engines.CannedEvil
{
    public static class ChampionCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("ClearChampByTarget", AccessLevel.Administrator, KillByTarget_OnCommand);
            CommandSystem.Register("ClearChampByRegion", AccessLevel.Administrator, KillByRegion_OnCommand);
        }

        [Usage("ClearChampByTarget")]
        [Description("Kills all minions of a champion spawn.")]
        private static void KillByTarget_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new KillTarget();
            e.Mobile.SendMessage("Which champion spawn would you like to clear?");
        }

        private class KillTarget : Target
        {
            public KillTarget() : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (from == null || from.AccessLevel < AccessLevel.Administrator)
                {
                    return;
                }

                ChampionSpawn spawn = targ switch
                {
                    ChampionSpawn championSpawn => championSpawn,
                    IdolOfTheChampion champion  => champion.Spawn,
                    ChampionAltar altar         => altar.Spawn,
                    ChampionPlatform platform   => platform.Spawn,
                    _                           => null
                };

                if (spawn == null)
                {
                    from.SendMessage("That is not a valid target. Please target the champion, altar, platform, or idol.");
                }

                spawn?.DeleteCreatures();
                spawn?.Champion?.Delete();
            }
        }

        [Usage("ClearChampByRegion")]
        [Description("Kills all minions of a champion spawn.")]
        private static void KillByRegion_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile.Region is ChampionSpawnRegion { Spawn: { } } region)
            {
                region.Spawn.DeleteCreatures();
                region.Spawn.Champion?.Delete();
            }
            else
            {
                e.Mobile.SendMessage("You are not in a champion spawn region.");
            }
        }
    }
}
