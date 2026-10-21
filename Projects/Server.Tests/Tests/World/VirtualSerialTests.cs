using System.Reflection;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class VirtualSerialTests
{
    // Walking up from ResetVirtualSerial takes ~17.9 million draws; start just short of the wrap.
    private const uint DrawsBeforeMax = 1000;

    private static readonly FieldInfo _nextVirtualSerial =
        typeof(World).GetField("_nextVirtualSerial", BindingFlags.NonPublic | BindingFlags.Static);

    [Fact]
    public void TestNewVirtualGetsAndRollover()
    {
        Assert.NotNull(_nextVirtualSerial);

        var saved = (uint)_nextVirtualSerial.GetValue(null);
        _nextVirtualSerial.SetValue(null, World.MaxVirtualSerial - DrawsBeforeMax);

        try
        {
            // Acquire virtual serials until we hit the max
            var lastSerial = World.NewVirtual;
            do
            {
                var virtualSerial = World.NewVirtual;
                Assert.Equal(lastSerial + 1, virtualSerial);
                lastSerial = (Serial)(uint)virtualSerial;
            } while (lastSerial != World.MaxVirtualSerial);

            // Next one should be MaxItemSerial (the first VirtualSerial) due to rollover
            var nextSerial = World.NewVirtual;
            Assert.Equal(nextSerial, (Serial)World.ResetVirtualSerial);
        }
        finally
        {
            _nextVirtualSerial.SetValue(null, saved);
        }
    }
}
