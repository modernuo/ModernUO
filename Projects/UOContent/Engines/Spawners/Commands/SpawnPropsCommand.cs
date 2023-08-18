/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpawnPropsCommand.cs                                            *
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
using Server.Gumps;
using Server.Targeting;

using static Server.Types;

namespace Server.Engines.Spawners
{
    public class SpawnPropsCommand : BaseCommand
    {
        public static void Initialize()
        {
            TargetCommands.Register(new SpawnPropsCommand());
        }

        public SpawnPropsCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Complex | CommandSupport.Simple;
            Commands = new[] { "SpawnProps" };
            ObjectTypes = ObjectTypes.Items;
            Usage = "SpawnProps";
            Description = "Shows a props gump that will modify the properties of spawn entries related to the chosen entity";
            ListOptimized = true;
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            if (list.Count == 0)
            {
                LogFailure("No matching objects found.");
                return;
            }

            e.Mobile.SendMessage("Target the object you want to use as a template for modifying the spawner properties.");
            e.Mobile.Target = new InternalTarget(list);
        }

        private class InternalTarget : Target
        {
            private readonly List<object> _list;

            public InternalTarget(List<object> list) : base(-1, false, TargetFlags.None) => _list = list;

            protected override void OnTarget(Mobile from, object targeted)
            {
                var type = targeted.GetType();
                if (!IsEntity(type))
                {
                    from.SendMessage("No type with that name was found.");
                }

                from.SendGump(new SpawnPropsGump(from, targeted, _list));
            }
        }
    }
}
