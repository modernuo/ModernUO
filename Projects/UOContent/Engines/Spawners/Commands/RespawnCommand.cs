/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: RespawnCommand.cs                                               *
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
using Server.Commands.Generic;
using Server.Network;

namespace Server.Engines.Spawners
{
    public class RespawnCommand : BaseCommand
    {
        public static void Initialize()
        {
            TargetCommands.Register(new RespawnCommand());
        }

        public RespawnCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Complex | CommandSupport.Simple;
            Commands = new[] { "Respawn" };
            ObjectTypes = ObjectTypes.Items;
            Usage = "Respawn";
            Description = "Respawns the given the spawners.";
            ListOptimized = true;
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            if (list.Count == 0)
            {
                LogFailure("No matching objects found.");
                return;
            }

            e.Mobile.SendMessage("Respawning...");

            NetState.FlushAll();

            foreach (var obj in list)
            {
                if (obj is ISpawner spawner)
                {
                    spawner.Respawn();
                }
            }

            e.Mobile.SendMessage("Respawn completed.");
        }
    }
}
