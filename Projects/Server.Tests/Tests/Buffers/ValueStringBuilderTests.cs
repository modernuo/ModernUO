using Server.Buffers;
using Xunit;

namespace Server.Tests.Buffers
{
    public class ValueStringBuilderTests
    {
        [Theory]
        [InlineData("Admin Kamron", "Kamron", 0, 6)]
        [InlineData("Admin Kamron", "Admin ron", 6, 3)]
        [InlineData("Admin Kamron", "Admin", 5, 7)]
        public void TestRemove(string original, string removed, int startIndex, int length)
        {
            using var sb = new ValueStringBuilder(stackalloc char[64]);
            sb.Append(original);
            sb.Remove(startIndex, length);

            Assert.Equal(removed, sb.ToString());
        }
    }
}
