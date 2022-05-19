using Server.Collections;
using Xunit;

namespace Server.Tests;

public class BitArrayTests
{
    [Fact]
    public void TestBitArray()
    {
        var bitArray = new BitArray(700); // Restricted Spells;
        bitArray.Set(5, true);
        bitArray.Set(39, true);
        bitArray.Set(125, true);

        // Simulate World Saving
        var writer = new BufferWriter(1024, false);
        writer.Write(bitArray); // Save it to a file

        // Simulate World Loading
        var reader = new BufferReader(writer.Buffer);
        var bitArrayTest = reader.ReadBitArray();
        Assert.Equal(700, bitArrayTest.Length);
        for (var i = 0; i < bitArrayTest.Length; i++)
        {
            Assert.Equal(i is 5 or 39 or 125, bitArrayTest.Get(i));
        }
    }
}
