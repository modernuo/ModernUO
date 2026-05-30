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

    [Fact]
    public void Stop_WhileAttacking_FallsBackToPersistentFollow()
    {
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), new Point3D(1002, 1000, 0));
        pet.ControlOrder = OrderType.Follow; // persistent = Follow
        pet.ControlOrder = OrderType.Attack; // transient
        Assert.Equal(OrderType.Follow, pet.AIObject.PersistentOrder);

        pet.ControlOrder = OrderType.Stop;

        Assert.Equal(OrderType.Follow, pet.ControlOrder); // resumed standing order
        Assert.Equal(OrderType.Follow, pet.AIObject.PersistentOrder);
    }

    [Fact]
    public void Stop_WhileFollowing_CancelsToIdleNone()
    {
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), new Point3D(1002, 1000, 0));
        pet.ControlOrder = OrderType.Follow;

        pet.ControlOrder = OrderType.Stop;

        Assert.Equal(OrderType.None, pet.ControlOrder);
        Assert.Equal(OrderType.None, pet.AIObject.PersistentOrder);
        Assert.Equal(pet.Location, pet.Home); // idle anchor = where stopped
    }

    [Fact]
    public void Stop_WhileStaying_RemainsStayingAtOriginalPost()
    {
        var post = new Point3D(1005, 1005, 0);
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), post);
        pet.ControlOrder = OrderType.Stay; // Home = post
        Assert.Equal(post, pet.Home);

        pet.ControlOrder = OrderType.Stop;

        Assert.Equal(OrderType.Stay, pet.ControlOrder);
        Assert.Equal(OrderType.Stay, pet.AIObject.PersistentOrder);
        Assert.Equal(post, pet.Home); // post unchanged
    }

    [Fact]
    public void Stay_ThenFollow_ThenStop_DoesNotReturnToOldStayAnchor() // report 2
    {
        var postA = new Point3D(1005, 1005, 0);
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), postA);
        pet.ControlOrder = OrderType.Stay;   // Home = A
        pet.ControlOrder = OrderType.Follow; // Home cleared to Zero
        pet.MoveToWorld(new Point3D(1050, 1050, 0), pet.Map); // walked to B
        pet.ControlOrder = OrderType.Stop;   // stop while following

        Assert.NotEqual(postA, pet.Home);    // never re-acquires A
        Assert.Equal(pet.Location, pet.Home); // idles at B
    }

    [Fact]
    public void AttackTargetLost_ResumesPersistentFollow()
    {
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), new Point3D(1002, 1000, 0));
        pet.ControlOrder = OrderType.Follow; // persistent = Follow
        pet.ControlOrder = OrderType.Attack;
        pet.ControlTarget = null;            // target gone

        pet.AIObject.DoOrderAttack();        // invalid-target path

        Assert.Equal(OrderType.Follow, pet.ControlOrder);
    }

    [Fact]
    public void StayPet_AttacksThenTargetLost_ReturnsToOriginalPost() // report 1
    {
        var post = new Point3D(1005, 1005, 0);
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), post);
        pet.ControlOrder = OrderType.Stay;   // persistent = Stay, Home = post
        pet.ControlOrder = OrderType.Attack;
        pet.MoveToWorld(new Point3D(1060, 1060, 0), pet.Map); // chased far to the "corpse"
        pet.ControlTarget = null;

        pet.AIObject.DoOrderAttack();

        Assert.Equal(OrderType.Stay, pet.ControlOrder);
        Assert.Equal(post, pet.Home);        // anchor still the original post, not the corpse
    }

    // NOTE: the test fixture does not load tile data, so Mobile.Move is blocked and Location
    // never changes here. DoMoveImpl still sets Mobile.Direction before the (blocked) move,
    // so an *attempted* wander is observable via Direction. The subjective wander cadence is
    // covered by manual QA; these tests verify the gate/frozen wiring deterministically.
    [Fact]
    public void IdlePet_DoesNotAttemptToMove_WhileResting()
    {
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), new Point3D(1002, 1000, 0));
        pet.ControlOrder = OrderType.Follow;
        pet.ControlOrder = OrderType.Stop; // -> idle None
        Assert.Equal(OrderType.None, pet.ControlOrder);

        pet.ForceIdle = true; // CheckIdle() reports resting -> idle wander must be skipped
        pet.Direction = Direction.North;
        for (var i = 0; i < 40; i++)
        {
            pet.AIObject.DoOrderNone();
        }

        Assert.Equal(Direction.North, pet.Direction); // gated by CheckIdle -> never attempts a step
    }

    [Fact]
    public void StayingPet_DoesNotAttemptToMove()
    {
        var post = new Point3D(1005, 1005, 0);
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), post);
        pet.ControlOrder = OrderType.Stay;
        pet.Direction = Direction.North;

        for (var i = 0; i < 40; i++)
        {
            pet.AIObject.DoOrderStay();
        }

        Assert.Equal(Direction.North, pet.Direction); // frozen -> no wander attempts
    }

    [Fact]
    public void Release_WithoutSpawner_AnchorsHomeToCurrentLocation()
    {
        var loc = new Point3D(1010, 1010, 0);
        var (_, pet) = PetTestSetup.SpawnControlledPet(new Point3D(1000, 1000, 0), loc);
        pet.ControlOrder = OrderType.Stay; // sets Home to loc
        pet.Home = new Point3D(800, 800, 0); // simulate a stale anchor
        pet.Spawner = null;

        pet.AIObject.DoOrderRelease();

        Assert.Equal(loc, pet.Home); // released where it stands, not the stale point
    }
}
