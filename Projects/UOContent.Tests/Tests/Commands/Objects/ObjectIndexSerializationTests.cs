using System.IO;
using Server.Commands;
using Server.Json;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

public class ObjectIndexSerializationTests
{
    [Fact]
    public void ObjectIndexFile_round_trips_through_json()
    {
        var index = new ObjectIndexFile
        {
            GeneratedUtc = "2026-07-19T00:00:00Z",
            Objects =
            [
                new ObjectIndexEntry
                {
                    Type = "Katana",
                    Entity = "item",
                    Category = "Items.Weapons.Swords",
                    Chunk = "items.weapons.swords",
                    ItemID = 8901,
                    Hue = 0x461,
                    Name = "katana",
                    Cliloc = 1041267
                }
            ]
        };

        var tempPath = Path.GetTempFileName();
        try
        {
            JsonConfig.Serialize(tempPath, index);
            var roundTripped = JsonConfig.Deserialize<ObjectIndexFile>(tempPath);

            Assert.NotNull(roundTripped);
            var entry = Assert.Single(roundTripped.Objects);

            Assert.Equal("Katana", entry.Type);
            Assert.Equal("items.weapons.swords", entry.Chunk);
            Assert.Equal(8901, entry.ItemID);
            Assert.Equal(0x461, entry.Hue);
            Assert.Equal(1041267, entry.Cliloc);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
