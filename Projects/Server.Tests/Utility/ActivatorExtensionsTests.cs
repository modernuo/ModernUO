using Server.Utilities;
using Xunit;

namespace Server.Tests
{
    public class ActivatorExtensionsTests
    {
        [Fact]
        public void TestZeroParamsActivator()
        {
            var result = typeof(TestZeroParamsClass).CreateInstance<TestZeroParamsClass>();
            Assert.IsType<TestZeroParamsClass>(result);
        }

        [Fact]
        public void TestZeroParamsNullActivator()
        {
            var result = typeof(TestZeroParamsClass).CreateInstance<TestZeroParamsClass>(null, null);
            Assert.IsType<TestZeroParamsClass>(result);
        }

        [Fact]
        public void TestTwoParamsActivator()
        {
            var result = typeof(TestTwoParamsClass).CreateInstance<TestTwoParamsClass>(10, "ModernUO");
            Assert.IsType<TestTwoParamsClass>(result);
            Assert.Equal(10, result.Amount);
            Assert.Equal("ModernUO", result.Name);
        }

        [Fact]
        public void TestAllParamsOptionalActivator()
        {
            var result = typeof(TestTwoOptionalParamsClass).CreateInstance<TestTwoOptionalParamsClass>();
            Assert.IsType<TestTwoOptionalParamsClass>(result);
            Assert.Equal(1, result.Amount);
            Assert.Equal("Test ModernUO", result.Name);
        }

        [Fact]
        public void TestAllParamsNullOptionalActivator()
        {
            var result = typeof(TestTwoOptionalParamsClass).CreateInstance<TestTwoOptionalParamsClass>(null);
            Assert.IsType<TestTwoOptionalParamsClass>(result);
            Assert.Equal(1, result.Amount);
            Assert.Equal("Test ModernUO", result.Name);
        }

        [Fact]
        public void TestLessParamsOptionalActivator()
        {
            var result = typeof(TestTwoOptionalParamsClass).CreateInstance<TestTwoOptionalParamsClass>(10);
            Assert.IsType<TestTwoOptionalParamsClass>(result);
            Assert.Equal(10, result.Amount);
            Assert.Equal("Test ModernUO", result.Name);
        }

        [Fact]
        public void TestTwoParamsOptionalActivator()
        {
            var result = typeof(TestTwoOptionalParamsClass).CreateInstance<TestTwoOptionalParamsClass>(10, "Prod ModernUO");
            Assert.IsType<TestTwoOptionalParamsClass>(result);
            Assert.Equal(10, result.Amount);
            Assert.Equal("Prod ModernUO", result.Name);
        }

        private class TestZeroParamsClass
        {
        }

        private class TestTwoParamsClass
        {
            public int Amount;
            public string Name;

            public TestTwoParamsClass(int amount, string name)
            {
                Amount = amount;
                Name = name;
            }
        }

        private class TestTwoOptionalParamsClass
        {
            public int Amount;
            public string Name;

            public TestTwoOptionalParamsClass(int amount = 1, string name = "Test ModernUO")
            {
                Amount = amount;
                Name = name;
            }
        }
    }
}
