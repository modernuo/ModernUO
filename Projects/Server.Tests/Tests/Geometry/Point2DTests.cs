using System;
using Xunit;

namespace Server.Tests
{
    public class Point2DTests
    {
        [Fact]
        public void TestToString()
        {
            Assert.Equal("(0, 0)", new Point2D(0, 0).ToString());
            Assert.Equal("(1, 1)", new Point2D(1, 1).ToString());
            Assert.Equal($"({Int32.MaxValue}, {Int32.MaxValue})", new Point2D(Int32.MaxValue, Int32.MaxValue).ToString());
            Assert.Equal($"({Int32.MinValue}, {Int32.MinValue})", new Point2D(Int32.MinValue, Int32.MinValue).ToString());
        }
    }
}
