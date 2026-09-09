using Server;
using Server.Items;

namespace UOContent.Tests.Engines.Spawners;

// Serializes an item through the same BufferWriter path production saves use, producing an
// exact byte-for-byte blob of the current on-disk save layout. Used both to capture the
// legacy fixtures (SpawnerFixtureCapture) and, in a later task, to feed those fixtures
// through the post-migration deserializer.
internal static class SpawnerBlob
{
    public static byte[] Write(Item item)
    {
        var writer = new BufferWriter(true);
        item.Serialize(writer);
        return writer.Buffer[..(int)writer.Position];
    }
}
