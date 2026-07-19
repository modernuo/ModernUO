using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Server.Commands;

public static class ObjectNaming
{
    public static string ChunkKey(string category) => category.ToLowerInvariant().Replace(' ', '-');

    public static string FriendlyTypeName(Type t)
    {
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return $"{FriendlyTypeName(Nullable.GetUnderlyingType(t))}?";
        }

        return t switch
        {
            _ when t == typeof(int)     => "int",
            _ when t == typeof(uint)    => "uint",
            _ when t == typeof(bool)    => "bool",
            _ when t == typeof(string)  => "string",
            _ when t == typeof(double)  => "double",
            _ when t == typeof(float)   => "float",
            _ when t == typeof(long)    => "long",
            _ when t == typeof(ulong)   => "ulong",
            _ when t == typeof(short)   => "short",
            _ when t == typeof(ushort)  => "ushort",
            _ when t == typeof(byte)    => "byte",
            _ when t == typeof(sbyte)   => "sbyte",
            _ when t == typeof(char)    => "char",
            _ when t == typeof(decimal) => "decimal",
            _                           => t.Name
        };
    }
}

public sealed class ObjectIndexEntry
{
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("entity")] public string Entity { get; set; }
    [JsonPropertyName("category")] public string Category { get; set; }
    [JsonPropertyName("chunk")] public string Chunk { get; set; }
    [JsonPropertyName("gfx")] public int ItemID { get; set; }
    [JsonPropertyName("hue")] public int Hue { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("cliloc")] public int? Cliloc { get; set; }
}

public sealed class ObjectIndexFile
{
    [JsonPropertyName("generatedUtc")] public string GeneratedUtc { get; set; }
    [JsonPropertyName("objects")] public List<ObjectIndexEntry> Objects { get; set; } = [];
}

public sealed class ParamDoc
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("default")] public string Default { get; set; }
    [JsonPropertyName("isParams")] public bool IsParams { get; set; }
}

public sealed class CtorDoc
{
    [JsonPropertyName("parameters")] public List<ParamDoc> Parameters { get; set; } = [];
}

public sealed class PropertyDoc
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("readLevel")] public string ReadLevel { get; set; }
    [JsonPropertyName("writeLevel")] public string WriteLevel { get; set; }
    [JsonPropertyName("readOnly")] public bool ReadOnly { get; set; }
    [JsonPropertyName("enumValues")] public string[] EnumValues { get; set; }
}

public sealed class OplLine
{
    [JsonPropertyName("cliloc")] public int Cliloc { get; set; }
    [JsonPropertyName("args")] public string Args { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; }
}

public sealed class ObjectDetail
{
    [JsonPropertyName("baseType")] public string BaseType { get; set; }
    [JsonPropertyName("ctors")] public List<CtorDoc> Ctors { get; set; } = [];
    [JsonPropertyName("properties")] public List<PropertyDoc> Properties { get; set; } = [];
    [JsonPropertyName("opl")] public List<OplLine> Opl { get; set; } = [];
}
