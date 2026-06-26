/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpawnerJsonSerializer.cs                                        *
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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Server.Json;
using Server.Logging;

namespace Server.Engines.Spawners;

public static class SpawnerJsonSerializer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SpawnerJsonSerializer));

    private static JsonDerivedType[] _derivedTypes = Array.Empty<JsonDerivedType>();
    private static JsonSerializerOptions _options;

    /// <summary>
    /// Invoked automatically during the Configure bootstrap phase
    /// (AssemblyHandler.Invoke("Configure")). Discovers every concrete SpawnerDto subclass
    /// marked with [JsonDiscoverableType] and registers it for STJ polymorphism.
    /// </summary>
    public static void Configure()
    {
        var discovered = new List<JsonDerivedType>();
        var byDiscriminator = new Dictionary<string, Type>();

        foreach (var asm in AssemblyHandler.Assemblies)
        {
            Collect(AssemblyHandler.GetTypeCache(asm).Types, discovered, byDiscriminator);
        }

        Collect(AssemblyHandler.GetTypeCache(Core.Assembly).Types, discovered, byDiscriminator);

        _derivedTypes = discovered.ToArray();
        _options = null; // force rebuild with the discovered types

        logger.Information("Discovered {Count} spawner JSON type(s)", _derivedTypes.Length);
    }

    private static void Collect(Type[] types, List<JsonDerivedType> discovered, Dictionary<string, Type> byDiscriminator)
    {
        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            if (type.IsAbstract || !type.IsAssignableTo(typeof(SpawnerDto)))
            {
                continue;
            }

            var attr = (JsonDiscoverableTypeAttribute)Attribute.GetCustomAttribute(
                type, typeof(JsonDiscoverableTypeAttribute), false);
            if (attr == null)
            {
                continue;
            }

            var (discriminator, derived) = Validate(type, byDiscriminator);
            byDiscriminator[discriminator] = type;
            discovered.Add(derived);
        }
    }

    internal static (string discriminator, JsonDerivedType derived) Validate(
        Type type, Dictionary<string, Type> byDiscriminator)
    {
        if (!IsJsonConstructible(type))
        {
            throw new Exception(
                $"SpawnerDto type '{type.FullName}' is marked [JsonDiscoverableType] but System.Text.Json cannot construct it. " +
                "Add a public parameterless constructor or use a record with init-only properties."
            );
        }

        var attr = (JsonDiscoverableTypeAttribute)Attribute.GetCustomAttribute(
            type, typeof(JsonDiscoverableTypeAttribute), false);
        var discriminator = attr?.Discriminator ?? type.Name;
        if (byDiscriminator.TryGetValue(discriminator, out var existing))
        {
            throw new Exception(
                $"Spawner JSON discriminator '{discriminator}' is claimed by both '{existing.FullName}' and " +
                $"'{type.FullName}'. Set an explicit discriminator via [JsonDiscoverableType(\"...\")] on one."
            );
        }

        return (discriminator, new JsonDerivedType(type, discriminator));
    }

    private static bool IsJsonConstructible(Type type)
    {
        foreach (var ctor in type.GetConstructors())
        {
            if (ctor.GetParameters().Length == 0)
            {
                return true;
            }

            if (Attribute.IsDefined(ctor, typeof(JsonConstructorAttribute)))
            {
                return true;
            }
        }

        return false;
    }

    public static JsonSerializerOptions Options =>
        _options ??= new JsonSerializerOptions(JsonConfig.GetOptions(new TextDefinitionConverterFactory()))
        {
            // Optional DTO fields are omitted at their CLR default; mandatory fields force-write with
            // [JsonIgnore(Condition = Never)]. (homeRange is special-cased below since 0 is valid.)
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    AddPolymorphism,
                    ConfigureHomeRange
                }
            }
        };

    private static void AddPolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(SpawnerDto))
        {
            return;
        }

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions();
        for (var i = 0; i < _derivedTypes.Length; i++)
        {
            typeInfo.PolymorphismOptions.DerivedTypes.Add(_derivedTypes[i]);
        }
    }

    // homeRange uses -1 as the "absent" sentinel (a real radius is >= 0, and 0 is a valid radius that
    // WhenWritingDefault could not emit). Write it only when it represents a real homeRange square.
    private static void ConfigureHomeRange(JsonTypeInfo typeInfo)
    {
        if (!typeInfo.Type.IsAssignableTo(typeof(SpawnerDto)))
        {
            return;
        }

        for (var i = 0; i < typeInfo.Properties.Count; i++)
        {
            if (typeInfo.Properties[i].Name == "homeRange")
            {
                typeInfo.Properties[i].ShouldSerialize = static (_, value) => value is int hr && hr >= 0;
            }
        }
    }
}
