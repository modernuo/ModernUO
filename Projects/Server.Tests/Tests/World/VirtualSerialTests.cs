using Xunit;

namespace Server.Tests;

[Collection("Sequential Tests")]
public class VirtualSerialTests_SingleThreaded
{
    [Fact]
    public void TestNewVirtualGetsAndRollover_SingleThreaded()
    {
        // Acquire virtual serials until we hit the max
        Serial lastSerial = World.NewVirtual;
        do
        {
            Serial virtualSerial = World.NewVirtual;
            Assert.Equal(lastSerial + 1, virtualSerial);
            lastSerial = (Serial) (uint) virtualSerial;
        } while (lastSerial != World.MaxVirtualSerial);

        // Next one should be MaxItemSerial (the first VirtualSerial) due to rollover
        var nextSerial = World.NewVirtual;
        Assert.Equal(nextSerial, (Serial)World.ResetVirtualSerial);
    }
}

public class VirtualSerialTests_MultiThreaded
{
    [Fact]
    public void TestNewVirtualGetsAndRollover_MultiThreaded()
    {
        // Acquire virtual serials until we hit the max
        Serial firstSerial = World.NewVirtual;
        Serial lastSerial = (Serial)(uint)firstSerial;
        do
        {
            Serial virtualSerial = World.NewVirtual;
            Assert.True(virtualSerial >= lastSerial + 1);
            lastSerial = (Serial)(uint)virtualSerial;
        } while (lastSerial < World.MaxVirtualSerial && lastSerial > firstSerial);
    }
}
