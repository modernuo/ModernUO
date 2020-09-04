/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
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
using System.Text.Json.Serialization;
using Server.Json;

namespace Server.Commands
{
    public static class CAGLoader
    {
        public static CAGCategory Load()
        {
            var root = new CAGCategory("Add Menu");
            var path = Path.Combine(Core.BaseDirectory, "Data/objects.json");

            var list = JsonConfig.Deserialize<List<CAGJson>>(path);

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
                parent.Nodes = new CAGNode[cag.Objects.Length];
                for (var i = 0; i < cag.Objects.Length; i++)
                {
                    var obj = cag.Objects[i];
                    obj.Parent = parent;
                    parent.Nodes[i] = obj;
                }
            }

            return root;
        }
    }

    public class CAGJson
    {
        [JsonPropertyName("category")] public string Category { get; set; }

        [JsonPropertyName("objects")] public CAGObject[] Objects { get; set; }
    }
}
