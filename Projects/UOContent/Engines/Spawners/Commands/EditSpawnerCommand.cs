/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EditSpawnerCommand.cs                                           *
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
using Server.Commands.Generic;
using static Server.Types;

namespace Server.Engines.Spawners
{

    public class EditSpawnCommand : BaseCommand
    {
        public static void Initialize()
        {
            TargetCommands.Register(new EditSpawnCommand());
        }

        public EditSpawnCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Complex | CommandSupport.Simple;
            Commands = new[] { "EditSpawner" };
            ObjectTypes = ObjectTypes.Items;
            Usage = "EditSpawner <type> <arguments> set <properties> where <properties>";
            Description = "Modifies spawners arguments and properties for the given type";
            ListOptimized = true;
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            var args = e.Arguments;

            if (args.Length <= 1)
            {
                LogFailure(Usage);
                return;
            }

            if (list.Count == 0)
            {
                LogFailure("No matching objects found.");
                return;
            }

            var name = args[0];

            var type = AssemblyHandler.FindTypeByName(name);

            if (!IsEntity(type))
            {
                LogFailure("No type with that name was found.");
                return;
            }

            var argSpan = e.ArgString.AsSpan(name.Length + 1);
            var setIndex = argSpan.InsensitiveIndexOf("set ");
            var where = argSpan.InsensitiveIndexOf("where ");

            ReadOnlySpan<char> props = null, findmatch = null;

            if (setIndex > -1 || where > -1)
            {
                var start = setIndex + 4;
                var len = where > -1 ? where : argSpan.Length;
                findmatch = argSpan.Slice(len < argSpan.Length ? len + 6 : argSpan.Length);
                if (setIndex > -1)
                {
                    props = argSpan[start..^len];
                    argSpan = argSpan[..setIndex];
                }
                else
                {
                    argSpan = argSpan[..^len];
                }
            }

            var argStr = argSpan.ToString().DefaultIfNullOrEmpty(null);
            var propsStr = props.ToString().DefaultIfNullOrEmpty(null);
            var whereStr = findmatch.ToString().DefaultIfNullOrEmpty(null);

            e.Mobile.SendMessage("Updating spawners...");

            foreach (var obj in list)
            {
                if (obj is BaseSpawner spawner)
                {
                    UpdateSpawner(spawner, name, argStr, propsStr, whereStr);
                }
            }

            e.Mobile.SendMessage("Update completed.");
        }

        public static void UpdateSpawner(BaseSpawner spawner, string name, string arguments, string properties, string find = null)
        {
            foreach (var entry in spawner.Entries)
            {
                // TODO: Should cache spawn type on the entry
                if (!entry.SpawnedName.InsensitiveEquals(name))
                {
                    continue;
                }

                var found = find != null ? entry.Properties?.LastIndexOf(find, StringComparison.OrdinalIgnoreCase) : null;
                var shouldUpdate = find == null || found > -1 &&
                    (found + find.Length == entry.Properties.Length ||
                     char.IsWhiteSpace(entry.Properties[(int)found + find.Length]));

                if (shouldUpdate)
                {
                    if (arguments != null)
                    {
                        entry.Parameters = arguments;
                    }

                    entry.Properties = properties;
                }
            }
        }
    }
}
