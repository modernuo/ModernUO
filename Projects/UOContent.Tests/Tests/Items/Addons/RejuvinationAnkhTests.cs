using Server.Items;
using Xunit;

namespace Server.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class RejuvinationAnkhTests
{
    [Fact]
    public void DoubleClick_FromOutOfReach_DoesNotUseTheAnkh()
    {
        var player = new Mobile((Serial)0x2201u);
        player.DefaultMobileInit();
        player.Location = new Point3D(10, 0, 0);
        var component = new RejuvinationAddonComponent(0x3);

        try
        {
            component.OnDoubleClick(player);

            Assert.True(player.CanBeginAction<RejuvinationAddonComponent>());
        }
        finally
        {
            component.Delete();
            player.Delete();
        }
    }
}
