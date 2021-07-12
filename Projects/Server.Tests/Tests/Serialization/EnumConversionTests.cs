using System;
using Xunit;

namespace Server.Tests
{
    public class EnumConversionTests
    {
        [Fact]
        public void TestToEnum()
        {
            var e = ReadEnum<TileFlag>();

            Assert.Equal(TileFlag.Container, e);
        }

        private unsafe T ReadEnum<T>() where T : unmanaged, Enum
        {
            var num = (long)TileFlag.Container;
            return *(T*)&num;
        }
    }
}
