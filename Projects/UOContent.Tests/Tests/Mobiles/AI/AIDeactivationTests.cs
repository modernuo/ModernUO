using Server;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class AIDeactivationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Deactivate_WithoutWorldMap_StopsTimer(bool internalMap, bool controlled)
    {
        var creature = new PetTestStub();
        try
        {
            creature.Controlled = controlled;
            creature.Map = internalMap ? Map.Internal : null;
            creature.AIObject.Activate();
            Assert.True(creature.AIObject.AITimer.Running);

            creature.AIObject.Deactivate();

            Assert.False(creature.AIObject.AITimer.Running);
        }
        finally
        {
            creature.Delete();
        }
    }
}
