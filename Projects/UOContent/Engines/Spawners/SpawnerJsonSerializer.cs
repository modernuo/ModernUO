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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Server.Json;
using Server.Logging;
using Server.Text;

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
            // Optional fields omit at default; mandatory ones force-write via [JsonIgnore(Never)].
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

    // homeRange writes only when >= 0 (0 is a valid radius WhenWritingDefault could not emit).
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

    // --- Compact writer (matches the on-disk spawn-file layout used by [ExportSpawners) ---

    private const int CompactPrintWidth = 100;

    private static readonly JsonSerializerOptions _scalarOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Serializes to the compact spawn-file layout: a container is inline only when all its values
    /// are scalars and the line fits within 100 columns; otherwise it expands (2-space indent).
    /// UTF-8, LF, no BOM. Admin/cold path.
    /// </summary>
    public static string SerializeCompact<T>(T value)
    {
        var node = JsonSerializer.SerializeToNode(value, Options);
        var sb = ValueStringBuilder.Create();
        try
        {
            WriteNode(node, ref sb, 0, 0);
            sb.Append("\n");
            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }

    private static void WriteNode(JsonNode node, ref ValueStringBuilder sb, int indent, int column)
    {
        if (node is not JsonArray and not JsonObject)
        {
            sb.Append(node?.ToJsonString(_scalarOptions) ?? "null");
            return;
        }

        if (AllScalarChildren(node))
        {
            var inline = Inline(node);
            if (column + inline.Length <= CompactPrintWidth)
            {
                sb.Append(inline);
                return;
            }
        }

        var childIndent = indent + 1;
        var childColumn = childIndent * 2;

        if (node is JsonArray arr)
        {
            sb.Append("[\n");
            for (var i = 0; i < arr.Count; i++)
            {
                sb.Append(' ', childColumn);
                WriteNode(arr[i], ref sb, childIndent, childColumn);
                sb.Append(i < arr.Count - 1 ? ",\n" : "\n");
            }

            sb.Append(' ', indent * 2);
            sb.Append("]");
            return;
        }

        var obj = (JsonObject)node;
        sb.Append("{\n");
        var index = 0;
        var count = obj.Count;
        foreach (var pair in obj)
        {
            sb.Append(' ', childColumn);
            var prefix = $"\"{pair.Key}\": ";
            sb.Append(prefix);
            WriteNode(pair.Value, ref sb, childIndent, childColumn + prefix.Length);
            sb.Append(++index < count ? ",\n" : "\n");
        }

        sb.Append(' ', indent * 2);
        sb.Append("}");
    }

    private static bool AllScalarChildren(JsonNode node)
    {
        if (node is JsonArray arr)
        {
            foreach (var element in arr)
            {
                if (element is JsonArray or JsonObject)
                {
                    return false;
                }
            }

            return true;
        }

        foreach (var pair in (JsonObject)node)
        {
            if (pair.Value is JsonArray or JsonObject)
            {
                return false;
            }
        }

        return true;
    }

    private static string Inline(JsonNode node)
    {
        var sb = ValueStringBuilder.Create();
        try
        {
            AppendInline(node, ref sb);
            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }

    private static void AppendInline(JsonNode node, ref ValueStringBuilder sb)
    {
        switch (node)
        {
            case JsonArray arr:
                {
                    sb.Append("[");
                    for (var i = 0; i < arr.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }

                        AppendInline(arr[i], ref sb);
                    }

                    sb.Append("]");
                    break;
                }
            case JsonObject obj:
                {
                    if (obj.Count == 0)
                    {
                        sb.Append("{}");
                        break;
                    }

                    sb.Append("{ ");
                    var first = true;
                    foreach (var pair in obj)
                    {
                        if (!first)
                        {
                            sb.Append(", ");
                        }

                        first = false;
                        sb.Append("\"");
                        sb.Append(pair.Key);
                        sb.Append("\": ");
                        AppendInline(pair.Value, ref sb);
                    }

                    sb.Append(" }");
                    break;
                }
            default:
                sb.Append(node?.ToJsonString(_scalarOptions) ?? "null");
                break;
        }
    }
}
