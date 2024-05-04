using Server.Collections;
using Xunit;

namespace Server.Tests;

public class OrderedSetTests
{
    [Fact]
    public void TestOrderedSet()
    {
        int[] expected = [6, 2, 4, 23, 0];
        var set = new OrderedSet<int>();
        for (var i = 0; i < expected.Length; i++)
        {
            set.Add(expected[i]);
        }

        Assert.Equal(expected.Length, set.Count);

        Assert.True(set.Contains(4));
        Assert.False(set.Contains(8));

        int index = 0;
        foreach (var value in set)
        {
            Assert.Equal(expected[index++], value);
        }
    }
}
