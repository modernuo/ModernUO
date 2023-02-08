/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: RegionJsonSerializer.cs                                         *
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
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Server.Json;
using Server.Logging;
using Server.Utilities;

namespace Server;

public static class RegionJsonSerializer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(RegionJsonSerializer));

    // Note: This is filled during the `Configure` phase by RegisterRegionForSerialization,
    // so it is not available to a JsonConverter which might be used during earlier phases.
    private static Dictionary<Type, Type> _regionToDtoLookup = new() { { typeof(Region), typeof(RegionJsonDto) } };
    private static JsonDerivedType[] _derivedTypes = { new(typeof(RegionJsonDto), nameof(Region)) };

    private static JsonSerializerOptions _options = new(JsonConfig.DefaultOptions)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                static typeInfo =>
                {
                    if (typeInfo.Type != typeof(RegionJsonDto))
                    {
                        return;
                    }

                    typeInfo.PolymorphismOptions = new JsonPolymorphismOptions();
                    for (var i = 0; i < _derivedTypes.Length; i++)
                    {
                        typeInfo.PolymorphismOptions.DerivedTypes.Add(_derivedTypes[i]);
                    }
                },
            }
        }
    };

    public static void Register<TDto, TRegion>()
        where TDto : RegionJsonDto, new() where TRegion : Region
    {
        for (var i = 0; i < _derivedTypes.Length; i++)
        {
            if (_derivedTypes[i].DerivedType == typeof(TDto))
            {
                throw new Exception(
                    $"Type '{typeof(TDto)}' has already been registered for serialization with the region loader."
                );
            }
        }

        Array.Resize(ref _derivedTypes, _derivedTypes.Length + 1);
        _derivedTypes[^1] = new JsonDerivedType(typeof(TDto), typeof(TRegion).Name);
        _regionToDtoLookup[typeof(TRegion)] = typeof(TDto);
    }

    internal static void LoadRegions()
    {
        var path = Path.Join(Core.BaseDirectory, "Data/regions.json");

        logger.Information("Loading regions");

        var stopwatch = Stopwatch.StartNew();

        var regions = JsonConfig.Deserialize<List<RegionJsonDto>>(path, _options);
        if (regions == null)
        {
            throw new JsonException($"Failed to deserialize {path}.");
        }

        var count = 0;
        foreach (var dto in regions)
        {
            if (Core.Expansion >= dto.MinExpansion && Core.Expansion <= dto.MaxExpansion)
            {
                var region = dto.ToRegion();
                region.Register();
                count++;
            }
        }

        stopwatch.Stop();

        logger.Information(
            "Loading regions {Status} ({Count} regions) ({Duration:F2} seconds)",
            "done",
            count,
            stopwatch.Elapsed.TotalSeconds
        );
    }

    // Note: This is not thread safe. To make `SerializeRegions` threadsafe, use `[ThreadStatic]` or just create the lists
    // in the function and take the GC hit.
    private static List<RegionJsonDto> _regionJsonDtos;

    public static void SerializeRegions(string path, IEnumerable<Region> regions)
    {
        _regionJsonDtos ??= new List<RegionJsonDto>();
        foreach (var region in regions)
        {
            if (_regionToDtoLookup.TryGetValue(region.GetType(), out var dtoType))
            {
                var dto = dtoType.CreateInstance<RegionJsonDto>();
                dto.FromRegion(region);
                _regionJsonDtos.Add(dto);
            }
        }

        JsonConfig.Serialize(path, _regionJsonDtos, _options);
        _regionJsonDtos.Clear();
    }

    public static string SerializeRegions(IEnumerable<Region> regions)
    {
        _regionJsonDtos ??= new List<RegionJsonDto>();
        foreach (var region in regions)
        {
            if (_regionToDtoLookup.TryGetValue(region.GetType(), out var dtoType))
            {
                var dto = dtoType.CreateInstance<RegionJsonDto>();
                dto.FromRegion(region);
                _regionJsonDtos.Add(dto);
            }
        }

        var output = JsonConfig.Serialize(_regionJsonDtos, _options);
        _regionJsonDtos.Clear();
        return output;
    }
}
