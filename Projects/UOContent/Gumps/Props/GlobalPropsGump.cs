/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: GlobalPropsGump.cs                                              *
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
using Server.Buffers;
using Server.Commands.Generic;
using Server.Engines.Spawners;
using Server.Network;
using CPA = Server.CommandPropertyAttribute;

namespace Server.Gumps
{
    public class GlobalPropsGump : PropertiesGump
    {
        public static readonly string[] MobileAttributes =
        {
            "Str",
            "Dex",
            "Int",
            "HitsMaxSeed",
            "Hits",
            "DamageMin",
            "DamageMax",
            "ActiveSpeed",
            "PassiveSpeed",
            "VirtualArmor"
        };

        public GlobalPropsGump(Mobile mobile, object o) : base(mobile, o)
        {
        }

        public GlobalPropsGump(Mobile mobile, object o, Stack<StackEntry> stack, StackEntry parent) : base(
            mobile, o, stack, parent
        )
        {
        }

        public GlobalPropsGump(Mobile mobile, object o, Stack<StackEntry> stack, List<object> list, int page) : base(
            mobile, o, stack, list, page
        )
        {
        }

        protected override int TotalHeight => base.TotalHeight + PropsConfig.ApplySize;

        protected override void Initialize(int page)
        {
            base.Initialize(page);
            var totalHeight = TotalHeight - PropsConfig.ApplySize;

            AddButton(BackWidth / 3, PropsConfig.BorderSize + totalHeight + PropsConfig.BorderSize, 5204, 5205, 3);
        }

        public static object GetPropValue(object src, string propName) =>
            src.GetType().GetProperty(propName)?.GetValue(src, null);

        public override void OnResponse(NetState state, RelayInfo info)
        {
            var from = state.Mobile;

            if (!BaseCommand.IsAccessible(from, m_Object))
            {
                from.SendMessage("You may no longer access their properties.");
                return;
            }

            switch (info.ButtonID)
            {
                default:
                    {
                        base.OnResponse(state, info);
                        break;
                    }
                case 3: // Apply
                    {
                        if (MobileAttributes?.Length > 0)
                        {
                            using var propsBuilder = new ValueStringBuilder(64);
                            for (var i = 0; i < MobileAttributes.Length; i++)
                            {
                                var attr = MobileAttributes[i];
                                var prop = GetPropValue(m_Object, attr);
                                if (prop != null)
                                {
                                    if (i > 0)
                                    {
                                        propsBuilder.Append(' ');
                                    }

                                    propsBuilder.Append(attr);
                                    propsBuilder.Append(' ');
                                    propsBuilder.Append(prop.ToString()); // TODO: Replace with ZString, or IFormatter code
                                }
                            }

                            var name = m_Object.GetType().Name;
                            var props = propsBuilder.ToString();

                            foreach (var obj in World.Items.Values)
                            {
                                if (obj is BaseSpawner spawner)
                                {
                                    EditSpawnCommand.UpdateSpawner(spawner, name, null, props);
                                }
                            }
                        }
                        break;
                    }
            }
        }

        protected override bool ShowAttribute(string name)
        {
            foreach (var item in MobileAttributes)
            {
                if (item.InsensitiveEquals(name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
