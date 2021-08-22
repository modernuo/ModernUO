using System;
using System.Collections.Generic;
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

        [Theory]
        [InlineData("this is a sentence that will probably wrap around a few times because it is long", 10, 6)]
        [InlineData("An Unnamed House", 10, 6)]
        [InlineData("Batville", 10, 6)]
        [InlineData("Bald's Shop", 10, 6)]
        [InlineData(
            "Something ThatIsVeryLongAndShouldBe broken up",
            10, 6,
            "Something", "ThatIsVery", "LongAndSho", "uldBe", "broken up"
        )]
        public void TestWrap(string sentence, int perLine, int maxLines, params string[] customExpected)
        {
            var expected = customExpected.Length > 0
                ? customExpected
                : OldWrap(sentence, perLine, maxLines).ToArray();
            var actual = sentence.Wrap(perLine, maxLines).ToArray();

            Assert.Equal(expected, actual);
        }

        // The old wrap function from HouseGump/HouseGumpAOS
        private static List<string> OldWrap(string value, int startIndex, int maxLines)
        {
            if (value == null || (value = value.Trim()).Length <= 0)
            {
                return null;
            }

            var values = value.Split(' ');
            var list = new List<string>();
            var current = "";

            for (var i = 0; i < values.Length; ++i)
            {
                var val = values[i];

                var v = current.Length == 0 ? val : $"{current} {val}";

                if (v.Length < startIndex)
                {
                    current = v;
                }
                else if (v.Length == startIndex)
                {
                    list.Add(v);

                    if (list.Count == maxLines)
                    {
                        return list;
                    }

                    current = "";
                }
                else if (val.Length <= startIndex)
                {
                    list.Add(current);

                    if (list.Count == maxLines)
                    {
                        return list;
                    }

                    current = val;
                }
                else
                {
                    while (v.Length >= startIndex)
                    {
                        list.Add(v[..startIndex]);

                        if (list.Count == maxLines)
                        {
                            return list;
                        }

                        v = v[startIndex..];
                    }

                    current = v;
                }
            }

            if (current.Length > 0)
            {
                list.Add(current);
            }

            return list;
        }
    }
}
