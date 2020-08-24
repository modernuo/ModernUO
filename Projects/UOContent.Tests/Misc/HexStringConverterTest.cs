using System;
using Xunit;
using Server.Misc;

namespace Server.Tests.Accounting
{
    public class HexStringConverterTest
    {
        [Theory]
        [InlineData("ABCDEF1234", new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34 })]
        public void ConvertsProperly(string input, byte[] bytes)
        {
            Span<byte> outputBytes = stackalloc byte[input.Length / 2];
            HexStringConverter.GetBytes(input, outputBytes);

            Assert.Equal(bytes, outputBytes.ToArray());
            Assert.Equal(input, HexStringConverter.GetString(bytes));
        }
    }
}
