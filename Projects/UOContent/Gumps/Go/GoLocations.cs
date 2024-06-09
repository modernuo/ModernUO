using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server.Json;
using Server.Logging;

namespace Server.Gumps;

public static class GoLocations
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GoLocations));

    private static LocationTree _felucca;
    private static LocationTree _trammel;
    private static LocationTree _ilshenar;
    private static LocationTree _malas;
    private static LocationTree _tokuno;
    private static LocationTree _terMur;

    public static LocationTree GetLocations(Map map)
    {
        if (map == Map.Ilshenar)
        {
            return _ilshenar ??= LoadLocations("ilshenar", Map.Ilshenar);
        }

        if (map == Map.TerMur)
        {
            return _terMur ??= LoadLocations("termur", Map.TerMur);
        }

        if (map == Map.Trammel)
        {
            return _trammel ??= LoadLocations("trammel", Map.Trammel);
        }

        if (map == Map.Malas)
        {
            return _malas ??= LoadLocations("malas", Map.Malas);
        }

        if (map == Map.Tokuno)
        {
            return _tokuno ??= LoadLocations("tokuno", Map.Tokuno);
        }

        return _felucca ??= LoadLocations("felucca", Map.Felucca);
    }

    private static LocationTree LoadLocations(string fileName, Map map)
    {
        var lastBranch = new Dictionary<Mobile, GoCategory>();

        var path = Path.Combine($"Data/Locations/{fileName}.json");

        try
        {
            var root = JsonConfig.Deserialize<GoCategory>(path);
            if (root == null)
            {
                throw new JsonException($"Failed to deserialize {path}.");
            }

            return new LocationTree(map, lastBranch, root);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to load file {Path}.", path);
            return new LocationTree(map, [], null);
        }
    }
}
