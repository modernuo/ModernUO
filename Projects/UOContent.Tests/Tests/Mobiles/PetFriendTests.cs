using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles;

// AddPetFriend is a public entry point — the pet context menu, the Friend order, and any
// server script all reach it — and the Friends list is capped by AllowNewPetFriend. The
// list holds no duplicate guard of its own (Tidy only drops deleted entries), so a repeated
// add stacks the same mobile: the count inflates against the five-friend cap, the duplicate
// is serialized, and a single RemovePetFriend leaves the person still a friend. The guard
// belongs in AddPetFriend so every caller is covered.
[Collection("Sequential UOContent Tests")]
public class PetFriendTests : IDisposable
{
    private readonly List<Mobile> _created = new();

    public void Dispose()
    {
        for (var i = 0; i < _created.Count; i++)
        {
            _created[i].Delete();
        }
    }

    private class CreatureStub : BaseCreature
    {
        public CreatureStub() : base(AIType.AI_Melee) => Body = 0xC9;

        public CreatureStub(Serial serial) : base(serial) => Body = 0xC9;

        // NPCSpeeds isn't configured in the test fixture; provide fixed speeds so the
        // AIType constructor doesn't hit the unconfigured speed table.
        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.3;
            passiveSpeed = 0.6;
        }

        public override void GetMoveSpeeds(out double activeMoveSpeed, out double passiveMoveSpeed)
        {
            activeMoveSpeed = 0.6;
            passiveMoveSpeed = 1.2;
        }
    }

    private CreatureStub NewCreature()
    {
        var bc = new CreatureStub();
        _created.Add(bc);
        return bc;
    }

    // A real mobile to friend; registered so a serialized round-trip would resolve it.
    private PlayerMobile NewFriend()
    {
        var friend = new PlayerMobile(World.NewMobile);
        friend.DefaultMobileInit();
        World.AddEntity(friend);
        _created.Add(friend);
        return friend;
    }

    [Fact]
    public void AddPetFriend_AddsTheFriendOnce()
    {
        var pet = NewCreature();
        var friend = NewFriend();

        pet.AddPetFriend(friend);

        Assert.True(pet.IsPetFriend(friend));
        Assert.Equal(friend, Assert.Single(pet.Friends));
    }

    [Fact]
    public void AddPetFriend_IgnoresDuplicate()
    {
        var pet = NewCreature();
        var friend = NewFriend();

        pet.AddPetFriend(friend);
        pet.AddPetFriend(friend);
        pet.AddPetFriend(friend);

        Assert.Equal(friend, Assert.Single(pet.Friends));
    }

    [Fact]
    public void AddPetFriend_DuplicatesDoNotConsumeTheFriendCap()
    {
        var pet = NewCreature();
        var friend = NewFriend();

        for (var i = 0; i < 10; i++)
        {
            pet.AddPetFriend(friend);
        }

        // Ten adds of one person must not exhaust the five-friend cap.
        Assert.True(pet.AllowNewPetFriend);

        var second = NewFriend();
        pet.AddPetFriend(second);
        Assert.True(pet.IsPetFriend(second));
    }

    [Fact]
    public void RemovePetFriend_AfterDuplicateAdd_LeavesNoResidue()
    {
        var pet = NewCreature();
        var friend = NewFriend();

        pet.AddPetFriend(friend);
        pet.AddPetFriend(friend);
        pet.RemovePetFriend(friend);

        // One unfriend must fully unfriend.
        Assert.False(pet.IsPetFriend(friend));
        Assert.True(pet.Friends == null || pet.Friends.Count == 0);
    }
}
