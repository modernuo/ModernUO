using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.Commands;

public static class GenObjects
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GenObjects));

    public static void Configure()
    {
        CommandSystem.Register("GenObjects", AccessLevel.Developer, GenObjects_OnCommand);
    }

    [Usage("GenObjects")]
    [Aliases("GenObjWeb")]
    [Description("Generates the objects cache (index + detail chunks) and syncs categorization.json.")]
    private static void GenObjects_OnCommand(CommandEventArgs e)
    {
        var baseDir = Core.BaseDirectory;
        var categorizationPath = Path.Combine(baseDir, "Data", "categorization.json");
        var categorization = JsonConfig.Deserialize<List<CAGJson>>(categorizationPath) ?? [];
        var discovered = ObjectIntrospection.DiscoverConstructibleTypes();

        var result = ObjectCacheGenerator.Generate(categorization, discovered);

        var objectsDir = Path.Combine(baseDir, "Data", "objects");
        var detailDir = Path.Combine(objectsDir, "detail");
        Directory.CreateDirectory(detailDir);

        JsonConfig.Serialize(Path.Combine(objectsDir, "index.json"), result.Index);
        foreach (var (chunkKey, map) in result.Chunks)
        {
            JsonConfig.Serialize(Path.Combine(detailDir, $"{chunkKey}.json"), map);
        }

        JsonConfig.Serialize(categorizationPath, result.UpdatedCategorization);

        e.Mobile.SendMessage(
            $"Objects cache written: {result.Index.Objects.Count} objects, {result.Chunks.Count} chunks. " +
            $"Appended {result.Report.Appended.Count} to Uncategorized, {result.Report.Orphaned.Count} orphaned."
        );

        if (result.Report.Appended.Count > 0)
        {
            logger.Information("Appended to Uncategorized: {Types}", string.Join(", ", result.Report.Appended));
        }

        if (result.Report.Orphaned.Count > 0)
        {
            logger.Warning("Orphaned categorization entries: {Types}", string.Join(", ", result.Report.Orphaned));
        }
    }
}
