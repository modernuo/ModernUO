using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class VirtualSerialTests
{
    [Fact]
    public void TestNewVirtualGetsAndRollover()
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
