using System;
using Xunit;

namespace Server.Tests
{
    public class TestStringHelpers
    {
        [Theory]
        [InlineData(null, "default value", "default value")]
        [InlineData("", "default value", "default value")]
        [InlineData("this is a valid string", "default value", "this is a valid string")]
        public void TestIsNullOrDefault(string value, string defaultValue, string expected)
        {
            var actual = value.DefaultIfNullOrEmpty(defaultValue);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("this is not capitalized", "This Is Not Capitalized")]
        [InlineData("", "")]
        [InlineData(null, null)]
        [InlineData("nospaceshere", "Nospaceshere")]
        [InlineData("harry the fireman", "Harry the Fireman")]
        public void TestCapitalize(string original, string capitalized)
        {
            var actual = original.Capitalize();

            Assert.Equal(capitalized, actual);
        }

        [Theory]
        [InlineData("we are testing removing spaces", " ", "wearetestingremovingspaces", StringComparison.Ordinal)]
        [InlineData("", " ", "", StringComparison.Ordinal)]
        [InlineData(null, null, null, StringComparison.Ordinal)]
        public void TestRemove(string original, string separator, string removed, StringComparison comparison)
        {
            var actual = original.AsSpan().Remove(separator, comparison);

            Assert.Equal(removed, actual);
        }
    }
}
