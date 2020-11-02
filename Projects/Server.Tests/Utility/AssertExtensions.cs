using System;
using System.Text;

namespace Server.Tests
{
    public static class AssertThat
    {
        public static string SpanToString(ReadOnlySpan<byte> bytes)
        {
            var builder = new StringBuilder();
            builder.Append("[");
            builder.AppendJoin(", ", bytes.ToArray());
            builder.Append("]");

            return builder.ToString();
        }

        public static void Equal(ReadOnlySpan<byte> actual, ReadOnlySpan<byte> expected) =>
            Xunit.Assert.True(
                expected.SequenceEqual(actual),
                $"Expected does not match actual.\nExpected:\t{SpanToString(expected)}\nActual:\t\t{SpanToString(actual)}"
            );
    }
}
