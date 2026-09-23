using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class PetFriendMembershipTests
{
    [Fact]
    public void AddPetFriend_Twice_StoresOneMembership()
    {
        var pet = new PetTestStub();
        var friend = new PlayerMobile();

        try
        {
            pet.AddPetFriend(friend);
            pet.AddPetFriend(friend);

            Assert.Single(pet.Friends);
            Assert.True(pet.IsPetFriend(friend));
        }
        finally
        {
            pet.Delete();
            friend.Delete();
        }
    }

    [Fact]
    public void RemovePetFriend_AfterRepeatedAdd_RevokesMembership()
    {
        var pet = new PetTestStub();
        var friend = new PlayerMobile();

        try
        {
            pet.AddPetFriend(friend);
            pet.AddPetFriend(friend);
            pet.RemovePetFriend(friend);

            Assert.False(pet.IsPetFriend(friend));
            Assert.Null(pet.Friends);
        }
        finally
        {
            pet.Delete();
            friend.Delete();
        }
    }

    [Fact]
    public void RemoveAndReadd_LeavesOtherFriendsUnchanged()
    {
        var pet = new PetTestStub();
        var first = new PlayerMobile();
        var second = new PlayerMobile();

        try
        {
            pet.AddPetFriend(first);
            pet.AddPetFriend(second);
            pet.RemovePetFriend(first);

            Assert.False(pet.IsPetFriend(first));
            Assert.True(pet.IsPetFriend(second));
            Assert.Single(pet.Friends);

            pet.AddPetFriend(first);
            Assert.True(pet.IsPetFriend(first));
            Assert.True(pet.IsPetFriend(second));
            Assert.Equal(2, pet.Friends.Count);
        }
        finally
        {
            pet.Delete();
            first.Delete();
            second.Delete();
        }
    }
}
