using System;
using Server.Text;

namespace Server.Tests
{
    public static class AssertThat
    {
        // TODO: Swap actual and expected to match Assert
        public static void Equal(ReadOnlySpan<byte> actual, ReadOnlySpan<byte> expected) =>
            Xunit.Assert.True(
                expected.SequenceEqual(actual),
                $"Expected does not match actual.\nExpected:\t{expected.ToDelimitedHexString()}\nActual:\t\t{actual.ToDelimitedHexString()}"
            );
    }
}
