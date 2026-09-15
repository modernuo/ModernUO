using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;

namespace Server.Commands;

public sealed record SyncReport(List<string> Appended, List<string> Orphaned);

public static class CategorizationSync
{
    public static (List<CAGJson> updated, SyncReport report) Reconcile(
        List<CAGJson> categorization, IReadOnlyList<Type> discovered
    )
    {
        var categorizedTypes = new HashSet<Type>();
        foreach (var cag in categorization)
        {
            foreach (var obj in cag.Objects ?? [])
            {
                if (obj.Type != null)
                {
                    categorizedTypes.Add(obj.Type);
                }
            }
        }

        var discoveredSet = new HashSet<Type>(discovered);

        var orphaned = categorizedTypes
            .Where(t => !discoveredSet.Contains(t))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var updated = new List<CAGJson>(categorization);
        var appended = new List<string>();
        var itemAppend = new List<CAGObject>();
        var mobileAppend = new List<CAGObject>();

        foreach (var type in discovered)
        {
            if (categorizedTypes.Contains(type))
            {
                continue;
            }

            appended.Add(type.Name);
            var target = typeof(Mobile).IsAssignableFrom(type) ? mobileAppend : itemAppend;
            target.Add(new CAGObject { Type = type });
        }

        AppendUncategorized(updated, "Items.Uncategorized", itemAppend);
        AppendUncategorized(updated, "Mobiles.Uncategorized", mobileAppend);

        return (updated, new SyncReport(appended, orphaned));
    }

    private static void AppendUncategorized(List<CAGJson> updated, string category, List<CAGObject> toAdd)
    {
        if (toAdd.Count == 0)
        {
            return;
        }

        var existing = updated.FirstOrDefault(c => c.Category == category);
        if (existing == null)
        {
            updated.Add(new CAGJson { Category = category, Objects = toAdd.ToArray() });
            return;
        }

        var merged = new List<CAGObject>(existing.Objects ?? []);
        merged.AddRange(toAdd);
        updated[updated.IndexOf(existing)] = existing with { Objects = merged.ToArray() };
    }
}
