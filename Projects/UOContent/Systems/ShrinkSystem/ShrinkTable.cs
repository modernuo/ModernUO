using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Server.Json;

namespace Server
{
    public static class ShrinkTable
    {
        public const int DefaultItemID = 0x1870; // Yellow virtue stone

        private static int[] _shrinkTable; // body is the index, value is the item id

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Lookup(Mobile m, int defaultValue = DefaultItemID) => Lookup(m.Body.BodyID, defaultValue);

        public static int Lookup(int body, int defaultValue = DefaultItemID)
        {
            _shrinkTable ??= Load();
            if (body < 0 || body >= _shrinkTable.Length)
            {
                return defaultValue;
            }

            var val = _shrinkTable[body];
            return val == 0 ? defaultValue : val;
        }

        private static int[] Load()
        {
            var path = "Data/shrink.json";

            var table = JsonConfig.Deserialize<Dictionary<string, string>>(path);
            if (table == null)
            {
                throw new JsonException($"Failed to deserialize {path}.");
            }

            int length = 0;

            foreach (var key in table.Keys)
            {
                var index = Utility.ToInt32(key);
                length = Math.Max(length, index + 1);
            }

            var list = new int[length];
            foreach (var (body, item) in table)
            {
                var index = Utility.ToInt32(body);
                list[index] = Utility.ToInt32(item);
            }

            return list;
        }
    }
}
