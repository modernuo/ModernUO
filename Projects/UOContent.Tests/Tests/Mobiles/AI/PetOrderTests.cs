using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class PetOrderTests : IDisposable
{
    // Track and delete every mobile we spawn so they don't linger in the shared static World
    // and pollute other tests in this collection (e.g. Tracking's nearby-mobile scan).
    private readonly List<Mobile> _created = new();

    private (PlayerMobile master, PetTestStub pet) Spawn(Point3D masterLoc, Point3D petLoc)
    {
        var pair = PetTestSetup.SpawnControlledPet(masterLoc, petLoc);
        _created.Add(pair.master);
        _created.Add(pair.pet);
        return pair;
    }

    public void Dispose()
    {
        foreach (var m in _created)
        {
            m?.Delete();
        }

        _created.Clear();
    }
    [Fact]
    public void SetPersistentOrder_Stay_AnchorsHomeToCurrentLocation()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1005, 1000, 0));

        pet.AIObject.SetPersistentOrder(OrderType.Stay);

        Assert.Equal(OrderType.Stay, pet.AIObject.PersistentOrder);
        Assert.Equal(pet.Location, pet.Home);
    }

    [Fact]
    public void SetPersistentOrder_Follow_ClearsAnchor()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1005, 1000, 0));
        pet.Home = new Point3D(900, 900, 0); // stale anchor

        pet.AIObject.SetPersistentOrder(OrderType.Follow);

        Assert.Equal(OrderType.Follow, pet.AIObject.PersistentOrder);
        Assert.Equal(Point3D.Zero, pet.Home);
    }

    [Fact]
    public void Stop_WhileAttacking_FallsBackToPersistentFollow()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1002, 1000, 0));
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
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1002, 1000, 0));
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
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), post);
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
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), postA);
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
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1002, 1000, 0));
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
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), post);
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
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1002, 1000, 0));
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
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), post);
        pet.ControlOrder = OrderType.Stay;
        pet.Direction = Direction.North;

        for (var i = 0; i < 40; i++)
        {
            pet.AIObject.DoOrderStay();
        }

        Assert.Equal(Direction.North, pet.Direction); // frozen -> no wander attempts
    }

    [Fact]
    public void ReleaseOrder_ClearsTheMaster_AndStartsTheDeleteCountdown()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.IsBonded = true;
        var followers = master.Followers;

        pet.ControlOrder = OrderType.Release;

        Assert.False(pet.Controlled);
        Assert.Null(pet.ControlMaster);
        Assert.False(pet.IsBonded);
        Assert.Equal(followers - pet.ControlSlots, master.Followers);
        Assert.True(pet.PendingDeleteTimer?.Running);
        Assert.Equal(pet.Location, pet.Home);
    }

    [Fact]
    public void LoyaltyRelease_ClearsTheMaster_AndStartsTheDeleteCountdown()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var followers = master.Followers;

        // What the loyalty drain assigns when loyalty reaches zero.
        pet.ControlOrder = OrderType.Release;

        Assert.False(pet.Controlled);
        Assert.Null(pet.ControlMaster);
        Assert.Equal(followers - pet.ControlSlots, master.Followers);
        Assert.True(pet.PendingDeleteTimer?.Running);
    }

    [Fact]
    public void Release_WithoutSpawner_AnchorsHomeToCurrentLocation()
    {
        var loc = new Point3D(1010, 1010, 0);
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), loc);
        pet.ControlOrder = OrderType.Stay; // sets Home to loc
        pet.Home = new Point3D(800, 800, 0); // simulate a stale anchor
        pet.Spawner = null;

        pet.ControlOrder = OrderType.Release;

        Assert.Equal(loc, pet.Home); // released where it stands, not the stale point
    }

    [Fact]
    public void Login_NearMaster_DerivesFollow()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        Assert.Equal(OrderType.None, pet.AIObject.PersistentOrder);

        PetLoginHandler.DeriveFollowerOrders(master);

        Assert.Equal(OrderType.Follow, pet.AIObject.PersistentOrder);
    }

    [Fact]
    public void Login_FarFromMaster_DerivesStay()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1040, 1000, 0));

        PetLoginHandler.DeriveFollowerOrders(master);

        Assert.Equal(OrderType.Stay, pet.AIObject.PersistentOrder);
    }

    [Fact]
    public void Stop_WhileFollowing_CancelsToIdle_NonML()
    {
        var previous = Core.Expansion;
        try
        {
            Core.Expansion = Expansion.SE; // pre-ML: Core.ML is false
            var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1002, 1000, 0));
            pet.ControlOrder = OrderType.Follow;

            pet.ControlOrder = OrderType.Stop;

            // Stop/idle resolution is era-independent.
            Assert.Equal(OrderType.None, pet.ControlOrder);
            Assert.Equal(OrderType.None, pet.AIObject.PersistentOrder);
            Assert.Equal(pet.Location, pet.Home);
        }
        finally
        {
            Core.Expansion = previous;
        }
    }
    // The parameterless ctor is the one that allocates the serial, runs DefaultMobileInit and
    // initializes PermaFlags/BOBFilter; the Serial ctor leaves those to Deserialize. Player is
    // what character creation sets, and the friend/transfer orders require a real player.
    private PlayerMobile SpawnPlayer(Point3D loc)
    {
        var pm = new PlayerMobile { Player = true };
        pm.MoveToWorld(loc, Map.Felucca);
        _created.Add(pm);
        return pm;
    }

    [Fact]
    public void Friend_Refused_RestsAtPersistentOrder_AndObeyDoesNotRepeat()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.ControlOrder = OrderType.Follow;          // persistent = Follow
        var friend = SpawnPlayer(new Point3D(1002, 1000, 0));
        pet.AddPetFriend(friend);                     // already a friend -> refusal 1049691

        pet.IssueOrder(OrderType.Friend, pet.ControlMaster, friend);

        Assert.Equal(OrderType.Follow, pet.ControlOrder); // never rests at Friend
        Assert.True(BaseAI.IsRestableOrder(pet.ControlOrder));

        // A tick must not re-run the refusal: the order is already Follow, so Obey follows.
        pet.AIObject.Obey();
        Assert.Equal(OrderType.Follow, pet.ControlOrder);
    }

    [Fact]
    public void Unfriend_OfNonFriend_RestsAtPersistentOrder()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.ControlOrder = OrderType.Stay;            // persistent = Stay
        var stranger = SpawnPlayer(new Point3D(1002, 1000, 0));

        pet.IssueOrder(OrderType.Unfriend, pet.ControlMaster, stranger);

        Assert.Equal(OrderType.Stay, pet.ControlOrder);
        Assert.Equal(OrderType.Stay, pet.AIObject.PersistentOrder);
    }

    [Fact]
    public void Friend_Accepted_FollowsTheNewFriend()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.ControlOrder = OrderType.Stay;
        var friend = SpawnPlayer(new Point3D(1002, 1000, 0));

        pet.IssueOrder(OrderType.Friend, master, friend);

        Assert.True(pet.IsPetFriend(friend));
        Assert.Equal(OrderType.Follow, pet.ControlOrder);
        Assert.Equal(OrderType.Follow, pet.AIObject.PersistentOrder);
        Assert.Same(friend, pet.ControlTarget);
        Assert.Equal(Point3D.Zero, pet.Home);
    }

    [Fact]
    public void Rename_RestsAtARestableOrder()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.ControlOrder = OrderType.Follow;

        pet.IssueOrder(OrderType.Rename, master);

        Assert.Equal(OrderType.Follow, pet.ControlOrder);
        Assert.True(BaseAI.IsRestableOrder(pet.ControlOrder));
    }

    [Fact]
    public void Drop_OnAPetThatCannotDrop_StillResolves()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.ControlOrder = OrderType.Stay;
        pet.IsDeadPet = true; // refuses to drop

        pet.IssueOrder(OrderType.Drop, master);

        Assert.Equal(OrderType.Stay, pet.ControlOrder);
    }

    [Fact]
    public void Stop_ResolvesToARestableOrder_FromEveryPrevious()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        OrderType[] previousOrders = [OrderType.Come, OrderType.Attack, OrderType.Guard, OrderType.Follow, OrderType.Stay, OrderType.None];

        for (var i = 0; i < previousOrders.Length; i++)
        {
            pet.ControlOrder = previousOrders[i];
            pet.ControlOrder = OrderType.Stop;
            Assert.True(BaseAI.IsRestableOrder(pet.ControlOrder));
            Assert.NotEqual(OrderType.Stop, pet.ControlOrder);
        }
    }

    [Fact]
    public void IssueOrder_RevealsTheIssuer_NeverTheMaster()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var friend = SpawnPlayer(new Point3D(1002, 1000, 0));
        pet.AddPetFriend(friend);
        master.Hidden = true;
        friend.Hidden = true;

        pet.IssueOrder(OrderType.Stay, friend);

        Assert.False(friend.Hidden);
        Assert.True(master.Hidden);
    }

    [Fact]
    public void SystemIssuedOrder_RevealsNobody()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        master.Hidden = true;

        pet.ControlOrder = OrderType.Follow; // raw assignment = system-issued

        Assert.True(master.Hidden);
    }

    [Fact]
    public void EndPickTarget_Attack_SetsCombatantAndFocus()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var victim = SpawnPlayer(new Point3D(1003, 1000, 0));

        pet.AIObject.EndPickTarget(master, victim, OrderType.Attack);

        Assert.Equal(OrderType.Attack, pet.ControlOrder);
        Assert.Same(victim, pet.ControlTarget);
        Assert.Same(victim, pet.Combatant);
        Assert.Same(victim, pet.FocusMob);
        Assert.True(pet.Warmode);
        Assert.Equal(1, pet.CombatantSets); // the Issue phase is the only writer

        pet.AIObject.Obey(); // the tick does not rewrite it
        Assert.Equal(1, pet.CombatantSets);
    }

    [Fact]
    public void Rename_WhileFollowing_KeepsFollowingTheMaster()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.IssueOrder(OrderType.Follow, master, master);

        pet.IssueOrder(OrderType.Rename, master); // the menu passes no target

        Assert.Equal(OrderType.Follow, pet.ControlOrder);
        Assert.Same(master, pet.ControlTarget); // restored, not null

        pet.AIObject.Obey();
        Assert.Equal(OrderType.Follow, pet.ControlOrder); // no "no one to follow" -> None
    }

    [Fact]
    public void TransferRefused_ResumesFollowingTheMaster_NotTheRecipient()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.IssueOrder(OrderType.Follow, master, master);
        var recipient = SpawnPlayer(new Point3D(1002, 1000, 0)); // no NetState -> the transfer is refused

        pet.IssueOrder(OrderType.Transfer, master, recipient);

        Assert.Equal(OrderType.Follow, pet.ControlOrder);
        Assert.Same(master, pet.ControlTarget);
        Assert.True(pet.Controlled);
        Assert.Same(master, pet.ControlMaster);
    }

    [Fact]
    public void SameOrderTwice_ReRunsIssue()
    {
        var postA = new Point3D(1005, 1005, 0);
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), postA);
        pet.ControlOrder = OrderType.Stay; // Home = A
        pet.MoveToWorld(new Point3D(1050, 1050, 0), pet.Map);

        pet.ControlOrder = OrderType.Stay; // reissued: re-anchor

        Assert.Equal(pet.Location, pet.Home);
    }

    [Fact]
    public void LoyaltyRelease_AndManualRelease_ProduceTheSameEndState()
    {
        var (masterA, petA) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var (masterB, petB) = Spawn(new Point3D(1100, 1100, 0), new Point3D(1101, 1100, 0));
        petA.Name = "Rex";
        petB.Name = "Rex";
        petA.IsBonded = true;
        petB.IsBonded = true;

        petA.IssueOrder(OrderType.Release, masterA); // player
        petB.ControlOrder = OrderType.Release;       // what the loyalty drain does

        PetTestStub[] pets = [petA, petB];

        for (var i = 0; i < pets.Length; i++)
        {
            var pet = pets[i];
            Assert.False(pet.Controlled);
            Assert.Null(pet.ControlMaster);
            Assert.False(pet.IsBonded);
            Assert.Null(pet.Name);
            Assert.Equal(OrderType.None, pet.ControlOrder);
            Assert.True(pet.PendingDeleteTimer?.Running);
            Assert.Equal(pet.Location, pet.Home);
        }

        Assert.Equal(masterA.Followers, masterB.Followers);
    }

    [Fact]
    public void SummonedPet_Released_IsKilledNotReleased()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.Summoned = true;
        pet.SummonMaster = master;

        pet.ControlOrder = OrderType.Release;

        Assert.True(pet.Deleted || !pet.Alive);
    }

    [Fact]
    public void Stop_WithNoStandingOrder_IdlesAnchoredWhereItStands()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        Assert.Equal(OrderType.Come, pet.ControlOrder); // fresh tame: no standing order, Home = Zero
        Assert.Equal(Point3D.Zero, pet.Home);

        pet.ControlOrder = OrderType.Stop; // what a vendor does after SetControlMaster(buyer)

        Assert.Equal(OrderType.None, pet.ControlOrder);
        Assert.Equal(pet.Location, pet.Home); // anchored: no unbounded wander
    }

    [Fact]
    public void Release_ClearsFriendsAndTheStandingOrder()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var friend = SpawnPlayer(new Point3D(1002, 1000, 0));
        pet.AddPetFriend(friend);
        pet.ControlOrder = OrderType.Guard; // persistent = Guard

        pet.IssueOrder(OrderType.Release, master);

        Assert.False(pet.IsPetFriend(friend));
        Assert.Equal(OrderType.None, pet.AIObject.PersistentOrder);
    }

    [Fact]
    public void ObeyOnALegacyTransientOrder_FallsBackToPersistent()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.ControlOrder = OrderType.Follow;          // persistent = Follow

        // Simulate a pre-refactor save resting at Rename: deserialization assigns the field raw.
        var field = typeof(BaseCreature).GetField("_controlOrder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(pet, OrderType.Rename);
        Assert.Equal(OrderType.Rename, pet.ControlOrder);

        pet.AIObject.Obey();

        Assert.Equal(OrderType.Follow, pet.ControlOrder);
    }
}
