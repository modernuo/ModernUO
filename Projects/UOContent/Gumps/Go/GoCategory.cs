using System.Text.Json.Serialization;

namespace Server.Gumps
{
    public class GoCategory
    {
        public GoCategory Parent { get; set; }

        [JsonPropertyName("locations")] public GoLocation[] Locations { get; set; }

        [JsonPropertyName("categories")] public GoCategory[] Categories { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }
    }
}
