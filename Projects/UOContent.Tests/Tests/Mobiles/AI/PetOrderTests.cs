using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class PetOrderTests
{
    [Fact]
    public void SetPersistentOrder_Stay_AnchorsHomeToCurrentLocation()
    {
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), new Point3D(1005, 1000, 0));

        pet.AIObject.SetPersistentOrder(OrderType.Stay);

        Assert.Equal(OrderType.Stay, pet.AIObject.PersistentOrder);
        Assert.Equal(pet.Location, pet.Home);
    }

    [Fact]
    public void SetPersistentOrder_Follow_ClearsAnchor()
    {
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), new Point3D(1005, 1000, 0));
        pet.Home = new Point3D(900, 900, 0); // stale anchor

        pet.AIObject.SetPersistentOrder(OrderType.Follow);

        Assert.Equal(OrderType.Follow, pet.AIObject.PersistentOrder);
        Assert.Equal(Point3D.Zero, pet.Home);
    }
}
