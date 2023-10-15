using Server;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

public class SkillPacketsTests : IClassFixture<ServerFixture>
{
    [Theory]
    [InlineData(SkillName.Alchemy, 0, 1)]
    [InlineData(SkillName.Archery, 10, 1000)]
    [InlineData(SkillName.Begging, 100000, 1000)]
    public void TestSkillChange(SkillName skillName, int baseFixedPoint, int capFixedPoint)
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var skill = m.Skills[skillName];
        skill.BaseFixedPoint = baseFixedPoint;
        skill.CapFixedPoint = capFixedPoint;

        var expected = new SkillChange(skill).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendSkillChange(skill);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestSkillsUpdate()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var skills = m.Skills;
        m.Skills[Utility.RandomSkill()].BaseFixedPoint = 1000;

        var expected = new SkillUpdate(skills).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendSkillsUpdate(skills);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }
}
