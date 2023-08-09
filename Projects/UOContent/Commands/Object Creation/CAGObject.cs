/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CAGObject.cs                                                    *
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
using System.Text.Json.Serialization;
using Server.Gumps;

namespace Server.Commands
{
    public class CAGObject : CAGNode
    {
        [JsonPropertyName("type")]
        public Type Type { get; set; }

#nullable enable
        [JsonPropertyName("gfx")]
        public int? ItemID { get; set; }

        [JsonPropertyName("hue")]
        public int? Hue { get; set; }
#nullable restore

        public CAGCategory Parent { get; set; }

        public override string Title => Type?.Name ?? "bad type";

        public override void OnClick(Mobile from, int page)
        {
            if (Type == null)
            {
                from.SendMessage("That is an invalid type name.");
            }
            else
            {
                CommandSystem.Handle(from, $"{CommandSystem.Prefix}Add {Type.Name}");

                from.SendGump(new CategorizedAddGump(from, Parent, page));
            }
        }
    }
}
