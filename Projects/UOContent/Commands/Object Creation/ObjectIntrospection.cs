using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Server.Items;

namespace Server.Commands;

public readonly record struct LeanMetadata(int ItemID, int Hue, string Name, int? Cliloc);

public static class ObjectIntrospection
{
    public static LeanMetadata ExtractLean(Type type)
    {
        if (type.IsAssignableTo(typeof(Item)))
        {
            var item = type.CreateInstance<Item>();
            try
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

                var hue = item.Hue & 0x7FFF;
                hue = (hue & 0x4000) != 0 ? 0 : hue;

                var cliloc = item.LabelNumber > 0 ? item.LabelNumber : (int?)null;
                var name = item.Name ?? (cliloc.HasValue ? Server.Localization.GetText(cliloc.Value) : null);

                return new LeanMetadata(itemID, hue, name, cliloc);
            }
            finally
            {
                item.Delete();
            }
        }

        if (type.IsAssignableTo(typeof(Mobile)))
        {
            var m = type.CreateInstance<Mobile>();
            try
            {
                var itemID = ShrinkTable.Lookup(m, 1);
                var hue = m.Hue & 0x7FFF;
                hue = (hue & 0x4000) != 0 ? 0 : hue;
                return new LeanMetadata(itemID, hue, m.Name, null);
            }
            finally
            {
                m.Delete();
            }
        }

        throw new ArgumentException($"{type} is neither Item nor Mobile.", nameof(type));
    }

    public static List<CtorDoc> ExtractCtors(Type type)
    {
        var docs = new List<CtorDoc>();

        foreach (var ctor in type.GetConstructors())
        {
            if (!Attributes.IsConstructible(ctor, AccessLevel.Developer))
            {
                continue;
            }

            var doc = new CtorDoc();
            foreach (var p in ctor.GetParameters())
            {
                doc.Parameters.Add(
                    new ParamDoc
                    {
                        Name = p.Name,
                        Type = ObjectNaming.FriendlyTypeName(p.ParameterType),
                        Default = p.HasDefaultValue ? p.DefaultValue?.ToString() ?? "null" : null,
                        IsParams = p.IsDefined(typeof(ParamArrayAttribute), false)
                    }
                );
            }

            docs.Add(doc);
        }

        return docs;
    }

    public static List<PropertyDoc> ExtractProperties(Type type)
    {
        var docs = new List<PropertyDoc>();

        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var p in props)
        {
            var attr = p.GetCustomAttribute<CommandPropertyAttribute>(true);
            if (attr == null)
            {
                continue;
            }

            var pt = p.PropertyType;
            docs.Add(
                new PropertyDoc
                {
                    Name = p.Name,
                    Type = ObjectNaming.FriendlyTypeName(pt),
                    ReadLevel = attr.ReadLevel.ToString(),
                    WriteLevel = attr.WriteLevel.ToString(),
                    ReadOnly = attr.ReadOnly || !p.CanWrite,
                    EnumValues = pt.IsEnum ? Enum.GetNames(pt) : null
                }
            );
        }

        return docs;
    }

    public static List<OplLine> ExtractOpl(Type type)
    {
        if (type.IsAssignableTo(typeof(Item)))
        {
            var item = type.CreateInstance<Item>();
            try
            {
                var opl = new ObjectPropertyList(item);
                item.GetProperties(opl);
                return DecodeOpl(opl);
            }
            finally
            {
                item.Delete();
            }
        }

        if (type.IsAssignableTo(typeof(Mobile)))
        {
            var m = type.CreateInstance<Mobile>();
            try
            {
                var opl = new ObjectPropertyList(m);
                m.GetProperties(opl);
                return DecodeOpl(opl);
            }
            finally
            {
                m.Delete();
            }
        }

        return [];
    }

    private static List<OplLine> DecodeOpl(ObjectPropertyList opl)
    {
        opl.Terminate();
        var buffer = opl.Buffer;
        var lines = new List<OplLine>();
        var pos = 15; // fixed OPL header length

        while (pos + 4 <= buffer.Length)
        {
            var cliloc = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(pos));
            pos += 4;
            if (cliloc == 0)
            {
                break;
            }

            var byteLen = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(pos));
            pos += 2;
            var args = byteLen > 0 ? Encoding.Unicode.GetString(buffer, pos, byteLen) : null;
            pos += byteLen;

            lines.Add(new OplLine { Cliloc = cliloc, Args = args, Text = Localization.GetText(cliloc) });
        }

        return lines;
    }
}
