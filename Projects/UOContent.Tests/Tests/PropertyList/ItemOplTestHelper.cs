using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Server;

namespace UOContent.Tests;

public static class ItemOplTestHelper
{
    // Builds the item's OPL and returns attribute/property cliloc lines (>= 1060000),
    // ignoring base-item lines (name, weight, etc.) so tests isolate the attribute surface.
    public static Dictionary<int, string> DecodeAttributeLines(Item item)
    {
        var opl = new ObjectPropertyList(item);
        item.GetProperties(opl);
        opl.Terminate();

        var buffer = opl.Buffer;
        var map = new Dictionary<int, string>();
        var pos = 15;
        while (true)
        {
            var cliloc = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(pos));
            pos += 4;
            if (cliloc == 0)
            {
                break;
            }

            var byteLen = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(pos));
            pos += 2;
            var arg = Encoding.Unicode.GetString(buffer, pos, byteLen);
            pos += byteLen;

            if (cliloc is >= 1060000 and < 1080000)
            {
                map[cliloc] = arg;
            }
        }

        return map;
    }
}
