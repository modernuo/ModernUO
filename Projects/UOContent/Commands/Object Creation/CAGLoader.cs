/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CAGLoader.cs                                                    *
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
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Items;
using Server.Json;
using Server.Logging;
using Server.Utilities;

namespace Server.Commands;

public static class CAGLoader
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CAGLoader));

    public static CAGCategory Load()
    {
        var root = new CAGCategory("Add Menu");
        var path = Path.Combine(Core.BaseDirectory, "Data/categorization.json");

        var list = JsonConfig.Deserialize<List<CAGJson>>(path);
        if (list == null)
        {
            throw new JsonException($"Failed to deserialize {path}.");
        }

        // Not an optimized solution
        foreach (var cag in list)
        {
            var parent = root;
            // Navigate through the dot notation categories until we find the last one
            var categories = cag.Category.Split(".");
            for (var i = 0; i < categories.Length; i++)
            {
                var category = categories[i];

                // No children, so let's make one
                if (parent.Nodes == null)
                {
                    var cat = new CAGCategory(category, parent);
                    parent.Nodes = new CAGNode[] { cat };
                    parent = cat;
                    continue;
                }

                var oldParent = parent;
                for (var j = 0; j < parent.Nodes.Length; j++)
                {
                    var node = parent.Nodes[j];
                    if (category == node.Title && node is CAGCategory cat)
                    {
                        parent = cat;
                        break;
                    }
                }

                // Didn't find the child, let's add it
                if (oldParent == parent)
                {
                    var nodes = parent.Nodes;
                    parent.Nodes = new CAGNode[nodes.Length + 1];
                    Array.Copy(nodes, parent.Nodes, nodes.Length);
                    var cat = new CAGCategory(category, parent);
                    parent.Nodes[^1] = cat;
                    parent = cat;
                }
            }

            // Set the objects associated with the child most node
            var pooledList = new List<CAGNode>(cag.Objects.Length);
            for (var i = 0; i < cag.Objects.Length; i++)
            {
                var cagObj = cag.Objects[i];
                cagObj.Parent = parent;

                // Set ItemID and Hue
                if (cagObj.Hue == null || cagObj.ItemID == null)
                {
                    var type = cagObj.Type;

                    try
                    {
                        if (type.IsAssignableTo(typeof(Item)))
                        {
                            var item = cagObj.Type.CreateInstance<Item>();

                            if (cagObj.ItemID == null)
                            {
                                var itemID = item.ItemID;

                                if (item is BaseAddon addon && addon.Components.Count == 1)
                                {
                                    itemID = addon.Components[0].ItemID;
                                }

                                if (itemID > TileData.MaxItemValue)
                                {
                                    itemID = 1;
                                }

                                cagObj.ItemID = itemID;
                            }

                            if (cagObj.Hue == null)
                            {
                                int hue = item.Hue & 0x7FFF;
                                hue = (hue & 0x4000) != 0 ? 0 : hue;
                                cagObj.Hue = hue == 0 ? null : hue;
                            }

                            item.Delete();
                        }

                        if (type.IsAssignableTo(typeof(Mobile)))
                        {
                            var m = cagObj.Type.CreateInstance<Mobile>();
                            cagObj.ItemID ??= ShrinkTable.Lookup(m, 1);

                            if (cagObj.Hue == null)
                            {
                                int hue = m.Hue & 0x7FFF;
                                hue = (hue & 0x4000) != 0 ? 0 : hue;
                                cagObj.Hue = hue == 0 ? null : hue;
                            }

                            m.Delete();
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Warning(e, "Failed to instantiate type {Type}.", type);
                        continue;
                    }
                }

                pooledList.Add(cagObj);
            }

            parent.Nodes = pooledList.ToArray();
        }

        return root;
    }
}

public record CAGJson
{
    [JsonPropertyName("category")]
    public string Category { get; init; }

    [JsonPropertyName("objects")]
    public CAGObject[] Objects { get; init; }
}
