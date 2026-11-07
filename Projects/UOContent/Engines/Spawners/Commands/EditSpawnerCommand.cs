/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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

namespace Server.Engines.Spawners;

public class EditSpawnCommand : BaseCommand
{
    public static void Configure()
    {
        TargetCommands.Register(new EditSpawnCommand());
    }

    public EditSpawnCommand()
    {
        AccessLevel = AccessLevel.GameMaster;
        Supports = CommandSupport.Complex | CommandSupport.Simple;
        Commands = ["EditSpawner"];
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

        ParseArguments(e.ArgString, name, out var argStr, out var propsStr, out var whereStr);

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

    /// <summary>
    /// Splits <c>&lt;type&gt; [arguments] [set &lt;properties&gt;] [where &lt;properties&gt;]</c> into its parts.
    /// Each part is trimmed, and is null when absent or empty.
    /// </summary>
    internal static void ParseArguments(
        string argString, string name, out string arguments, out string properties, out string where
    )
    {
        var argSpan = argString.Length > name.Length ? argString.AsSpan(name.Length + 1) : default;
        var setIndex = argSpan.InsensitiveIndexOf("set ");
        var whereIndex = argSpan.InsensitiveIndexOf("where ");

        var end = whereIndex > -1 ? whereIndex : argSpan.Length;
        var findmatch = whereIndex > -1 ? argSpan[(whereIndex + 6)..] : default;
        ReadOnlySpan<char> props = default;

        // A "set " after "where " is part of the where clause.
        if (setIndex > -1 && setIndex < end)
        {
            props = argSpan[(setIndex + 4)..end];
            argSpan = argSpan[..setIndex];
        }
        else
        {
            argSpan = argSpan[..end];
        }

        arguments = argSpan.Trim().ToString().DefaultIfNullOrEmpty(null);
        properties = props.Trim().ToString().DefaultIfNullOrEmpty(null);
        where = findmatch.Trim().ToString().DefaultIfNullOrEmpty(null);
    }

    public static void UpdateSpawner(BaseSpawner spawner, string name, string arguments, string properties, string find = null)
    {
        for (var i = 0; i < spawner.Entries.Count; i++)
        {
            var entry = spawner.Entries[i];

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
