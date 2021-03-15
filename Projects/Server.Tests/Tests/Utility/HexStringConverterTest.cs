using System;
using Server.Text;
using Xunit;

namespace Server.Tests
{
    public class HexStringConverterTest
    {
        [Theory, InlineData("ABCDEF1234", new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34 })]
        public void TestGetBytes(string input, byte[] bytes)
        {
            Span<byte> outputBytes = stackalloc byte[input.Length / 2];
            input.GetBytes(outputBytes);

            Assert.Equal(bytes, outputBytes.ToArray());
            Assert.Equal(input, bytes.ToHexString());
        }

        [Theory, InlineData("[AB, CD, EF, 12, 34]", new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34 })]
        public void TestsGetStringDelimited(string expected, byte[] bytes)
        {
            Assert.Equal(expected, bytes.ToDelimitedHexString());
        }
    }
}
