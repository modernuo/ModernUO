using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Server.Tests
{
    public class HashSetTests
    {
        [Fact]
        public void TestHashSet()
        {
            var set = new HashSet<string> { "random string1", "another random string1", "another random string2", "another random string3" };

            var list = set.ToList();
            string[] arr = { "random string1", "another random string1", "another random string2", "another random string3" };
            int i = 0;
            foreach (var entry in list)
            {
                Assert.Equal(entry, arr[i++]);
            }
        }
    }
}
