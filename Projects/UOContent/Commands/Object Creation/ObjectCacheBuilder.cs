using System;
using System.Collections.Generic;

namespace Server.Commands;

public sealed record ExtractedObject(
    Type Type,
    string Entity,
    string Category,
    LeanMetadata Lean,
    List<CtorDoc> Ctors,
    List<PropertyDoc> Properties,
    List<OplLine> Opl,
    string BaseType
);

public static class ObjectCacheBuilder
{
    public static (ObjectIndexFile index, Dictionary<string, Dictionary<string, ObjectDetail>> chunks) Build(
        IReadOnlyList<ExtractedObject> objects, string generatedUtc
    )
    {
        var index = new ObjectIndexFile { GeneratedUtc = generatedUtc };
        var chunks = new Dictionary<string, Dictionary<string, ObjectDetail>>();

        foreach (var obj in objects)
        {
            var chunkKey = ObjectNaming.ChunkKey(obj.Category);

            index.Objects.Add(
                new ObjectIndexEntry
                {
                    Type = obj.Type.Name,
                    Entity = obj.Entity,
                    Category = obj.Category,
                    Chunk = chunkKey,
                    ItemID = obj.Lean.ItemID,
                    Hue = obj.Lean.Hue,
                    Name = obj.Lean.Name,
                    Cliloc = obj.Lean.Cliloc
                }
            );

            if (!chunks.TryGetValue(chunkKey, out var chunk))
            {
                chunks[chunkKey] = chunk = new Dictionary<string, ObjectDetail>();
            }

            chunk[obj.Type.Name] = new ObjectDetail
            {
                BaseType = obj.BaseType,
                Ctors = obj.Ctors,
                Properties = obj.Properties,
                Opl = obj.Opl
            };
        }

        return (index, chunks);
    }
}
