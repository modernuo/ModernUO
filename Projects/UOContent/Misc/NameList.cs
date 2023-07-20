using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server
{
    public class NameList
    {
        private static readonly Dictionary<string, NameList> m_Table =
            new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("names")] public string[] List { get; set; }

        public bool ContainsName(string name)
        {
            for (var i = 0; i < List.Length; i++)
            {
                if (name == List[i])
                {
                    return true;
                }
            }

            return false;
        }

        public string GetRandomName() => List.RandomElement() ?? "";

        public static NameList GetNameList(string type)
        {
            m_Table.TryGetValue(type, out var n);
            return n;
        }

        public static string RandomName(string type) => GetNameList(type)?.GetRandomName() ?? "";

        public static void Configure()
        {
            // TODO: Turn this into a command so it can be updated in-game
            var filePath = Path.Combine(Core.BaseDirectory, "Data/names.json");

            var nameLists = JsonConfig.Deserialize<List<NameList>>(filePath);

            if (nameLists == null)
            {
                throw new JsonException($"Failed to deserialize {filePath}.");
            }

            foreach (var nameList in nameLists)
            {
                nameList.FixNames();
                m_Table.Add(nameList.Type, nameList);
            }
        }

        private void FixNames()
        {
            for (var i = 0; i < List.Length; i++)
            {
                List[i] = List[i].Trim().Intern();
            }
        }
    }
}
