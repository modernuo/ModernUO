using System.Text.Json.Serialization;

namespace Server.Gumps
{
    public class GoLocation
    {
        public GoCategory Parent { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("location")] public Point3D Location { get; set; }
    }
}
