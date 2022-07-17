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
using Server.Mobiles;
using Server.Network;
using CPA = Server.CommandPropertyAttribute;

namespace Server.Gumps
{
    public class SpawnPropsGump : PropertiesGump
    {
        public static readonly HashSet<string> MobileAttributes = new()
        {
            nameof(Mobile.Hue),
            nameof(Mobile.Str),
            nameof(Mobile.Dex),
            nameof(Mobile.Int),
            nameof(BaseCreature.HitsMaxSeed),
            nameof(BaseCreature.Hits),
            nameof(BaseCreature.DamageMin),
            nameof(BaseCreature.DamageMax),
            nameof(BaseCreature.ActiveSpeed),
            nameof(BaseCreature.PassiveSpeed),
            nameof(BaseCreature.VirtualArmor)
        };

        private List<object> _spawners;

        public SpawnPropsGump(Mobile mobile, object o, List<object> spawners) : base(mobile, o)
        {
            _spawners = spawners;
        }

        public SpawnPropsGump(Mobile mobile, object o, Stack<StackEntry> stack, StackEntry parent, List<object> spawners) : base(
            mobile, o, stack, parent
        )
        {
            _spawners = spawners;
        }

        public SpawnPropsGump(Mobile mobile, object o, Stack<StackEntry> stack, List<object> list, int page, List<object> spawners) : base(
            mobile, o, stack, list, page
        )
        {
            _spawners = spawners;
        }

        protected override int TotalHeight => base.TotalHeight + PropsConfig.ApplySize;

        protected override void Initialize(int page)
        {
            base.Initialize(page);
            var totalHeight = TotalHeight - PropsConfig.ApplySize;

            AddButton(BackWidth / 3, PropsConfig.BorderSize + totalHeight + PropsConfig.BorderSize, 5204, 5205, 3);
        }

        public override void SendPropertiesGump() =>
            m_Mobile.SendGump(new SpawnPropsGump(m_Mobile, m_Object, m_Stack, m_List, m_Page, _spawners));

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
                        using var propsBuilder = ValueStringBuilder.Create();
                        bool first = true;
                        foreach (var attr in MobileAttributes)
                        {
                            var prop = GetPropValue(m_Object, attr);
                            if (prop != null)
                            {
                                if (first)
                                {
                                    first = false;
                                }
                                else
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

                        m_Mobile.SendMessage("Updating spawners...");

                        foreach (var obj in _spawners)
                        {
                            if (obj is BaseSpawner spawner)
                            {
                                EditSpawnCommand.UpdateSpawner(spawner, name, null, props);
                            }
                        }

                        m_Mobile.SendMessage("Update completed.");

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
