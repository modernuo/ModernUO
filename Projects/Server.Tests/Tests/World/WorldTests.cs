using System;
using System.Net;
using Xunit;

namespace Server.Tests
{
    public class WorldTests
    {
        [Fact]
        public void TestVirtualSerialRollover()
        {
            // Acquire virtual serials until we hit the max
            Serial lastSerial = World.NewVirtual;
            do
            {
                Serial virtualSerial = World.NewVirtual;
                Assert.True(virtualSerial >= lastSerial + 1);
                lastSerial = (Serial) (uint) virtualSerial;
            } while (lastSerial != 0x7FFFFFFF);

            // Next one should be 0x7EEEEEEE due to rollover
            var nextSerial = World.NewVirtual;
            Assert.Equal(nextSerial, (Serial)0x7EEEEEEE);
        }
    }
}
