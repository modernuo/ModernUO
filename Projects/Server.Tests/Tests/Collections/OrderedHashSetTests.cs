using System.Linq;
using Server.Collections;
using Xunit;

namespace Server.Tests
{
    public class OrderedHashSetTests
    {
        [Fact]
        public void TestOrderedHashSet()
        {
            var set = new OrderedHashSet<string>(1)
            {
                "random string1", "another random string1", "another random string2", "another random string3"
            };
            set.Remove("another random string1");
            var list = set.ToList();

            string[] arr = { "random string1", "another random string2", "another random string3" };
            int i = 0;
            foreach (var entry in list)
            {
                Assert.Equal(entry, arr[i++]);
            }
        }

        [Fact]
        public void TestOrderedHashWithNull()
        {
            var set = new OrderedHashSet<string>(1)
            {
                "random string1", null, "another random string2", "another random string3"
            };

            set.Remove("another random string1");
            var list = set.ToList();

            string[] arr = { "random string1", null, "another random string2", "another random string3" };
            int i = 0;
            foreach (var entry in list)
            {
                Assert.Equal(entry, arr[i++]);
            }
        }
    }
}
