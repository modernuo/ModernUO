using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.Commands;

public sealed record ObjectCacheResult(
    ObjectIndexFile Index,
    Dictionary<string, Dictionary<string, ObjectDetail>> Chunks,
    List<CAGJson> UpdatedCategorization,
    SyncReport Report
);

public static class ObjectCacheGenerator
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ObjectCacheGenerator));

    public static ObjectCacheResult Generate(List<CAGJson> categorization, IReadOnlyList<Type> discovered)
    {
        var (updatedCategorization, report) = CategorizationSync.Reconcile(categorization, discovered);

        var categoryByType = new Dictionary<Type, string>();
        foreach (var cag in updatedCategorization)
        {
            foreach (var obj in cag.Objects ?? [])
            {
                if (obj.Type != null)
                {
                    categoryByType[obj.Type] = cag.Category;
                }
            }
        }

        var generatedUtc = DateTime.UtcNow.ToString("O");
        var extracted = new List<ExtractedObject>();
        foreach (var type in discovered)
        {
            try
            {
                var entity = typeof(Mobile).IsAssignableFrom(type) ? "mobile" : "item";
                var category = categoryByType.TryGetValue(type, out var c)
                    ? c
                    : entity == "mobile" ? "Mobiles.Uncategorized" : "Items.Uncategorized";

                extracted.Add(
                    new ExtractedObject(
                        type,
                        entity,
                        category,
                        ObjectIntrospection.ExtractLean(type),
                        ObjectIntrospection.ExtractCtors(type),
                        ObjectIntrospection.ExtractProperties(type),
                        ObjectIntrospection.ExtractOpl(type),
                        type.BaseType?.Name ?? "object"
                    )
                );
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to introspect {Type}; skipping.", type);
            }
        }

        var (index, chunks) = ObjectCacheBuilder.Build(extracted, generatedUtc);
        return new ObjectCacheResult(index, chunks, updatedCategorization, report);
    }
}
