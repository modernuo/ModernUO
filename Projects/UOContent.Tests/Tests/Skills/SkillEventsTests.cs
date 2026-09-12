using System;
using Server;
using Server.Misc;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SkillEventsTests
{
    [Fact]
    public void CheckSkill_RaisesSkillChecked_WithFromSkillAndOutcome()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];

        Mobile seenFrom = null;
        Skill seenSkill = null;
        var seenSuccess = false;
        var calls = 0;

        void Handler(Mobile m, Skill s, bool ok)
        {
            seenFrom = m;
            seenSkill = s;
            seenSuccess = ok;
            calls++;
        }

        SkillEvents.SkillChecked += Handler;
        try
        {
            // Utility.RandomDouble() is in [0, 1), so a chance of 1.0 always succeeds and 0.0 always fails.
            var guaranteed = SkillCheck.CheckSkill(from, skill, null, 1.0);
            Assert.True(guaranteed);
            Assert.Equal(1, calls);
            Assert.Same(from, seenFrom);
            Assert.Same(skill, seenSkill);
            Assert.True(seenSuccess);

            var impossible = SkillCheck.CheckSkill(from, skill, null, 0.0);
            Assert.False(impossible);
            Assert.Equal(2, calls);
            Assert.False(seenSuccess);
        }
        finally
        {
            SkillEvents.SkillChecked -= Handler;
            from.Delete();
        }
    }

    [Fact]
    public void CheckSkill_WithZeroSkillCap_DoesNotRaiseSkillChecked()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];
        from.Skills.Cap = 0;

        var calls = 0;

        void Handler(Mobile m, Skill s, bool ok) => calls++;

        SkillEvents.SkillChecked += Handler;
        try
        {
            Assert.False(SkillCheck.CheckSkill(from, skill, null, 1.0));
            Assert.Equal(0, calls);
        }
        finally
        {
            SkillEvents.SkillChecked -= Handler;
            from.Delete();
        }
    }

    [Fact]
    public void CheckSkill_WithNoSubscriber_DoesNotThrow()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];

        Assert.True(SkillCheck.CheckSkill(from, skill, null, 1.0));

        from.Delete();
    }
}
