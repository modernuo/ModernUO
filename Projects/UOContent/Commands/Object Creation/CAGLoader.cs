/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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

namespace Server.Commands;

public static class CAGLoader
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CAGLoader));

    public static CAGCategory Load()
    {
        var indexPath = Path.Combine(Core.BaseDirectory, "Data/objects/index.json");

        if (!File.Exists(indexPath))
        {
            logger.Warning("objects/index.json missing — run [GenObjects. Falling back to live categorization load.");
            return LoadLegacy();
        }

        var index = JsonConfig.Deserialize<ObjectIndexFile>(indexPath);
        if (index?.Objects == null)
        {
            throw new JsonException($"Failed to deserialize {indexPath}.");
        }

        var root = BuildTree(index);
        AddFallbackForStaleCache(root, index);
        return root;
    }

    public static CAGCategory BuildTree(ObjectIndexFile index)
    {
        var root = new CAGCategory("Add Menu");

        foreach (var entry in index.Objects)
        {
            var type = AssemblyHandler.FindTypeByName(entry.Type);
            if (type == null)
            {
                logger.Warning("Cached type {Type} no longer resolves; skipping.", entry.Type);
                continue;
            }

            var category = NavigateToCategory(root, entry.Category);
            AppendObject(
                category,
                new CAGObject
                {
                    Type = type,
                    ItemID = entry.ItemID,
                    Hue = entry.Hue == 0 ? null : entry.Hue,
                    Parent = category
                }
            );
        }

        return root;
    }

    private static CAGCategory NavigateToCategory(CAGCategory root, string dotted)
    {
        var parent = root;
        foreach (var name in dotted.Split('.'))
        {
            var child = FindCategory(parent, name);
            if (child == null)
            {
                child = new CAGCategory(name, parent);
                AppendNode(parent, child);
            }

            parent = child;
        }

        return parent;
    }

    private static CAGCategory FindCategory(CAGCategory parent, string title)
    {
        if (parent.Nodes == null)
        {
            return null;
        }

        foreach (var node in parent.Nodes)
        {
            if (node is CAGCategory cat && cat.Title == title)
            {
                return cat;
            }
        }

        return null;
    }

    private static void AppendNode(CAGCategory parent, CAGNode node)
    {
        var nodes = parent.Nodes ?? [];
        var grown = new CAGNode[nodes.Length + 1];
        Array.Copy(nodes, grown, nodes.Length);
        grown[^1] = node;
        parent.Nodes = grown;
    }

    private static void AppendObject(CAGCategory category, CAGObject obj) => AppendNode(category, obj);

    private static void AddFallbackForStaleCache(CAGCategory root, ObjectIndexFile index)
    {
        var cached = new HashSet<string>();
        foreach (var entry in index.Objects)
        {
            cached.Add(entry.Type);
        }

        var categorizationPath = Path.Combine(Core.BaseDirectory, "Data/categorization.json");
        var categorization = JsonConfig.Deserialize<List<CAGJson>>(categorizationPath);
        if (categorization == null)
        {
            return;
        }

        foreach (var cag in categorization)
        {
            foreach (var obj in cag.Objects ?? [])
            {
                if (obj.Type == null || cached.Contains(obj.Type.Name))
                {
                    continue;
                }

                logger.Warning("objects cache stale for {Type} — run [GenObjects.", obj.Type.Name);
                try
                {
                    var lean = ObjectIntrospection.ExtractLean(obj.Type);
                    var category = NavigateToCategory(root, cag.Category);
                    AppendObject(
                        category,
                        new CAGObject
                        {
                            Type = obj.Type,
                            ItemID = lean.ItemID,
                            Hue = lean.Hue == 0 ? null : lean.Hue,
                            Parent = category
                        }
                    );
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Failed live fallback for {Type}.", obj.Type.Name);
                }
            }
        }
    }

    private static CAGCategory LoadLegacy()
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
                                var hue = item.Hue & 0x7FFF;
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
                                var hue = m.Hue & 0x7FFF;
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
