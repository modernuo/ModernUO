using Server.Items;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class WeaponAbilityPacketTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(0, false)]
    [InlineData(100, true)]
    [InlineData(1000, false)]
    public void TestSpecialAbility(int abilityId, bool active)
    {
        var expected = new ToggleSpecialAbility(abilityId, active).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendToggleSpecialAbility(abilityId, active);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestClearAbility()
    {
        var expected = new ClearWeaponAbility().Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendClearWeaponAbility();

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
